module Repo
open System.Data.SQLite
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

let createDb() = 
    let dbFileName = "sample.db"
    let connectionString = sprintf "Data Source=%s;Version=3;" dbFileName
    SQLiteConnection.CreateFile dbFileName |> ignore
    let connection = new SQLiteConnection(connectionString)
    connection.Open();

    let createTableQuery = "CREATE TABLE IF NOT EXISTS Records (Name TEXT, Description TEXT)"
    let structureCommand = new SQLiteCommand(createTableQuery, connection)
    structureCommand.ExecuteNonQuery() |> ignore

    connection.Close()

let getCategories() =
    let dbFileName = "sample.db"
    let connectionString = sprintf "Data Source=%s;Version=3;" dbFileName
    let connection = new SQLiteConnection(connectionString)
    connection.Open()

    let query = "SELECT distinct Category FROM Records"
    let command = new SQLiteCommand(query, connection)
    let result = command.ExecuteReader()

    let mutable categories = []
    while result.Read() do
        let category = result.GetString(0)
        categories <- category :: categories

    connection.Close()
    categories

let returnTestData() = 
    let x = {Title = "GitHub"; Username = "user"; Password = "P@ssword"; Url = "https://github.com"; Notes = "Notes"; Category = "Social Media"; CreationDate = DateTime.Now; LastModifiedDate = DateTime.Now}
    let y = {Title = "Facebook"; Username = "user"; Password = "P@ssword"; Url = "https://facebook.com"; Notes = "Notes"; Category = "Social Media";  CreationDate = DateTime.Now; LastModifiedDate = DateTime.Now}
    let z = {Title = "Twitter"; Username = "user"; Password = "P@ssword"; Url = "https://twitter.com"; Notes = "Notes"; Category = "Social Media";  CreationDate = DateTime.Now; LastModifiedDate = DateTime.Now}
    [x; y; z]
    



