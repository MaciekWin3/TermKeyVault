module Repo

open Types
open Microsoft.Data.Sqlite
open System
open System.IO

let dbPath =     
    let appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
    let configDir = Path.Combine(appDataPath, "termkeyvault")
    let dbPath = Path.Combine(configDir, "termkeyvault.db")
    dbPath

let checkIfDbExists() =
    File.Exists(dbPath)

let connectionString(file: string, password: string)= sprintf "Data Source=file:%s;Password=%s;" file password

let checkPassword(password: string) =
    let connection = new SqliteConnection(connectionString(dbPath, password))
    try
        connection.Open()
        connection.Close()
        true
    with
    | _ -> false

let prepareDb(password: string) =
    let connection = new SqliteConnection(connectionString(dbPath, password))
    connection.Open()
    let command = connection.CreateCommand()
    try
        // TODO: Make title unique
        command.CommandText <- "Create Table Records (
            Id INTEGER  primary key autoincrement,
            Title varchar(255), 
            Username varchar(255),
            Password varchar(255),
            Url varchar(255),
            Notes varchar(255),
            Category varchar(255),
            CreationDate datetime,
            LastModifiedDate datetime)"
        command.ExecuteNonQuery() |> ignore
    with
    | _ -> ()
    connection.Close()

let createRecord(record: Record) =
    let connection = new SqliteConnection(connectionString(dbPath, "test"))
    connection.Open()
    let command = connection.CreateCommand()
    command.CommandText <- sprintf "INSERT INTO Records (
        Title,
        Username,
        Password,
        Url,
        Notes,
        Category,
        CreationDate,
        LastModifiedDate)
        VALUES ('%s', '%s', '%s', '%s', '%s', '%s', '%s', '%s')" record.Title record.Username record.Password record.Url record.Notes record.Category (record.CreationDate.ToString()) (record.LastModifiedDate.ToString())
    command.ExecuteNonQuery() |> ignore
    connection.Close()

let getRecords() =
    let connection = new SqliteConnection(connectionString(dbPath, "test"))
    connection.Open()
    let command = connection.CreateCommand()
    command.CommandText <- "SELECT * FROM Records"
    let result = command.ExecuteReader()
    let mutable records = []
    while result.Read() do
        let id = result.GetInt32(0)
        let title = result.GetString(1)
        let username = result.GetString(2)
        let password = result.GetString(3)
        let url = result.GetString(4)
        let notes = result.GetString(5)
        let category = result.GetString(6)
        let creationDate = result.GetString(7)
        let lastModifiedDate = result.GetString(8)
        let record = {Id = id; Title = title; Username = username; Password = password; Url = url; Notes = notes; Category = category; CreationDate = DateTime.Parse(creationDate); LastModifiedDate = DateTime.Parse(lastModifiedDate)}
        records <- record :: records

    connection.Close()
    records

let getRecordsByCategory(category: string)  =
    let connection = new SqliteConnection(connectionString(dbPath, "test"))
    connection.Open()
    let command = connection.CreateCommand()
    command.CommandText <- sprintf "SELECT * FROM Records WHERE Category = '%s'" category
    let result = command.ExecuteReader()
    let mutable records = []
    while result.Read() do
        let id = result.GetInt32(0)
        let title = result.GetString(1)
        let username = result.GetString(2)
        let password = result.GetString(3)
        let url = result.GetString(4)
        let notes = result.GetString(5)
        let category = result.GetString(6)
        let creationDate = result.GetString(7)
        let lastModifiedDate = result.GetString(8)
        let record = {Id = id; Title = title; Username = username; Password = password; Url = url; Notes = notes; Category = category; CreationDate = DateTime.Parse(creationDate); LastModifiedDate = DateTime.Parse(lastModifiedDate)}
        records <- record :: records

    connection.Close()
    records

let getRecordByTitle(title: string): Record option =
    let connection = new SqliteConnection(connectionString(dbPath, "test"))
    connection.Open()
    let command = connection.CreateCommand()
    command.CommandText <- sprintf "SELECT * FROM Records WHERE Title = '%s'" title
    let reader = command.ExecuteReader()
    
    let record =
        if reader.Read() then
            let id = reader.GetInt32(0)
            let title = reader.GetString(1)
            let username = reader.GetString(2)
            let password = reader.GetString(3)
            let url = reader.GetString(4)
            let notes = reader.GetString(5)
            let category = reader.GetString(6)
            let creationDate = reader.GetString(7)
            let lastModifiedDate = reader.GetString(8)
            
            Some {
                Id = id
                Title = title
                Username = username
                Password = password
                Url = url
                Notes = notes
                Category = category
                CreationDate = DateTime.Parse(creationDate)
                LastModifiedDate = DateTime.Parse(lastModifiedDate)
            }
        else
            None

    connection.Close()
    record

let getCategories() = 
    let connection = new SqliteConnection(connectionString(dbPath, "test"))
    connection.Open() 
    let command = connection.CreateCommand()
    command.CommandText <- "SELECT DISTINCT Category FROM Records"
    let result = command.ExecuteReader()

    let mutable categories = []
    while result.Read() do
        let category = result.GetString(0)
        categories <- category :: categories

    connection.Close()
    categories

let deleteRecord(title: string) = 
    let connection = new SqliteConnection(connectionString(dbPath, "test"))
    connection.Open()
    let command = connection.CreateCommand()
    command.CommandText <- sprintf "DELETE FROM Records WHERE Title = '%s'" title
    command.ExecuteNonQuery() |> ignore
    connection.Close();

let returnTestData() = 
    let x = {Id = 1; Title = "GitHub"; Username = "user"; Password = "P@ssword"; Url = "https://github.com"; Notes = "Notes"; Category = "Social Media"; CreationDate = DateTime.Now; LastModifiedDate = DateTime.Now}
    let y = {Id = 2; Title = "Facebook"; Username = "user"; Password = "P@ssword"; Url = "https://facebook.com"; Notes = "Notes"; Category = "Social Media";  CreationDate = DateTime.Now; LastModifiedDate = DateTime.Now}
    let z = {Id = 3; Title = "Twitter"; Username = "user"; Password = "P@ssword"; Url = "https://twitter.com"; Notes = "Notes"; Category = "Social Media";  CreationDate = DateTime.Now; LastModifiedDate = DateTime.Now}
    [x; y; z]

let insertTestData() = 
    let testData = returnTestData()
    testData |> List.iter (fun item -> createRecord(item))




