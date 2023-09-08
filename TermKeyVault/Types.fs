module Types

open System

type Record = {
    Id: int
    Title: string
    Username: string
    Password: string
    Url: string
    Notes: string 
    Category: string
    CreationDate: DateTime 
    LastModifiedDate: DateTime
} 

type PasswordParams = {
    Length: int
    Numbers: bool
    Uppercase: bool
    Lowercase: bool
    Special: bool
    ExcludeSimilar: bool
}
