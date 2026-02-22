module Cryptography

open System.Security.Cryptography
open System.Text
open System

open Types

let private encryptionPrefix = "v2:"

let private getAesKeyBytes (encryptionKey: int) =
    let keyBytes = Encoding.UTF8.GetBytes(encryptionKey.ToString())
    SHA256.HashData(keyBytes)

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

let generatePassword (parameters: PasswordParams) : string =
    let lowerCase = [ 'a' .. 'z' ]
    let upperCase = [ 'A' .. 'Z' ]
    let special = "!@#$%^&*()_+-=[]{};':,./<>?`~" |> Seq.toList
    let numbers = "0123456789" |> Seq.toList
    let simmilarCharacters = "il1Lo0O" |> Seq.toList

    let selectedCharacters =
        List.concat
            [ if parameters.Lowercase then lowerCase else []
              if parameters.Uppercase then upperCase else []
              if parameters.Special then special else []
              if parameters.Numbers then numbers else [] ]

    let filteredCharacters =
        if parameters.ExcludeSimilar then
            selectedCharacters
            |> List.filter (fun c -> not (simmilarCharacters |> List.contains c))
        else
            selectedCharacters

    let random = Random()

    let password =
        List.init parameters.Length (fun _ ->
            let index = random.Next(0, filteredCharacters.Length)
            filteredCharacters.[index])
        |> List.map string
        |> String.concat ""

    password

let private legacyShiftEncrypt (text: string, encryptionKey: int) : string =
    [ for i = 0 to text.Length - 1 do
          let character = text.[i]
          let encryptedCharCode = int character + encryptionKey
          let encryptedChar = char encryptedCharCode
          yield string encryptedChar ]
    |> String.concat ""

let xorEncrypt (text: string, encryptionKey: int) : string =
    if String.IsNullOrEmpty(text) then
        text
    else
        let plainBytes = Encoding.UTF8.GetBytes(text)
        let encryptedBytes = Array.zeroCreate<byte> plainBytes.Length
        let tag = Array.zeroCreate<byte> 16
        let nonce = RandomNumberGenerator.GetBytes(12)
        let keyBytes = getAesKeyBytes encryptionKey

        use aes = new AesGcm(keyBytes, 16)
        aes.Encrypt(nonce, plainBytes, encryptedBytes, tag)

        let payload = Array.concat [ nonce; tag; encryptedBytes ]
        encryptionPrefix + Convert.ToBase64String(payload)

let xorDecrypt (text: string, encryptionKey: int) : string =
    if String.IsNullOrEmpty(text) then
        text
    elif text.StartsWith(encryptionPrefix, StringComparison.Ordinal) then
        try
            let payload = text.Substring(encryptionPrefix.Length) |> Convert.FromBase64String

            if payload.Length < 28 then
                // Invalid or truncated payload; return the original text instead of throwing.
                text
            else
                let nonce = payload.[0..11]
                let tag = payload.[12..27]
                let encryptedBytes = payload.[28..]
                let plainBytes = Array.zeroCreate<byte> encryptedBytes.Length
                let keyBytes = getAesKeyBytes encryptionKey

                use aes = new AesGcm(keyBytes, 16)
                aes.Decrypt(nonce, encryptedBytes, tag, plainBytes)
                Encoding.UTF8.GetString(plainBytes)
        with
        | :? FormatException
        | :? CryptographicException
        | :? ArgumentException ->
            // Any error during decoding/decryption results in returning the original text
            // to avoid crashing UI call sites on corrupted/partial data.
            text
    else
        legacyShiftEncrypt (text, -encryptionKey)

