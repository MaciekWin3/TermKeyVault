module CryptographyTests

open NUnit.Framework
open FsUnit

open Types
open Cryptography

[<Test>]
let ShouldGeneratePassword () =
    // Arrange
    let passwordParams : PasswordParams = {
        Length = 10
        Numbers = true
        Uppercase = true
        Lowercase = true
        Special = true
        ExcludeSimilar = true
    }

    // Act
    let password = generatePassword passwordParams 

    // Assert
    password |> should not' (equal null)
    password.Length |> should equal 10
