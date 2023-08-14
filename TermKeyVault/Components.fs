module Components

open Terminal.Gui
open System.Data
open System

open Types
open Repo
open Cryptography

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

let showRecordDialog() = 
    let dialog = new Dialog("Add record", 60, 20)

    let record = {
        Title = ""
        Username = ""
        Password = ""
        Url = ""
        Notes = ""
        Category = ""
        CreationDate = DateTime.Now 
        LastModifiedDate = DateTime.Now 
    }

    (* Title *)
    let titleLabel = new Label(
        Text = "Title: ",
        X = 0,
        Y = 1
    )

    let titleTextField = new TextField(
        Text = record.Title,
        X = Pos.Right(titleLabel),
        Y = Pos.Top(titleLabel),
        Width = Dim.Fill()
    )

    let usernameLabel = new Label(
        Text = "Username: ",
        X = 0,
        Y = Pos.Bottom(titleLabel)
    )

    let usernameTextField = new TextField(
        Text = record.Username,
        X = Pos.Right(usernameLabel),
        Y = Pos.Top(usernameLabel),
        Width = Dim.Fill()
    )

    let passwordLabel = new Label(
        Text = "Password: ",
        X = 0,
        Y = Pos.Bottom(usernameLabel)
    )

    let passwordTextField = new TextField(
        Text = record.Password,
        X = Pos.Right(passwordLabel),
        Y = Pos.Top(passwordLabel),
        Width = Dim.Fill()
    )

    let urlLabel = new Label(
        Text = "Url: ",
        X = 0,
        Y = Pos.Bottom(passwordLabel)
    )

    let urlTextField = new TextField(
        Text = record.Url,
        X = Pos.Right(urlLabel),
        Y = Pos.Top(urlLabel),
        Width = Dim.Fill()
    )

    let notesLabel = new Label(
        Text = "Notes: ",
        X = 0,
        Y = Pos.Bottom(urlLabel)
    )

    let notesTextField = new TextField(
        Text = record.Notes,
        X = Pos.Right(notesLabel),
        Y = Pos.Top(notesLabel),
        Width = Dim.Fill()
    )

    dialog.Add(titleLabel, titleTextField,
               usernameLabel, usernameTextField,
               passwordLabel, passwordTextField,
               urlLabel, urlTextField,
               notesLabel, notesTextField)

    (* Exit button *)
    let exitButton = new Button("Exit", true)
    exitButton.add_Clicked (fun _ -> Application.RequestStop(dialog))

    (* Create button *)
    let createButton = new Button("Create", true)
    createButton.add_Clicked(fun _ -> 

        let salt = generateSalt 32
        let enteredPassword = passwordTextField.Text
        let hashedEnteredPassword =
            enteredPassword
            |> fun password ->
                hashPassword (password |> string) salt
            |> string

        let updatedRecord = {
            record with
                Title = titleTextField.Text |> string
                Username = usernameTextField.Text |> string
                Password = hashedEnteredPassword
                Url = urlTextField.Text |> string
                Notes = notesTextField.Text |> string
                CreationDate = DateTime.Now
                LastModifiedDate = DateTime.Now
        }

        createRecord(updatedRecord)
        categoryTable.Table <- convertListToDataTableCategory(Repo.getCategories())
        recordTable.Table <- convertListToDataTable(Repo.getRecords())

        categoryTable.Table <- convertListToDataTableCategory(Repo.getCategories())
        recordTable.Table <- convertListToDataTable(Repo.getRecords())
        Application.RequestStop(dialog)
    )

    dialog.AddButton(createButton)
    dialog.AddButton(exitButton)
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

categoryTable.add_SelectedCellChanged(fun e -> 
    let row = e.NewRow
    let name = e.Table.Rows[row][0]
    recordTable.Table <- convertListToDataTable(Repo.getRecordsByCategory(name.ToString()))
)

let mainWindow = 
    let window = new Window(
        Title = "TermKeyVault",
        X = 0,
        Y = 1,
        Width = Dim.Fill(),
        Height = Dim.Fill()
    )
    window.Add(categoryTable)
    window.Add(recordTable)
    window.Add(frameView)
    window

let switchWindow(newWindow: Window) (showMenu: bool) = 
    Application.Top.RemoveAll();
    Application.Top.Add(newWindow)
    if showMenu then
        Application.Top.Add(menu)

(* Login Window *)
let loginLabel = new Label(
    Text = "Enter Master Password: ",
    X = Pos.Center(),
    Y = Pos.Center()
)

let passwordField = 
    let field = new TextField(
        X = Pos.Center(),
        Y = Pos.Bottom(loginLabel),
        Width = 30,
        Secret = true
    )
    field.add_KeyPress(fun e -> 
        if (e.KeyEvent.Key = Key.Enter) then
            e.Handled <- true
            let salt = generateSalt 32
            let enteredPassword = field.Text
            let hashedEnteredPassword =
                enteredPassword
                |> fun password ->
                    hashPassword (password |> string) salt
                |> string

            let masterPassword = "dupa"
            if (masterPassword = "dupa") then
                switchWindow mainWindow true
            else
                field.Text <- ""
                MessageBox.ErrorQuery("Error", "Wrong password", "Ok") |> ignore
    )
    field

let loginWindow =
    let window = new Window(
        Title = "Login",
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill()
    )
    window.Add(loginLabel)
    window.Add(passwordField)
    window








