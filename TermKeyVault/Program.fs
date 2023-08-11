open Terminal.Gui
open System.Data
open FSharp.Data

let loginWindow =
    new Window(
        Title = "Login",
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill()
    )

let mainWindow = 
    new Window(
        Title = "TermKeyVault",
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill()
    )

let table = 
    new TableView(
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        FullRowSelect = true
    )

type record = {
    Name: string
    Description: string
} 

// init list of record
let x = {Name = "GitHub"; Description = "P@ssword"}
let y = {Name = "Facebook"; Description = "P@ssword"}

let list = [x; y]

let convertListToDataTable(list: List<record>) =
    let table = new DataTable()
    table.Columns.Add("Name") |> ignore
    table.Columns.Add("Description") |> ignore
    list |> List.iter (fun item -> 
        let row = table.NewRow()
        row.[0] <- item.Name
        row.[1] <- item.Description
        table.Rows.Add(row) |> ignore
    )
    table

let action (e: TableView.CellActivatedEventArgs) = 
    let row = e.Row
    let name = e.Table.Rows[row][0]
    match name with
    | null -> ()
    | string -> 
        //Application.Top.RemoveAll();
        //let imageWindow = Me
        //Application.Top.Add()
        MessageBox.Query("Test", "Test")
        |> ignore

table.add_CellActivated(action)
table.Table <- convertListToDataTable(list)

mainWindow.Add(table)

[<EntryPoint>]
let initApp _ = 
    Application.Init()
    Application.Run(mainWindow)
    Application.Shutdown();
    0 
