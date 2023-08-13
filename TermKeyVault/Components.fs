module Components

open Terminal.Gui
open System.Data
open Repo

let categoryTable = 
    new TableView(
        X = 0,
        Y = 0,
        Width = Dim.Percent(25f),
        Height = Dim.Percent(70f),
        FullRowSelect = true
    )

categoryTable.Style.AlwaysShowHeaders <- true

let recordTable = 
    new TableView(
        X = Pos.Right(categoryTable),
        Y = 0,
        Width = Dim.Percent(75f),
        Height = Dim.Percent(70f),
        FullRowSelect = true
    )

let frameView =
    new FrameView(
        X = 0,
        Y = Pos.Bottom(categoryTable),
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        Title = "Details"
    )

recordTable.Style.AlwaysShowHeaders <- true

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

let convertListToDataTableCategory(list: List<Record>) =
    let table = new DataTable()
    table.Columns.Add("Category") |> ignore
    list |> List.iter (fun item -> 
        let row = table.NewRow()
        row.[0] <- item.Category
        table.Rows.Add(row) |> ignore
    )
    table

let openFileDialog() = 
    // make it cross platform
    let dialog = new OpenDialog("Open", "Open a file")
    dialog.DirectoryPath <- "/home"
    Application.Run dialog |> ignore
    dialog.FilePath |> ignore


let menu = 
    new MenuBar(
        [|
            MenuBarItem ("File",
                [| MenuItem ("Open", "", (fun () -> openFileDialog())) 
                   MenuItem ("Create", "", (fun () -> Application.RequestStop ())) |]);
            MenuBarItem ("Tools",
                [| MenuItem ("Password generator", "", Unchecked.defaultof<_>)
                   MenuItem ("Paste", "", Unchecked.defaultof<_>) |])
            MenuBarItem ("Help",
                [| MenuItem ("About", "", Unchecked.defaultof<_>)
                   MenuItem ("Website", "", Unchecked.defaultof<_>) |])
        |])



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


let showContextMenu(screenPoint: Point, id: string) = 
    let contextMenu = new ContextMenu(0,0,
        MenuBarItem ("File",
            [| 
                MenuItem ("Inspect", "", (fun () -> openFileDialog())) 
                MenuItem ("Edit", "", (fun () -> Application.RequestStop ()))
            |]))

    contextMenu.Show() 

recordTable.add_CellActivated(action)
recordTable.Table <- convertListToDataTable(Repo.returnTestData())
recordTable.add_MouseClick(fun e -> 
    if (e.MouseEvent.Flags.HasFlag(MouseFlags.Button3Clicked)) then
        recordTable.SetSelection(1, e.MouseEvent.Y - 3, false);
        try
            let id = string recordTable.Table.Rows[e.MouseEvent.Y - 3].[0]
            showContextMenu(Point(e.MouseEvent.X + recordTable.Frame.X + 5, e.MouseEvent.Y + recordTable.Frame.Y + 5), id)
            e.Handled <- true
        with
        | _ -> ()
)
categoryTable.Table <- convertListToDataTableCategory(Repo.returnTestData())

recordTable.add_SelectedCellChanged(fun e -> 
    let row = e.NewRow
    let name = e.Table.Rows[row][0]
    frameView.Text <- name.ToString()
)
