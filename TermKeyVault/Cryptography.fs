module Cryptography

open System.Security.Cryptography
open System.Text
open System

let generateSalt (saltSize: int) =
    let salt = Array.zeroCreate<byte> saltSize
    use rng = RandomNumberGenerator.Create()
    rng.GetBytes(salt)
    salt

let hashPassword (password: string) (salt: byte[]) =
    let combinedData = Encoding.UTF8.GetBytes(password) |> Array.append salt
    let hashedBytes = SHA256.Create().ComputeHash(combinedData)
    BitConverter.ToString(hashedBytes).Replace("-", "")

let verifyPassword (enteredPassword: string) (storedHash: string) (salt: byte[]) =
    let hashedEnteredPassword = hashPassword enteredPassword salt
    hashedEnteredPassword = storedHash
