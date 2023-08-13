﻿module Types

open System

type Record = {
    Title: string
    Username: string
    Password: string
    Url: string
    Notes: string 
    Category: string
    CreationDate: DateTime 
    LastModifiedDate: DateTime
} 
