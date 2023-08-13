module Components

open Terminal.Gui
open System.Data
open Types

let convertListToDataTable(list: List<Record>) =
    let table = new DataTable()
    table.Columns.Add("Title") |> ignore
    table.Columns.Add("Username") |> ignore
    table.Columns.Add("Password") |> ignore
    table.Columns.Add("Category") |> ignore
    table.Columns.Add("Url") |> ignore
    table.Columns.Add("Notes") |> ignore
    list |> List.iter (fun item -> 
        let row = table.NewRow()
        row.[0] <- item.Title
        row.[1] <- item.Username
        row.[2] <- item.Password
        row.[3] <- item.Category
        row.[4] <- item.Url
        row.[5] <- item.Notes
        table.Rows.Add(row) |> ignore
    )
    table

let convertListToDataTableCategory(list: List<string>) =
    let table = new DataTable()
    table.Columns.Add("Category") |> ignore
    list |> List.iter (fun item -> 
        let row = table.NewRow()
        row.[0] <- item
        table.Rows.Add(row) |> ignore
    )
    table

let action (e: TableView.CellActivatedEventArgs) = 
    let row = e.Row
    let name = e.Table.Rows[row][0]
    match name with
    | null -> ()
    | _ -> 
        MessageBox.Query("Test", "Test")
        |> ignore


let openFileDialog() = 
    let dialog = new OpenDialog("Open", "Open a file")
    dialog.DirectoryPath <- "/home"
    Application.Run dialog |> ignore
    dialog.FilePath |> ignore

let showRecordDialog() = 
    let dialog = new Dialog("Add record", 60, 20)
    Application.Run(dialog) 

(* MenuBar *)
let menu = 
    new MenuBar(
        [|
            MenuBarItem ("File",
                [| MenuItem ("Open", "", (fun () -> openFileDialog())) 
                   MenuItem ("Create", "", (fun () -> Application.RequestStop ())) |]);
            MenuBarItem ("Tools",
                [| MenuItem ("Password generator", "", Unchecked.defaultof<_>)
                   MenuItem ("Paste", "", Unchecked.defaultof<_>) |])
            MenuBarItem ("Records",
                [| MenuItem ("Add record", "", (fun () -> showRecordDialog()))
                   MenuItem ("Paste", "", Unchecked.defaultof<_>) |])
            MenuBarItem ("Help",
                [| MenuItem ("About", "", Unchecked.defaultof<_>)
                   MenuItem ("Website", "", Unchecked.defaultof<_>) |])
        |])

(* Context Menu *)
let showContextMenu(screenPoint: Point, id: string) = 
    let contextMenu = new ContextMenu(screenPoint.X, screenPoint.Y,
        MenuBarItem ("File",
            [| 
                MenuItem ("Inspect", "", (fun () -> openFileDialog())) 
                MenuItem ("Edit", "", (fun () -> Application.RequestStop ()))
            |]))

    contextMenu.Show() 

(* Category table *)
let categoryTable = 
    let table = new TableView(
        X = 0,
        Y = 0,
        Width = Dim.Percent(25f),
        Height = Dim.Percent(70f),
        FullRowSelect = true
    )
    table.Style.AlwaysShowHeaders <- true
    table.Table <- convertListToDataTableCategory(Repo.getCategories())
    table

(* Deaitls frame *)
let frameView =
    new FrameView(
        X = 0,
        Y = Pos.Bottom(categoryTable),
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        Title = "Details"
    )

(* Record table *)
let recordTable = 
    let table = new TableView(
        X = Pos.Right(categoryTable),
        Y = 0,
        Width = Dim.Percent(75f),
        Height = Dim.Percent(70f),
        FullRowSelect = true
    )
    table.Style.AlwaysShowHeaders <- true
    table.add_CellActivated(action)
    table.Table <- convertListToDataTable(Repo.getRecords())
    table.add_MouseClick(fun e -> 
        if (e.MouseEvent.Flags.HasFlag(MouseFlags.Button3Clicked)) then
            table.SetSelection(1, e.MouseEvent.Y - 3, false);
            try
                let id = string table.Table.Rows[e.MouseEvent.Y - 3].[0]
                showContextMenu(Point(e.MouseEvent.X + table.Frame.X + 2, e.MouseEvent.Y + table.Frame.Y + 2), id)
                e.Handled <- true
            with
            | _ -> ()
    )

    table.add_SelectedCellChanged(fun e -> 
        let row = e.NewRow
        let name = e.Table.Rows[row][0]
        frameView.Text <- name.ToString()
    )
    table

categoryTable.add_SelectedCellChanged(fun e -> 
    let row = e.NewRow
    let name = e.Table.Rows[row][0]
    recordTable.Table <- convertListToDataTable(Repo.getRecordsByCategory(name.ToString()))
)





