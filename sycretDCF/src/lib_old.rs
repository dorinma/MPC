#![allow(clippy::needless_range_loop)]

//use std::os::raw::c_char;
//use std::ffi::CString;

use rayon::prelude::*;
use rand::Rng;

pub mod eq;
pub mod fss;
pub mod le;
pub mod stream;
pub mod utils;

// Byte precision and security.
pub const N: usize = 4;
pub const L: usize = 16;

// ------------------- Added code ------------------- 
use fss::dpf::*;
use fss::dif::*;
use utils::Mmo;
use crate::stream::Prg;
// ----------temp----------
use std::fs::File;
use std::io::prelude::*;
use std::fs::OpenOptions;
use serde_json::{Result, Value};
use std::io::{self, BufRead};
use std::path::Path;
// --------------------

#[repr(C)]
pub struct Keys {    
    pub aes_keys: Box<Vec<u8>>,
    pub key_a: Box<Vec<u8>>,
    pub key_b: Box<Vec<u8>>
}

#[repr(C)]
pub struct Response {    
    pub aes_keys: Box<Vec<u8>>,
    pub share: u32
}

#[no_mangle]
pub extern fn gen_dpf(alpha: u32) -> Keys {
    let mut rng = rand::thread_rng();
    let aes_keys: [u128; 4] = rng.gen();
    let mut prg = Mmo::from_slice(&aes_keys);
    let (k_a, k_b) = DPFKeyAlpha1::generate_keypair(&mut prg, alpha);
    let bytes_aes_keys = bincode::serialize(&aes_keys).unwrap();
    let bytes_a = bincode::serialize(&k_a).unwrap();
    let bytes_b = bincode::serialize(&k_b).unwrap();
    
    let x_aes_keys = Box::new(bytes_aes_keys);
    let x_a = Box::new(bytes_a);
    let x_b = Box::new(bytes_b);

    let keys = Keys {
        aes_keys: x_aes_keys,
        key_a: x_a,
        key_b: x_b
    };
    keys
}

fn read_lines<P>(filename: P) -> io::Result<io::Lines<io::BufReader<File>>>
where P: AsRef<Path>, {
    let file = File::open(filename)?;
    Ok(io::BufReader::new(file).lines())
}

#[no_mangle]
pub extern fn gen_dpf_test(alpha: u32) -> u32 {
    let mut rng = rand::thread_rng();
    let aes_keys: [u128; 4] = rng.gen();
    let mut prg = Mmo::from_slice(&aes_keys);
    let (k_a, k_b) = DPFKeyAlpha1::generate_keypair(&mut prg, alpha);

    let json_a = serde_json::to_string(&k_a).unwrap();
    let json_b = serde_json::to_string(&k_b).unwrap();
    let json_aes = serde_json::to_string(&aes_keys).unwrap();

    let path = format!("C:\\Users\\eden\\Desktop\\BGU\\Project\\eden.txt");
    let mut res = File::create(path);
    match res {
        Ok(ref mut file) =>{
            write!(file, "{}\n", json_a);
            write!(file, "{}\n", json_b);
            write!(file, "{}\n", json_aes);
            return 1;
        }
        Err(e) => return 2,
    };
}

#[no_mangle]
pub extern fn eval_dpf_test(alpha: u32,  party_id: u8) -> u32 {
    let f = File::open("C:\\Users\\eden\\Desktop\\BGU\\Project\\eden.txt").unwrap();
    let mut reader = io::BufReader::new(f);

    let mut str_a = String::new();
    let mut str_b = String::new();
    let mut str_aes = String::new();
    reader.read_line(&mut str_a).unwrap();
    reader.read_line(&mut str_b).unwrap();
    reader.read_line(&mut str_aes).unwrap();

    let key_a: DPFKeyAlpha1 = serde_json::from_str(&str_a).unwrap();
    let key_b: DPFKeyAlpha1 = serde_json::from_str(&str_b).unwrap();
    let aes: [u128; 4] = serde_json::from_str(&str_aes).unwrap();

    let mut prg = Mmo::from_slice(&aes);

    if (party_id == 0){
        let t_output = key_a.eval(&mut prg, party_id, alpha);
        t_output
    }
    else{
        let t_output = key_b.eval(&mut prg, party_id, alpha);
        t_output
    }
    
}

#[no_mangle]
pub extern fn gen_dpf2(alpha: u32) -> u32 {
    let mut rng = rand::thread_rng();
    let aes_keys: [u128; 4] = rng.gen();
    let mut prg = Mmo::from_slice(&aes_keys);
    let (k_a, k_b) = DPFKeyAlpha1::generate_keypair(&mut prg, alpha);

    let json_a = serde_json::to_string(&k_a).unwrap();
    let json_b = serde_json::to_string(&k_b).unwrap();
    let json_aes = serde_json::to_string(&aes_keys).unwrap();

    let path = format!("C:\\Users\\eden\\Desktop\\BGU\\Project\\eden.txt");
    let mut res = File::create(path);
    match res {
        Ok(ref mut file) =>{
            write!(file, "{}\n", json_a);
            write!(file, "{}\n", json_b);
            write!(file, "{}\n", json_aes);
        }
        Err(e) => return 2,
    };

    let f = File::open("C:\\Users\\eden\\Desktop\\BGU\\Project\\eden.txt").unwrap();
    let mut reader = io::BufReader::new(f);

    let mut str_a = String::new();
    let mut str_b = String::new();
    let mut str_aes = String::new();
    reader.read_line(&mut str_a).unwrap();
    reader.read_line(&mut str_b).unwrap();
    reader.read_line(&mut str_aes).unwrap();

    let key_a: DPFKeyAlpha1 = serde_json::from_str(&str_a).unwrap();
    let key_b: DPFKeyAlpha1 = serde_json::from_str(&str_b).unwrap();
    let aes: [u128; 4] = serde_json::from_str(&str_aes).unwrap();

    let mut prg = Mmo::from_slice(&aes);
    let t_output_a = key_a.eval(&mut prg, 0, alpha);
    let t_output_b = key_b.eval(&mut prg, 1, alpha);

    t_output_a.wrapping_add(t_output_b) 
    // if let Ok(lines) = read_lines("C:\\Users\\eden\\Desktop\\BGU\\Project\\eden.txt") {
    //     // Consumes the iterator, returns an (Optional) String
    //     //let key_a: DPFKeyAlpha1 = serde_json::from_str(&lines[0].unwrap()).unwrap();
    //     for line in lines {
    //         if let Ok(ip) = line {
    //             println!("{}", ip);
    //         }
    //     }
    // }

    //let t: DPFKeyAlpha1 = serde_json::from_str(&a).unwrap();
    
    // if k_a.cw_leaf == key_a.cw_leaf && k_b.cw_leaf == key_b.cw_leaf && aes_keys == aes {
    //     1
    // } else {
    //     0
    // }
    // let bytes_aes_keys = bincode::serialize(&aes_keys).unwrap();
    // let bytes_a = bincode::serialize(&k_a).unwrap();
    // let bytes_b = bincode::serialize(&k_b).unwrap();
    // let path = format!("C:\\Users\\eden\\Desktop\\BGU\\Project\\eden.txt");
    // let mut res = File::create(path);
    // match res {
    //     Ok(ref mut file) =>
    //         file.write_all(&bytes_aes_keys),
    //     Err(e) => return 1,
    // };

    
    // let x_aes_keys = Box::new(bytes_aes_keys);
    // let x_a = Box::new(bytes_a);
    // let x_b = Box::new(bytes_b);

    // let keys = Keys {
    //     aes_keys: x_aes_keys,
    //     key_a: x_a,
    //     key_b: x_b
    // };
    // keys
}

#[no_mangle]
pub extern fn eval_dpf(key: Box<Vec<u8>>, aes_keys_pointer: Box<Vec<u8>>, alpha: u32, party_id: u8) -> u32 {
    let path = format!("C:\\Users\\דורין\\Desktop\\FinalProject\\MPC\\log{party_id}.txt");
    let mut res = File::create(path);
    match res {
        Ok(ref mut file) =>
            file.write_all(format!("aes_keys_pointer {:p}", aes_keys_pointer).as_bytes()),
        Err(e) => return 1,
    };
    let aes_keys: [u128; 4] = bincode::deserialize(&aes_keys_pointer[..]).unwrap();
    match res {
        Ok(ref mut file) =>
            file.write_all(format!("2").as_bytes()),
        Err(e) => return 1,
    };
    let k: DPFKeyAlpha1 = bincode::deserialize(&key[..]).unwrap();
    let mut prg = Mmo::from_slice(&aes_keys);
    let t_output = k.eval(&mut prg, party_id, alpha);
    match res {
        Ok(ref mut file) =>
            file.write_all(format!("Abc {t_output}").as_bytes()),
        Err(e) => return 1,
    };
    t_output
    /*let response = Response {
        aes_keys: aes_keys_pointer,
        share: t_output,
    };
    response*/
}


// -------------------dcf-------------------------

#[no_mangle]
pub extern fn gen_dcf_test(alpha: u32) -> u32 {
    let mut rng = rand::thread_rng();
    let aes_keys: [u128; 4] = rng.gen();
    let mut prg = Mmo::from_slice(&aes_keys);
    let (k_a, k_b) = DPFKeyAlpha1::generate_keypair(&mut prg, alpha);

    let json_a = serde_json::to_string(&k_a).unwrap();
    let json_b = serde_json::to_string(&k_b).unwrap();
    let json_aes = serde_json::to_string(&aes_keys).unwrap();

    let path = format!("C:\\Users\\eden\\Desktop\\BGU\\Project\\eden.txt");
    let mut res = File::create(path);
    match res {
        Ok(ref mut file) =>{
            write!(file, "{}\n", json_a);
            write!(file, "{}\n", json_b);
            write!(file, "{}\n", json_aes);
            return 1;
        }
        Err(e) => return 2,
    };
}

#[no_mangle]
pub extern fn gen_dcf(alpha: u32) -> Keys {
    let mut rng = rand::thread_rng();
    let aes_keys: [u128; 4] = rng.gen();
    let mut prg = Mmo::from_slice(&aes_keys);
    let (k_a, k_b) = DIFKeyAlpha1::generate_keypair(&mut prg, alpha);
    let bytes_aes_keys = bincode::serialize(&aes_keys).unwrap();
    let bytes_a = bincode::serialize(&k_a).unwrap();
    let bytes_b = bincode::serialize(&k_b).unwrap();
    
    let x_aes_keys = Box::new(bytes_aes_keys);
    let x_a = Box::new(bytes_a);
    let x_b = Box::new(bytes_b);

    let keys = Keys {
        aes_keys: x_aes_keys,
        key_a: x_a,
        key_b: x_b
    };
    keys
}

#[no_mangle]
pub extern fn eval2(keys: Keys, alpha: u32) -> u32 {
    let aes_keys: [u128; 4] = bincode::deserialize(&keys.aes_keys[..]).unwrap();
    let mut prg = Mmo::from_slice(&aes_keys);
    let k_a: DPFKeyAlpha1 = bincode::deserialize(&keys.key_a[..]).unwrap();
    let k_b: DPFKeyAlpha1 = bincode::deserialize(&keys.key_b[..]).unwrap();

    let t_a_output = k_a.eval(&mut prg, 0, alpha);
    let t_b_output = k_b.eval(&mut prg, 1, alpha);
    let t_output = t_a_output.wrapping_add(t_b_output);
    t_output
}

#[no_mangle]
pub extern fn eval_dcf(key: Box<Vec<u8>>, aes_keys_pointer: Box<Vec<u8>>, alpha: u32, party_id: u8) -> u32 {
    let path = format!("C:\\Users\\דורין\\Desktop\\FinalProject\\MPC\\logdcf{party_id}.txt");
    let mut res = File::create(path);
    let aes_keys: [u128; 4] = bincode::deserialize(&aes_keys_pointer[..]).unwrap();
    let k: DIFKeyAlpha1 = bincode::deserialize(&key[..]).unwrap();
    let mut prg = Mmo::from_slice(&aes_keys);
    let t_output = k.eval(&mut prg, party_id, alpha);
    match res {
        Ok(ref mut file) =>
            file.write_all(format!("Abc {t_output}").as_bytes()),
        Err(e) => return 1,
    };
    t_output
}
// ------------------- End of added code ------------------- 


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
