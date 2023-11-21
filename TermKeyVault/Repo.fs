module Repo

open Microsoft.Data.Sqlite
open System
open System.IO

open Cryptography
open Utils
open Utils.Configuration 
open Types
open Terminal.Gui

let dbPath () =
    let config = Configuration.getConfig ()
    config.DatabasePath

let getDbPasswordFromCache() = 
    let cacheValue = Cache.getValueFromCache("password")
    if cacheValue.IsNone then
        (*
            TODO
            let password = askForPassword ()
            Cache.addValueToCache("password", password)
            password
        *)
        MessageBox.ErrorQuery("Error", "Password not found in cache", "Ok") |> ignore
        ""
    else
        xorDecrypt(cacheValue.Value, getEncryptionKey ()) 

let connectionString (file: string, password: string) =
    sprintf "Data Source=file:%s;Password=%s;" file password

let checkPassword (password: string) =
    let connection = new SqliteConnection(connectionString (dbPath (), password))

    try
        connection.Open()
        let tableName = "Records"
        let commandText = sprintf "SELECT name FROM sqlite_master WHERE type='table' AND name='%s'" tableName
        let command = new SqliteCommand(commandText, connection)
        let reader = command.ExecuteReader()
        let tableExists = reader.HasRows
        connection.Close()
        true
    with _ ->
        false

let checkIfDbExists () =
    let dbPath = dbPath ()
    if File.Exists(dbPath) && dbPath.EndsWith(".db") then
        true
    else
        false

let checkIfDbIsValid(password: string): bool =
    use connection = new SqliteConnection(connectionString (dbPath (), password))
    let isValid = 
        try
            connection.Open()
            let tableName = "Records"
            let commandText = sprintf "SELECT name FROM sqlite_master WHERE type='table' AND name='%s'" tableName
            let command = new SqliteCommand(commandText, connection)
            let reader = command.ExecuteReader()
            reader.HasRows
        with
        | _ -> false

    connection.Close()
    isValid

let prepareDb (password: string) =
    let connection = new SqliteConnection(connectionString (dbPath (), password))
    connection.Open()
    let command = connection.CreateCommand()

    try
        command.CommandText <-
            "Create Table Records (
            Id INTEGER  primary key autoincrement,
            Title varchar(255) UNIQUE, 
            Username varchar(255),
            Password varchar(255),
            Url varchar(255),
            Notes varchar(255),
            Category varchar(255),
            CreationDate datetime,
            LastModifiedDate datetime)"

        command.ExecuteNonQuery() |> ignore
    with _ ->
        ()

    connection.Close()

let createRecord (record: Record) =
    use connection = new SqliteConnection(connectionString (dbPath (), getDbPasswordFromCache()))
    connection.Open()
    let command = connection.CreateCommand()

    command.CommandText <-
        sprintf
            "INSERT INTO Records (
        Title,
        Username,
        Password,
        Url,
        Notes,
        Category,
        CreationDate,
        LastModifiedDate)
        VALUES ('%s', '%s', '%s', '%s', '%s', '%s', '%s', '%s')"
            record.Title
            record.Username
            record.Password
            record.Url
            record.Notes
            record.Category
            (record.CreationDate.ToString())
            (record.LastModifiedDate.ToString())

    command.ExecuteNonQuery() |> ignore
    connection.Close()

let updateRecord (title: string, updatedRecord: Record) =
    use connection = new SqliteConnection(connectionString (dbPath (), getDbPasswordFromCache()))
    connection.Open()
    let command = connection.CreateCommand()

    command.CommandText <-
        sprintf
           "UPDATE Records
            SET Title = '%s',
            Username = '%s',
            Password = '%s',
            Url = '%s',
            Notes = '%s',
            Category = '%s',
            CreationDate = '%s',
            LastModifiedDate = '%s'
            WHERE Title= '%s'"
            updatedRecord.Title
            updatedRecord.Username
            updatedRecord.Password
            updatedRecord.Url
            updatedRecord.Notes
            updatedRecord.Category
            (updatedRecord.CreationDate.ToString())
            (DateTime.Now.ToString())
            title

    command.ExecuteNonQuery() |> ignore
    connection.Close()

let getRecords () =
    use connection = new SqliteConnection(connectionString (dbPath (), getDbPasswordFromCache()))
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

        let record =
            { Id = id
              Title = title
              Username = username
              Password = password
              Url = url
              Notes = notes
              Category = category
              CreationDate = DateTime.Parse(creationDate)
              LastModifiedDate = DateTime.Parse(lastModifiedDate) }

        records <- record :: records

    connection.Close()
    records

let getRecordsByCategory (category: string) =
    use connection = new SqliteConnection(connectionString (dbPath (), getDbPasswordFromCache()))
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

        let record =
            { Id = id
              Title = title
              Username = username
              Password = password
              Url = url
              Notes = notes
              Category = category
              CreationDate = DateTime.Parse(creationDate)
              LastModifiedDate = DateTime.Parse(lastModifiedDate) }

        records <- record :: records

    connection.Close()
    records

let getRecordByTitle (title: string) : Record option =
    use connection = new SqliteConnection(connectionString (dbPath (), getDbPasswordFromCache()))
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

            Some
                { Id = id
                  Title = title
                  Username = username
                  Password = password
                  Url = url
                  Notes = notes
                  Category = category
                  CreationDate = DateTime.Parse(creationDate)
                  LastModifiedDate = DateTime.Parse(lastModifiedDate) }
        else
            None

    connection.Close()
    record

let getCategories () =
    use connection = new SqliteConnection(connectionString (dbPath (), getDbPasswordFromCache()))
    connection.Open()
    let command = connection.CreateCommand()
    command.CommandText <- "SELECT DISTINCT Category FROM Records"
    let result = command.ExecuteReader()

    let mutable categories = []

    while result.Read() do
        let category = result.GetString(0)
        categories <- category :: categories

    connection.Close()
    [ "All" ] @ categories

let deleteRecord (title: string) =
    use connection = new SqliteConnection(connectionString (dbPath (), getDbPasswordFromCache()))
    connection.Open()
    let command = connection.CreateCommand()
    command.CommandText <- sprintf "DELETE FROM Records WHERE Title = '%s'" title
    command.ExecuteNonQuery() |> ignore
    connection.Close()

let returnTestData () =
    let x =
        { Id = 1
          Title = "GitHub"
          Username = "user"
          Password = "P@ssword"
          Url = "https://github.com"
          Notes = "Notes"
          Category = "Social Media"
          CreationDate = DateTime.Now
          LastModifiedDate = DateTime.Now }

    let y =
        { Id = 2
          Title = "Facebook"
          Username = "user"
          Password = "P@ssword"
          Url = "https://facebook.com"
          Notes = "Notes"
          Category = "Social Media"
          CreationDate = DateTime.Now
          LastModifiedDate = DateTime.Now }

    let z =
        { Id = 3
          Title = "Twitter"
          Username = "user"
          Password = "P@ssword"
          Url = "https://twitter.com"
          Notes = "Notes"
          Category = "Social Media"
          CreationDate = DateTime.Now
          LastModifiedDate = DateTime.Now }

    [ x; y; z ]

let insertTestData () =
    let testData = returnTestData ()
    testData |> List.iter (fun item -> createRecord (item))

