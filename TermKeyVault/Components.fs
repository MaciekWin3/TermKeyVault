module Components

open Terminal.Gui
open System.Data
open System

open Types
open Repo
open Cryptography
open Utils

let convertListToDataTable(list: List<Record>) =
    let table = new DataTable()
    table.Columns.Add("Title") |> ignore
    table.Columns.Add("Username") |> ignore
    table.Columns.Add("Password") |> ignore
    table.Columns.Add("Category") |> ignore
    table.Columns.Add("Url") |> ignore
    list |> List.iter (fun item -> 
        let decryptedPassword = xorDecrypt (item.Password, 32)
        let maskedPassword = new string('*', decryptedPassword.Length)
        let row = table.NewRow()
        row.[0] <- item.Title
        row.[1] <- item.Username
        row.[2] <- maskedPassword 
        row.[3] <- item.Category
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

let recordDialog (record: Record) = 
    let dialog = new Dialog(record.Title, 80, 20)
    let titleLabel = new Label(
        Text = Cryptography.xorDecrypt(record.Password, 32),
        X = 0,
        Y = 1
    )

    dialog.Add(titleLabel)
    Application.Run dialog

let action (e: TableView.CellActivatedEventArgs) = 
    let row = e.Row
    let name = e.Table.Rows.[row].[0]
    match name with
    | :? string as str ->
        let record = Repo.getRecordByTitle(str)
        match record with
        | Some record -> 
            recordDialog(record)
        | None ->
            ()
    | _ ->
        ()

let openFileDialog() = 
    let dialog = new OpenDialog("Open", "Open a file")
    dialog.DirectoryPath <- "/home"
    Application.Run dialog |> ignore
    dialog.FilePath |> ignore

(* Context Menu *)
let showContextMenu(screenPoint: Point, title: string) = 
    let contextMenu = new ContextMenu(screenPoint.X, screenPoint.Y,
        MenuBarItem ("File",
            [| 
                MenuItem ("Inspect", "", (fun () -> openFileDialog())) 
                MenuItem ("Edit", "", (fun () -> Application.RequestStop ()))
                MenuItem ("Delete", "", (fun () -> Repo.deleteRecord(title)))
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

let textFieldDetails = 
    let tv = new TextView(
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        ReadOnly = true
    )
    tv.DesiredCursorVisibility <- CursorVisibility.Invisible
    tv.ColorScheme <- Colors.Menu
    tv

(* Deaitls frame *)
let frameView =
    let fv = new FrameView(
        X = 0,
        Y = Pos.Bottom(categoryTable),
        Width = Dim.Fill(),
        Height = Dim.Fill()
    )
    fv.Add(textFieldDetails)
    fv

(* Record table *)
let recordTable = 
    let table = new TableView(
        X = Pos.Right(categoryTable),
        Y = 0,
        Width = Dim.Percent(75f),
        Height = Dim.Percent(70f),
        FullRowSelect = true
    )
    let records = Repo.getRecords()
    table.Style.AlwaysShowHeaders <- true
    table.Table <- convertListToDataTable(records)
    table.add_CellActivated(action)
    table.add_MouseClick(fun e -> 
        if (e.MouseEvent.Flags.HasFlag(MouseFlags.Button3Clicked)) then
            table.SetSelection(1, e.MouseEvent.Y - 3, false);
            try
                let title = string table.Table.Rows[e.MouseEvent.Y - 3].[0]
                showContextMenu(Point(
                    e.MouseEvent.X + table.Frame.X + 2, e.MouseEvent.Y + table.Frame.Y + 2), title)
                e.Handled <- true
            with
            | _ -> ()
    )

    table.add_SelectedCellChanged(fun e -> 
        let row = e.NewRow
        let name = e.Table.Rows[row][0]
        textFieldDetails.Text <- name.ToString()
    )
    table

let showRecordDialog() = 
    let dialog = new Dialog("Add record", 60, 20)

    let record = {
        Id = 0
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
        X = 0,
        Y = Pos.Bottom(titleLabel),
        Width = Dim.Fill()
    )

    let usernameLabel = new Label(
        Text = "Username: ",
        X = 0,
        Y = Pos.Bottom(titleTextField)
    )

    let usernameTextField = new TextField(
        Text = record.Username,
        X = 0,
        Y = Pos.Bottom(usernameLabel),
        Width = Dim.Fill()
    )

    let passwordLabel = new Label(
        Text = "Password: ",
        X = 0,
        Y = Pos.Bottom(usernameTextField)
    )

    let passwordTextField = new TextField(
        Text = record.Password,
        X = 0,
        Y = Pos.Bottom(passwordLabel),
        Width = Dim.Fill()
    )

    let urlLabel = new Label(
        Text = "Url: ",
        X = 0,
        Y = Pos.Bottom(passwordTextField)
    )

    let urlTextField = new TextField(
        Text = record.Url,
        X = 0,
        Y = Pos.Bottom(urlLabel),
        Width = Dim.Fill()
    )

    let notesLabel = new Label(
        Text = "Notes: ",
        X = 0,
        Y = Pos.Bottom(urlTextField)
    )

    let notesTextField = new TextField(
        Text = record.Notes,
        X = 0,
        Y = Pos.Bottom(notesLabel),
        Width = Dim.Fill()
    )

    let categoryLabel = new Label(
        Text = "Category: ",
        X = 0,
        Y = Pos.Bottom(notesTextField)
    )

    let categoryComboBox: ComboBox = 
        let cb = new ComboBox(
            X = 0,
            Y = Pos.Bottom(categoryLabel),
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        )

        let x = ["x"; "y"; "z"] |> List.toArray

        //cb.SetSource(categories |> List.toArray);
        cb.SetSource(x);
        cb


    dialog.Add(titleLabel, titleTextField,
               usernameLabel, usernameTextField,
               passwordLabel, passwordTextField,
               urlLabel, urlTextField,
               notesLabel, notesTextField,
               categoryLabel, categoryComboBox)

    (* Exit button *)
    let exitButton = new Button("Exit", true)
    exitButton.add_Clicked (fun _ -> Application.RequestStop(dialog))

    (* Create button *)
    let createButton = new Button("Create", true)
    createButton.add_Clicked(fun _ -> 

        let salt = generateSalt 32
        let enteredPassword = passwordTextField.Text
        let encryptedPassword =
            enteredPassword
            |> fun password ->
                xorEncrypt(password |> string, 32)
            |> string

        let c = categoryComboBox.Subviews.[0]
        let z = categoryComboBox.SelectedItem
        let x = urlTextField

        let updatedRecord = {
            record with
                Title = titleTextField.Text |> string
                Username = usernameTextField.Text |> string
                Password = encryptedPassword
                Url = urlTextField.Text |> string
                Notes = notesTextField.Text |> string
                Category = categoryComboBox.Text |> string
                CreationDate = DateTime.Now
                LastModifiedDate = DateTime.Now
        }

        createRecord(updatedRecord)
        categoryTable.Table <- convertListToDataTableCategory(Repo.getCategories())
        recordTable.Table <- convertListToDataTable(Repo.getRecords())
        Application.RequestStop(dialog)
    )

    dialog.AddButton(createButton)
    dialog.AddButton(exitButton)
    Application.Run(dialog) 

let openPasswordGeneratorDialog() = 
    let dialog = new Dialog("Password generator", 60, 20)

    let password = generatePassword {
        Length = 16
        Numbers = true
        Uppercase = true
        Lowercase = false
        Special = true
        ExcludeSimilar = true
    }

    let passwordField = new TextField(
        Text = password,
        X = 0,
        Y = 1,
        Width = Dim.Fill(),
        ReadOnly = true
    )

    let numbersCheckBox = new CheckBox(
        s = "Allow numbers",
        X = 0,
        Y = Pos.Bottom(passwordField),
        Checked = true
    )

    let uppercaseCheckBox = new CheckBox(
        s = "Allow uppercase",
        X = 0,
        Y = Pos.Bottom(numbersCheckBox),
        Checked = true
    )

    let lowercaseCheckBox = new CheckBox(
        s = "Allow lowercase",
        X = 0,
        Y = Pos.Bottom(uppercaseCheckBox),
        Checked = true
    )

    let specialCheckBox = new CheckBox(
        s = "Allow special characters",
        X = 0,
        Y = Pos.Bottom(lowercaseCheckBox),
        Checked = true
    )

    let excludeSimilarCheckBox = new CheckBox(
        s = "Exclude similar characters",
        X = 0,
        Y = Pos.Bottom(specialCheckBox),
        Checked = true
    )

    let generateButton = new Button("Generate", true)

    generateButton.add_Clicked(fun _ -> 
        let password = generatePassword {
            Length = 16
            Numbers = numbersCheckBox.Checked
            Uppercase = uppercaseCheckBox.Checked 
            Lowercase = lowercaseCheckBox.Checked 
            Special = specialCheckBox.Checked 
            ExcludeSimilar = excludeSimilarCheckBox.Checked 
        }

        passwordField.Text <- password
    )

    let exitButton = new Button("Exit", true)
    exitButton.add_Clicked (fun _ -> Application.RequestStop(dialog))

    dialog.Add(passwordField, numbersCheckBox, uppercaseCheckBox,
        lowercaseCheckBox, specialCheckBox, excludeSimilarCheckBox)
    dialog.AddButton generateButton 
    dialog.AddButton exitButton 
    Application.Run dialog

(* MenuBar *)
let menu = 
    new MenuBar(
        [|
            MenuBarItem ("File",
                [| MenuItem ("Open", "", (fun () -> openFileDialog())) 
                   MenuItem ("Create", "", (fun () -> Application.RequestStop ())) |]);
            MenuBarItem ("Tools",
                [| MenuItem ("Password generator", "", (fun () -> openPasswordGeneratorDialog()))
                   MenuItem ("Paste", "", Unchecked.defaultof<_>) |])
            MenuBarItem ("Records",
                [| MenuItem ("Add record", "", (fun () -> showRecordDialog()))
                   MenuItem ("Paste", "", Unchecked.defaultof<_>) |])
            MenuBarItem ("Help",
                [| MenuItem ("About", "",(fun () -> openUrl("https://github.com/MaciekWin3/TermKeyVault") |> ignore))
                   MenuItem ("Website", "", (fun () -> openUrl("https://github.com/MaciekWin3/TermKeyVault#readme") |> ignore)) |])
        |])

categoryTable.add_SelectedCellChanged(fun e -> 
    let row = e.NewRow
    let name = e.Table.Rows[row][0]
    recordTable.Table <- convertListToDataTable(Repo.getRecordsByCategory (name.ToString()))
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

            let checkIfPasswordIsValid =
                field.Text
                |> string 
                |> checkPassword

            if (checkIfPasswordIsValid) then
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

