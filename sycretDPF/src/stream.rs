pub trait FSSKey: Sized {
    fn eval(&self, prg: &mut impl Prg, party_id: u8, x: u32) -> u32;

    fn generate_keypair(prg: &mut impl Prg) -> (Self, Self);
}

pub trait RawKey: Sized {
    const KEY_LEN: usize;

    /// # Safety
    /// De-referencing raw pointer
    unsafe fn from_raw_line(raw_line_pointer: *const u8) -> Self;

    /// # Safety
    /// De-referencing raw pointer
    unsafe fn to_raw_line(&self, raw_line_pointer: *mut u8);
}

// Keyed Prg
pub trait Prg {
    fn from_slice(key: &[u128]) -> Self;

    fn from_vec(key: &[u128]) -> Self;

    // NOTE: Rust Stable does not have const generics
    // const expansion_factor: usize;
    // fn expand(&mut self, seed: u128) -> [u128; Self::expansion_factor];
    fn expand(&mut self, seed: u128) -> Vec<u128>;

    // TODO: key type, read/write state to line
}