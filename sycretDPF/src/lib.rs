#![allow(clippy::needless_range_loop)]


pub mod fss;
pub mod stream;
pub mod utils;

//------
use fss::dpf::*;
use utils::Mmo;
use crate::stream::Prg;
use rand::Rng;

use std::os::raw::c_char;
use std::ffi::CString;

use std::ffi::CStr;

// Byte precision and security.
pub const N: usize = 4;
pub const L: usize = 16;

#[repr(C)]
pub struct Keys {    
    pub aes_keys: *mut c_char,
    pub key_a: *mut c_char,
    pub key_b: *mut c_char
}

#[repr(C)]
pub struct Pair {    
    pub first: u32,
    pub second: u32,
}


//--------------

fn store_string_on_heap(string_to_store: &'static str) -> *mut c_char {
    //create a new raw pointer
    let pntr = CString::new(string_to_store).unwrap().into_raw();

    return pntr;
}

fn string_to_static_str(s: String) -> &'static str {
    Box::leak(s.into_boxed_str())
}

fn my_string_safe(ptr: *mut c_char) -> String {
    unsafe {
        CStr::from_ptr(ptr).to_string_lossy().into_owned()
    }
}

#[no_mangle]
pub extern fn gen_dpf(alpha: u32, beta: u32) -> Keys {
    let mut rng = rand::thread_rng();
    let aes_keys: [u128; 4] = rng.gen();
    let mut prg = Mmo::from_slice(&aes_keys);
    let (k_a, k_b) = DPFKeyAlpha1::generate_keypair(&mut prg, alpha, beta);

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

#[no_mangle]
pub extern fn eval_dpf(key_ptr:*mut c_char, aes_keys_ptr: *mut c_char, alpha: u32, party_id: u8) -> Pair {
    let key_str = my_string_safe(key_ptr);
    let aes_str = my_string_safe(aes_keys_ptr);

    let key: DPFKeyAlpha1 = serde_json::from_str(&key_str).unwrap();
    let aes_keys: [u128; 4] = serde_json::from_str(&aes_str).unwrap();

    let mut prg = Mmo::from_slice(&aes_keys);
    let (t_output_1, t_output_2) = key.eval(&mut prg, party_id, alpha);

    Pair{
        first: t_output_1,
        second: t_output_2
    }
}

#[no_mangle]
pub extern fn free_string(ptr: *mut c_char) {
    unsafe {
        let mut ptr_to_free = ptr;
        let _ = CString::from_raw(ptr_to_free);
        ptr_to_free = 0 as *mut c_char;
    }
}

