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

[<Test>]
let ShouldEncryptText () =
    // Arrange
    let text = "Hello World"
    let encryptionKey = 10

    // Act
    let encryptedText = xorEncrypt(text, encryptionKey)

    // Assert
    encryptedText |> should not' (equal null)
    encryptedText |> should not' (equal text)
    encryptedText.StartsWith("v2:") |> should equal true

[<Test>]
let ShouldDecryptEncryptedText () =
    // Arrange
    let text = "Hello World"
    let encryptionKey = 10

    // Act
    let encryptedText = xorEncrypt(text, encryptionKey)
    let decryptedText = xorDecrypt(encryptedText, encryptionKey)

    // Assert
    encryptedText |> should not' (equal null)
    encryptedText |> should not' (equal text)
    decryptedText |> should equal text

[<Test>]
let ShouldDecryptLegacyEncryptedText () =
    // Arrange
    let text = "Hello World"
    let encryptionKey = 10

    let legacyEncryptedText =
        [ for i = 0 to text.Length - 1 do
              let character = text.[i]
              let encryptedCharCode = int character + encryptionKey
              let encryptedChar = char encryptedCharCode
              yield string encryptedChar ]
        |> String.concat ""

    // Act
    let decryptedText = xorDecrypt(legacyEncryptedText, encryptionKey)

    // Assert
    decryptedText |> should equal text



