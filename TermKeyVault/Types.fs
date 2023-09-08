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
    length: int
    numbers: bool
    uppercase: bool
    lowercase: bool
    special: bool
    excludeSimilar: bool
}
