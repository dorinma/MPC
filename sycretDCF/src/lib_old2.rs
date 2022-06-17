#![allow(clippy::needless_range_loop)]

use rayon::prelude::*;

pub mod eq;
pub mod fss;
pub mod le;
pub mod stream;
pub mod utils;

//------
use fss::dpf::*;
use fss::dif::*;
use utils::Mmo;
use crate::stream::Prg;
use rand::Rng;

use serde_json::{Result, Value};
use std::os::raw::c_char;
use std::ffi::CString;

use std::ffi::CStr;
//remove
use std::fs::File;
use std::io::Write;
//------
// Byte precision and security.
pub const N: usize = 4;
pub const L: usize = 16;

#[repr(C)]
pub struct Keys {    
    pub aes_keys: *mut c_char,
    pub key_a: *mut c_char,
    pub key_b: *mut c_char
}


//--------------

fn store_string_on_heap(string_to_store: &'static str) -> *mut c_char {
    //create a new raw pointer
    let pntr = CString::new(string_to_store).unwrap().into_raw();

    //return the c_char
    return pntr;
}

fn string_to_static_str(s: String) -> &'static str {
    Box::leak(s.into_boxed_str())
}

#[no_mangle]
pub extern fn gen_dcf2(alpha: u32) -> Keys {
    let mut rng = rand::thread_rng();
    let aes_keys: [u128; 4] = rng.gen();
    let mut prg = Mmo::from_slice(&aes_keys);
    let (k_a, k_b) = DIFKeyAlpha1::generate_keypair(&mut prg, alpha);

    let json_a = serde_json::to_string(&k_a).unwrap();
    let json_b = serde_json::to_string(&k_b).unwrap();
    let json_aes = serde_json::to_string(&aes_keys).unwrap();

    let keys = Keys {
        aes_keys: store_string_on_heap(string_to_static_str(json_aes)),
        key_a: store_string_on_heap(string_to_static_str(json_a)),
        key_b: store_string_on_heap(string_to_static_str(json_b))
    };
    keys
}

fn my_string_safe(ptr: *mut c_char) -> String {
    unsafe {
        CStr::from_ptr(ptr).to_string_lossy().into_owned()
    }
}

#[no_mangle]
pub extern fn eval_dcf2(key_ptr:*mut c_char, aes_keys_ptr: *mut c_char, alpha: u32, party_id: u8) -> u32 {
    let path = format!("C:\\Users\\eden\\Desktop\\BGU\\Project\\shahar.txt");
    let mut res = File::create(path);

    let key_str = my_string_safe(key_ptr);
    let aes_str = my_string_safe(aes_keys_ptr);
    let key: DIFKeyAlpha1 = serde_json::from_str(&key_str).unwrap();
    let aes_keys: [u128; 4] = serde_json::from_str(&aes_str).unwrap();
   
    // let aes_keys: [u128; 4] = bincode::deserialize(&aes_keys_pointer[..]).unwrap();
    // let k: DIFKeyAlpha1 = bincode::deserialize(&key[..]).unwrap();
    let mut prg = Mmo::from_slice(&aes_keys);
    let t_output = key.eval(&mut prg, party_id, alpha);
    t_output
    // match res {
    //     Ok(ref mut file) =>
    //         file.write_all(format!("Abc {key_str}").as_bytes()),
    //     Err(e) => return 1,
    // };
    // 5
}

//public methods
#[no_mangle]
pub extern fn free_string(ptr: *mut c_char) {
    unsafe {
        let mut ptr_to_free = ptr;
        let _ = CString::from_raw(ptr_to_free);
        ptr_to_free = 0 as *mut c_char;
    }
}

//--------------
fn build_params(op_id: usize) -> (usize, usize, usize) {
    let (keylen, n_aes_keys) = match op_id {
        // 1 => (1205, 4),
        1 => (920, 3),
        _ => (621, 2),
    };

    // TODO: small inputs
    let n_aes_streams = 128;

    (n_aes_keys, keylen, n_aes_streams)
}

/// # Safety
/// Declare function to be used within C
#[no_mangle]
pub unsafe extern "C" fn keygen(
    keys_a_pointer: *mut u8,
    keys_b_pointer: *mut u8,
    n_values: usize,
    n_threads: usize,
    op_id: usize,
) {
    assert!(!keys_a_pointer.is_null());
    assert!(!keys_b_pointer.is_null());

    let (n_aes_keys, keylen, n_aes_streams) = build_params(op_id);

    // Harcoded AES-128 keys for Mmo
    let mut aes_keys = Vec::new();
    for i in 0..n_aes_keys {
        aes_keys.push(i as u128);
    }

    let mut key_stream_args = vec![];
    let mut line_counter = 0;
    let default_length = n_values / n_aes_streams;
    let n_longer_streams = n_values % n_aes_streams;
    let mut stream_length: usize;

    for stream_id in 0..n_aes_streams {
        // The first streams work a bit more if necessary
        if stream_id < n_longer_streams {
            stream_length = default_length + 1;
        } else {
            stream_length = default_length;
        }

        if stream_length > 0 {
            // Cast raw pointers to a type that can be sent to threads
            key_stream_args.push((
                stream_id,
                stream_length,
                keys_a_pointer.add(keylen * line_counter) as usize,
                keys_b_pointer.add(keylen * line_counter) as usize,
            ));
            line_counter += stream_length;
        }
    }

    // Each thread will repeatedly execute this closure in parallel
    let create_keypair = |key_stream_arg: &(usize, usize, usize, usize)| {
        let (stream_id, stream_length, key_a_pointer, keys_b_pointer) = *key_stream_arg;
        stream::generate_key_stream(
            &aes_keys,
            stream_id,
            stream_length,
            key_a_pointer,
            keys_b_pointer,
            op_id,
        );
    };

    // Force Rayon to use the number of thread provided by the user, unless a pool already exists
    let _ = rayon::ThreadPoolBuilder::new()
        .num_threads(n_threads)
        .build_global();
    key_stream_args.par_iter().for_each(create_keypair);
}

/// # Safety
/// Declare function to be used within C
#[no_mangle]
pub unsafe extern "C" fn eval(
    party_id: usize,
    xs_pointer: *const u8,
    keys_pointer: *const u8,
    results_pointer: *mut i64,
    n_values: usize,
    n_threads: usize,
    op_id: usize,
) {
    assert!(!xs_pointer.is_null());
    assert!(!keys_pointer.is_null());
    assert!(!results_pointer.is_null());

    let (n_aes_keys, keylen, n_aes_streams) = build_params(op_id);

    // Harcoded AES-128 keys for Mmo
    let mut aes_keys = Vec::new();
    for i in 0..n_aes_keys {
        aes_keys.push(i as u128);
    }

    let mut key_stream_args = vec![];
    let mut line_counter = 0;
    let default_length = n_values / n_aes_streams;
    let n_longer_streams = n_values % n_aes_streams;
    let mut stream_length: usize;

    for stream_id in 0..n_aes_streams {
        // The first streams work a bit more if necessary
        if stream_id < n_longer_streams {
            stream_length = default_length + 1;
        } else {
            stream_length = default_length;
        }

        if stream_length > 0 {
            // Cast raw pointers to a type that can be sent to threads
            key_stream_args.push((
                stream_id,
                stream_length,
                xs_pointer.add(N * line_counter) as usize,
                keys_pointer.add(keylen * line_counter) as usize,
                results_pointer.add(line_counter) as usize,
            ));
            line_counter += stream_length;
        }
    }

    // Each thread will repeatedly execute this closure in parallel
    let eval_key = |key_stream_arg: &(usize, usize, usize, usize, usize)| {
        let (stream_id, stream_length, x_pointer, key_pointer, result_pointer) = *key_stream_arg;
        stream::eval_key_stream(
            party_id as u8,
            &aes_keys,
            stream_id,
            stream_length,
            x_pointer,
            key_pointer,
            result_pointer,
            op_id,
        );
    };

    // Force Rayon to use the number of thread provided by the user, unless a pool already exists
    let _ = rayon::ThreadPoolBuilder::new()
        .num_threads(n_threads)
        .build_global();
    key_stream_args.par_iter().for_each(eval_key);
}