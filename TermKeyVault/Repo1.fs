﻿module Repo1

open System
open Types
open System.Data.SQLite

let createDb(password: string) = 
    let dbFileName = "sample.db"
    if System.IO.File.Exists(dbFileName) then
        ()
    else
        let connectionString = sprintf "Data Source=%s;Version=3;" dbFileName
        SQLiteConnection.CreateFile dbFileName |> ignore
        let connection = new SQLiteConnection(connectionString)
        connection.Open();
        connection.ChangePassword(password)

        let createTableQuery = "Create Table Records (
            Id INTEGER  primary key autoincrement,
            Title varchar(255),
            Username varchar(255),
            Password varchar(255),
            Url varchar(255),
            Notes varchar(255),
            Category varchar(255),
            CreationDate datetime,
            LastModifiedDate datetime)"

        let structureCommand = new SQLiteCommand(createTableQuery, connection)
        structureCommand.ExecuteNonQuery() |> ignore

        connection.Close()


let createRecord(record: Record) = 
    let dbFileName = "sample.db"
    let connectionString = sprintf "Data Source=%s;Version=3;Password=dupa;Encryption=SQLiteCrypt" dbFileName
    let connection = new SQLiteConnection(connectionString)
    connection.Open()

    let query = sprintf "INSERT INTO Records (
        Title,
        Username,
        Password,
        Url,
        Notes,
        Category,
        CreationDate,
        LastModifiedDate)
        VALUES ('%s', '%s', '%s', '%s', '%s', '%s', '%s', '%s')" record.Title record.Username record.Password record.Url record.Notes record.Category (record.CreationDate.ToString()) (record.LastModifiedDate.ToString())

    let command = new SQLiteCommand(query, connection)
    command.ExecuteNonQuery() |> ignore

    connection.Close()

let getRecords() =
    let dbFileName = "sample.db"
    let connectionString = sprintf "Data Source=%s;Version=3;Password=dupa;" dbFileName
    let connection = new SQLiteConnection(connectionString)
    connection.Open()

    let query = "SELECT * FROM Records"
    let command = new SQLiteCommand(query, connection)
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

let getRecordsByCategory(category: string) = 
    let dbFileName = "sample.db"
    let connectionString = sprintf "Data Source=%s;Version=3;Password=dupa;" dbFileName
    let connection = new SQLiteConnection(connectionString)
    connection.Open()

    let query = sprintf "SELECT * FROM Records WHERE Category = '%s'" category
    let command = new SQLiteCommand(query, connection)
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

let getCategories() =
    let dbFileName = "sample.db"
    let connectionString = sprintf "Data Source=%s;Version=3;Password=dupa;" dbFileName
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
    let x = {Id = 1; Title = "GitHub"; Username = "user"; Password = "P@ssword"; Url = "https://github.com"; Notes = "Notes"; Category = "Social Media"; CreationDate = DateTime.Now; LastModifiedDate = DateTime.Now}
    let y = {Id = 2; Title = "Facebook"; Username = "user"; Password = "P@ssword"; Url = "https://facebook.com"; Notes = "Notes"; Category = "Social Media";  CreationDate = DateTime.Now; LastModifiedDate = DateTime.Now}
    let z = {Id = 3; Title = "Twitter"; Username = "user"; Password = "P@ssword"; Url = "https://twitter.com"; Notes = "Notes"; Category = "Social Media";  CreationDate = DateTime.Now; LastModifiedDate = DateTime.Now}
    [x; y; z]

let insertTestData() = 
    let testData = returnTestData()
    testData |> List.iter (fun item -> createRecord(item))



