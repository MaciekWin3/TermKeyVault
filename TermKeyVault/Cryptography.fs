module Cryptography

open System.Security.Cryptography
open System.Text
open System

open Types

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
