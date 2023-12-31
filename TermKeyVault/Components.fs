﻿module Components

open Terminal.Gui
open System.Data
open System

open Types
open Repo
open Cryptography
open Utils

type DialogType =
    | Add
    | Edit

module TableDataConversions =
    open Utils.Configuration

    let convertListToDataTableOfRecords (list: List<Record>) =
        let table = new DataTable()
        table.Columns.Add("Title") |> ignore
        table.Columns.Add("Username") |> ignore
        table.Columns.Add("Password") |> ignore
        table.Columns.Add("Category") |> ignore
        table.Columns.Add("Url") |> ignore
        table.Columns.Add("Notes") |> ignore

        list
        |> List.iter (fun item ->
            let decryptedPassword = xorDecrypt (item.Password, getEncryptionKey ())
            let maskedPassword = new string ('*', decryptedPassword.Length)
            let row = table.NewRow()
            row.[0] <- item.Title
            row.[1] <- item.Username
            row.[2] <- maskedPassword
            row.[3] <- item.Category
            row.[4] <- item.Url
            row.[5] <- item.Notes
            table.Rows.Add(row) |> ignore)

        table

    let convertListToDataTableOfCategories (list: List<string>) =
        let table = new DataTable()
        table.Columns.Add("Category") |> ignore

        list
        |> List.iter (fun item ->
            let row = table.NewRow()
            row.[0] <- item
            table.Rows.Add(row) |> ignore)

        table

module ClipboardTimer =
    open System.Threading
    open System.Timers

    let mutable isTimerRunning = false
    let timerLock = obj ()
    let cancellationTokenSource = ref (null: CancellationTokenSource)

    let message (time: string) = $"Clipboard will be cleared in {time}"

    let resetTimer (timer: Timer) =
        timer.Stop()
        timer.Dispose()

    let runTimer (timer: Timer, statusBar: StatusBar, elapsedTime: byref<float>, _) =
        Application.Refresh()
        elapsedTime <- elapsedTime + 0.8

        let timeString =
            (TimeSpan.FromSeconds(8.0) - TimeSpan.FromSeconds(elapsedTime))
                .ToString(@"hh\:mm\:ss")

        statusBar.Items.[2].Title <- message timeString

        if elapsedTime >= 8.0 then
            resetTimer (timer)
            Clipboard.TrySetClipboardData("") |> ignore
            elapsedTime <- 0.0
            statusBar.Items.[2].Title <- ""
            Application.Refresh()
            isTimerRunning <- false


    let setClipboardTimer (statusBar: StatusBar) =
        Application.MainLoop.Invoke(fun () ->
            let newCancellationTokenSource = new CancellationTokenSource()
            let timer = new Timer(800.0)
            timer.AutoReset <- true
            statusBar.Items.[2].Title <- message "00:00:08"
            let mutable timeLeftInClipboard: float = 0.0

            if isTimerRunning then
                (cancellationTokenSource.Value).Cancel()

            lock timerLock (fun () ->
                cancellationTokenSource.contents <- newCancellationTokenSource
                isTimerRunning <- true

                timer.Elapsed.Add(fun _ ->
                    if not newCancellationTokenSource.Token.IsCancellationRequested then
                        runTimer (timer, statusBar, &timeLeftInClipboard, newCancellationTokenSource.Token))

                timer.Start()))

module StatusBar =
    let statusBar =
        let bar = new StatusBar()

        bar.Items <-
            [|
               // TODO: Implements ctrl c on quit app from login window
               new StatusItem(Key.C ||| Key.CtrlMask, "~CTRL-C~ Quit", (fun () -> Application.RequestStop()))
               new StatusItem(Key.Null, $"OS Clipboard IsSupported : {Clipboard.IsSupported}", null)
               new StatusItem(Key.CharMask, "", (fun _ -> ())) |]

        bar

module Categories =
    module CategoryTable =
        let convertListToDataTableOfCategories (list: List<string>) =
            let dataTable = new DataTable()
            dataTable.Columns.Add("Category") |> ignore

            list
            |> List.iter (fun item ->
                let row = dataTable.NewRow()
                row.[0] <- item
                dataTable.Rows.Add(row) |> ignore)

            dataTable

        let categoryTable =
            let Win = new Window("Example window for colors")

            let table =
                new TableView(
                    X = 0,
                    Y = 0,
                    Width = Dim.Percent(25f),
                    Height = Dim.Percent(70f),
                    FullRowSelect = true,
                    ColorScheme =
                        new ColorScheme(
                            HotNormal = Win.ColorScheme.HotNormal,
                            Focus = Win.ColorScheme.Focus,
                            HotFocus = Attribute.Make(Color.Blue, Color.Gray),
                            Disabled = Win.ColorScheme.Disabled,
                            Normal = Win.ColorScheme.Normal
                        )
                )

            table.Style.AlwaysShowHeaders <- true
            table.Table <- convertListToDataTableOfCategories (Repo.getCategories ())
            table

module DetailsFrame =
    open Categories.CategoryTable

    let textFieldDetails =
        let tv =
            new TextView(X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true)

        tv.WordWrap <- true
        tv.DesiredCursorVisibility <- CursorVisibility.Invisible
        tv.ColorScheme <- Colors.Menu
        tv

    let frameView =
        let fv =
            new FrameView(X = 0, Y = Pos.Bottom(categoryTable), Width = Dim.Fill(), Height = Dim.Fill())

        fv.Add(textFieldDetails)
        fv

module RecordDialog =
    open Utils.Configuration

    let showCreateRecordDialog (r: Record option, dialogType: DialogType, onFinish) =
        let title = if r = None then "Add record" else "Edit record"
        let dialog = new Dialog(title, 62, 22)

        let record =
            match r with
            | Some r -> r
            | None ->
                { Id = 0
                  Title = ""
                  Username = ""
                  Password = ""
                  Url = "https://"
                  Notes = ""
                  Category = ""
                  CreationDate = DateTime.Now
                  LastModifiedDate = DateTime.Now }

        (* Title *)
        let titleLabel = new Label(Text = "Title: ", X = 0, Y = 1)

        let titleTextField =
            new TextField(Text = record.Title, X = 0, Y = Pos.Bottom(titleLabel), Width = Dim.Fill())

        let usernameLabel =
            new Label(Text = "Username: ", X = 0, Y = Pos.Bottom(titleTextField))

        let usernameTextField =
            new TextField(Text = record.Username, X = 0, Y = Pos.Bottom(usernameLabel), Width = Dim.Fill())

        let passwordLabel =
            new Label(Text = "Password: ", X = 0, Y = Pos.Bottom(usernameTextField))

        let passwordTextField =
            new TextField(Text = record.Password, X = 0, Y = Pos.Bottom(passwordLabel), Width = Dim.Fill())

        passwordTextField.Secret <- true

        let confirmPasswordLabel =
            new Label(Text = "Confirm password: ", X = 0, Y = Pos.Bottom(passwordTextField))

        let confirmPassword = if dialogType = DialogType.Edit then record.Password else ""

        let confirmPasswordTextField =
            new TextField(Text = confirmPassword, X = 0, Y = Pos.Bottom(confirmPasswordLabel), Width = Dim.Fill())

        confirmPasswordTextField.Secret <- true

        let urlLabel =
            new Label(Text = "Url: ", X = 0, Y = Pos.Bottom(confirmPasswordTextField))

        let urlTextField =
            new TextField(Text = record.Url, X = 0, Y = Pos.Bottom(urlLabel), Width = Dim.Fill())

        let notesLabel = new Label(Text = "Notes: ", X = 0, Y = Pos.Bottom(urlTextField))

        let notesTextField =
            new TextView(Text = record.Notes, X = 0, Y = Pos.Bottom(notesLabel), Height = 3, Width = Dim.Fill())

        let categoryLabel =
            new Label(Text = "Category: ", X = 0, Y = Pos.Bottom(notesTextField))

        let categoryComboBox: ComboBox =
            let cb =
                new ComboBox(X = 0, Y = Pos.Bottom(categoryLabel), Width = Dim.Fill(), Height = Dim.Fill(1))

            let categories = Repo.getCategories () |> List.toArray |> Array.filter ((<>) "All")

            cb.SetSource(categories)

            if dialogType = DialogType.Edit then
                let index = Array.findIndex ((=) record.Category) categories
                cb.SelectedItem <- index

            cb

        dialog.Add(
            titleLabel,
            titleTextField,
            usernameLabel,
            usernameTextField,
            passwordLabel,
            passwordTextField,
            confirmPasswordLabel,
            confirmPasswordTextField,
            urlLabel,
            urlTextField,
            notesLabel,
            notesTextField,
            categoryLabel,
            categoryComboBox
        )

        // TODO: make better validation
        let validate (password: string, confirmation: string) =
            password = confirmation

        (* Exit button *)
        let exitButton = new Button("Exit", true)
        exitButton.add_Clicked (fun _ -> Application.RequestStop(dialog))

        (* Action button *)
        let actionButtonTitle =
            match dialogType with
            | Add -> "Create"
            | Edit -> "Update"

        let actionButton = new Button(actionButtonTitle, true)

        actionButton.add_Clicked (fun _ ->
            match validate (passwordTextField.Text |> string, confirmPasswordTextField.Text |> string) with
            | true ->
                let enteredPassword = passwordTextField.Text

                let encryptedPassword =
                    enteredPassword
                    |> fun password -> xorEncrypt (password |> string, getEncryptionKey ())
                    |> string

                let updatedRecord =
                    { record with
                        Title = titleTextField.Text |> string
                        Username = usernameTextField.Text |> string
                        Password = encryptedPassword
                        Url = urlTextField.Text |> string
                        Notes = notesTextField.Text |> string
                        Category = categoryComboBox.SearchText |> string
                        CreationDate = DateTime.Now
                        LastModifiedDate = DateTime.Now }

                match dialogType with
                | Add -> createRecord (updatedRecord)
                | Edit -> updateRecord (record.Title, updatedRecord)

                onFinish ()
                Application.RequestStop(dialog)
            | false -> MessageBox.ErrorQuery("Error", "Passwords do not match", "Ok") |> ignore)

        dialog.AddButton(actionButton)
        dialog.AddButton(exitButton)
        titleTextField.SetFocus()
        Application.Run(dialog)

module InspectDialog =
    open RecordDialog
    open Categories.CategoryTable

    // Important: This is workaround because F# disallaows circular references
    let refresh () =
        let categoryRow = categoryTable.SelectedRow

        if categoryRow = 0 then
            categoryTable.SetSelection(0, categoryRow + 1, false)
        else
            categoryTable.SetSelection(0, categoryRow - 1, false)

        categoryTable.SetSelection(0, categoryRow, false)

    let action (e: TableView.CellActivatedEventArgs) =
        let recordRow = e.Row
        let name = e.Table.Rows.[recordRow].[0]

        match name with
        | :? string as str ->
            let record = Repo.getRecordByTitle (str)

            match record with
            | Some record -> showCreateRecordDialog (Some record, DialogType.Edit, (fun () -> refresh ()))
            | None -> ()
        | _ -> ()

module RecordTable =
    open Utils.Configuration
    open ClipboardTimer
    open StatusBar
    open DetailsFrame
    open Categories.CategoryTable
    open RecordDialog

    let convertListToDataTableOfRecords (list: List<Record>) =
        let table = new DataTable()
        table.Columns.Add("Title") |> ignore
        table.Columns.Add("Username") |> ignore
        table.Columns.Add("Password") |> ignore
        table.Columns.Add("Category") |> ignore
        table.Columns.Add("Url") |> ignore
        table.Columns.Add("Notes") |> ignore

        list
        |> List.iter (fun item ->
            let decryptedPassword = xorDecrypt (item.Password, getEncryptionKey ())
            let maskedPassword = new string ('*', decryptedPassword.Length)
            let row = table.NewRow()
            row.[0] <- item.Title
            row.[1] <- item.Username
            row.[2] <- maskedPassword
            row.[3] <- item.Category
            row.[4] <- item.Url
            row.[5] <- item.Notes
            table.Rows.Add(row) |> ignore)

        table

    let recordTable =
        let Win = new Window("Example window for colors")

        let table =
            new TableView(
                X = Pos.Right(categoryTable),
                Y = 0,
                Width = Dim.Percent(75f),
                Height = Dim.Percent(70f),
                FullRowSelect = true,
                ColorScheme =
                    new ColorScheme(
                        HotNormal = Win.ColorScheme.HotNormal,
                        Focus = Win.ColorScheme.Focus,
                        HotFocus = Attribute.Make(Color.Blue, Color.Gray),
                        Disabled = Win.ColorScheme.Disabled,
                        Normal = Win.ColorScheme.Normal
                    )
            )

        let refreshTables () =
            let category = string categoryTable.Table.Rows.[categoryTable.SelectedRow].[0]
            categoryTable.Table <- convertListToDataTableOfCategories (Repo.getCategories ())

            if category <> "All" then
                table.Table <- convertListToDataTableOfRecords (Repo.getRecordsByCategory (category))
            else
                table.Table <- convertListToDataTableOfRecords (Repo.getRecords ())

        let copyPasswordToClipboard (record: Record) =
            let preparedPassword = xorDecrypt (record.Password |> string, getEncryptionKey ())
            let isCopingSuccessfull = Clipboard.TrySetClipboardData(preparedPassword)

            match isCopingSuccessfull with
            | true -> setClipboardTimer (statusBar) |> ignore
            | false -> MessageBox.ErrorQuery("Clipboard", "Failed to copy to clipboard") |> ignore

        let showContextMenu (screenPoint: Point, record: Record, deleteMethod) =
            let contextMenu =
                new ContextMenu(
                    screenPoint.X,
                    screenPoint.Y,
                    MenuBarItem(
                        "File",
                        [| MenuItem("Copy", "Copy password to clipboard", (fun () -> copyPasswordToClipboard (record)))
                           MenuItem(
                               "Edit",
                               "Edit record",
                               (fun () ->
                                   showCreateRecordDialog (Some record, DialogType.Edit, (fun () -> refreshTables ())))
                           )
                           MenuItem("Delete", "Delete record", (fun () -> deleteMethod (record.Title))) |]
                    )
                )

            contextMenu.Show()

        let deleteItem (title: string) =
            let decision =
                MessageBox.Query("Delete", "Are you sure you want to delete this item?", "Yes", "No")

            if decision = 0 then
                Repo.deleteRecord (title)
                refreshTables ()

        let records = Repo.getRecords ()
        table.Style.AlwaysShowHeaders <- true
        table.Table <- convertListToDataTableOfRecords (records)

        table.add_CellActivated (fun (e) -> 
            let recordRow = table.SelectedRow
            let name = table.Table.Rows.[recordRow].[0]
            let point = Point(table.Frame.X + 5, table.Frame.Y + 5)
            match name with
            | :? string as str ->
                let record = Repo.getRecordByTitle (str)

                match record with
                | Some record -> showContextMenu (point, record, deleteItem)
                | None -> MessageBox.ErrorQuery("Error", "Unable to show context menu", "Ok") |> ignore
            | _ -> MessageBox.ErrorQuery("Error", "Unabale to find given record", "Ok") |> ignore)

        table.add_MouseClick (fun e ->
            if (e.MouseEvent.Flags.HasFlag(MouseFlags.Button3Clicked)) then
                e.Handled <- true
                let cell = table.ScreenToCell(e.MouseEvent.X, e.MouseEvent.Y)

                if cell.HasValue then
                    table.SetSelection(1, cell.Value.Y, false)
                    let title = string table.Table.Rows[e.MouseEvent.Y - 3].[0]
                    let record = Repo.getRecordByTitle (title)

                    match record with
                    | Some record ->
                        showContextMenu (
                            Point(e.MouseEvent.X + table.Frame.X + 2, e.MouseEvent.Y + table.Frame.Y + 2),
                            record,
                            deleteItem
                        )

                    | None -> 
                        MessageBox.ErrorQuery("Error", "Unable to show context menu", "Ok") |> ignore)

        
        table.add_MouseClick (fun e ->
            if (e.MouseEvent.Flags.HasFlag(MouseFlags.Button1DoubleClicked)) then
                e.Handled <- true
                let cell = table.ScreenToCell(e.MouseEvent.X, e.MouseEvent.Y)

                if cell.HasValue then
                    table.SetSelection(1, cell.Value.Y, false)
                    let point = Point(table.Frame.X + 5, table.Frame.Y + 5)
                    let title = string table.Table.Rows[e.MouseEvent.Y - 3].[0]
                    let record = Repo.getRecordByTitle (title)
                    match record with
                    | Some record -> copyPasswordToClipboard (record)
                    | None -> MessageBox.ErrorQuery("Error", "Unable to copy password to clipboard", "Ok") |> ignore)

        table.add_SelectedCellChanged (fun e ->
            let row = e.NewRow

            if row < 0 then
                textFieldDetails.Text <- ""
            else
                let title = e.Table.Rows[row][0]
                let username = e.Table.Rows[row][1]
                let password = e.Table.Rows[row][2]
                let category = e.Table.Rows[row][3]
                let url = e.Table.Rows[row][4]
                let notes = e.Table.Rows[row][5]

                let text =
                    $"Title: {title}, User Name: {username}, Password: {password}, Category: {category}, Url: {url}, Notes: {notes}"

                textFieldDetails.Text <- text)

        table

    let refreshTables () =
        let category = string categoryTable.Table.Rows.[categoryTable.SelectedRow].[0]
        categoryTable.Table <- convertListToDataTableOfCategories (Repo.getCategories ())
        if category <> "All" then
            recordTable.Table <- convertListToDataTableOfRecords (Repo.getRecordsByCategory (category))
        else
            recordTable.Table <- convertListToDataTableOfRecords (Repo.getRecords ())

module PasswordGenerator =
    let openPasswordGeneratorDialog () =
        let dialog = new Dialog("Password generator", 60, 20)

        let password =
            generatePassword
                { Length = 16
                  Numbers = true
                  Uppercase = true
                  Lowercase = false
                  Special = true
                  ExcludeSimilar = true }

        let passwordField =
            new TextField(Text = password, X = 0, Y = 1, Width = Dim.Fill(), ReadOnly = true)

        let numbersCheckBox =
            new CheckBox(s = "Allow numbers", X = 0, Y = Pos.Bottom(passwordField), Checked = true)

        let uppercaseCheckBox =
            new CheckBox(s = "Allow uppercase", X = 0, Y = Pos.Bottom(numbersCheckBox), Checked = true)

        let lowercaseCheckBox =
            new CheckBox(s = "Allow lowercase", X = 0, Y = Pos.Bottom(uppercaseCheckBox), Checked = true)

        let specialCheckBox =
            new CheckBox(s = "Allow special characters", X = 0, Y = Pos.Bottom(lowercaseCheckBox), Checked = true)

        let excludeSimilarCheckBox =
            new CheckBox(s = "Exclude similar characters", X = 0, Y = Pos.Bottom(specialCheckBox), Checked = true)

        let generateButton = new Button("Generate", true)

        generateButton.add_Clicked (fun _ ->
            let password =
                generatePassword
                    { Length = 16
                      Numbers = numbersCheckBox.Checked
                      Uppercase = uppercaseCheckBox.Checked
                      Lowercase = lowercaseCheckBox.Checked
                      Special = specialCheckBox.Checked
                      ExcludeSimilar = excludeSimilarCheckBox.Checked }

            passwordField.Text <- password)

        let exitButton = new Button("Exit", true)
        exitButton.add_Clicked (fun _ -> Application.RequestStop(dialog))

        dialog.Add(
            passwordField,
            numbersCheckBox,
            uppercaseCheckBox,
            lowercaseCheckBox,
            specialCheckBox,
            excludeSimilarCheckBox
        )

        dialog.AddButton generateButton
        dialog.AddButton exitButton
        Application.Run dialog

module Navbar =
    open System.Text
    open PasswordGenerator
    open RecordDialog
    open Categories.CategoryTable
    open RecordTable

    let showAsciiArt () =
        let sb = new StringBuilder()
        sb.AppendLine ("Simple TUI password manager") |> ignore
        sb.AppendLine ("") |> ignore
        sb.AppendLine("  _______ _  ___      __ ") |> ignore
        sb.AppendLine(" |__   __| |/ \ \    / / ") |> ignore
        sb.AppendLine("    | |  | ' / \ \  / /  ") |> ignore
        sb.AppendLine("    | |  |  <   \ \/ /   ") |> ignore
        sb.AppendLine("    | |  | . \   \  /    ") |> ignore
        sb.AppendLine("    |_|  |_|\_\   \/     ") |> ignore
        sb.AppendLine("                         ") |> ignore
        sb.AppendLine("") |> ignore
        sb.AppendLine("https://github.com/MaciekWin3/TermKeyVault") |> ignore
        sb.ToString() |> ignore
        MessageBox.Query("About TermKeyVault", sb.ToString(), "Ok") |> ignore


    let menu =
        new MenuBar(
            [| MenuBarItem(
                   "App",
                   [| MenuItem("Config", "Show config file", (fun () -> Configuration.openConfigFile () |> ignore))
                      MenuItem("Quit", "Quit application", (fun () -> Application.RequestStop())) |]
               )
               MenuBarItem(
                   "Tools",
                   [| MenuItem(
                          "Password generator",
                          "Generate new password",
                          (fun () -> openPasswordGeneratorDialog ())
                      ) |]
               )
               MenuBarItem(
                   "Records",
                   [| MenuItem(
                          "Add",
                          "Adds new record",
                          (fun () -> showCreateRecordDialog (None, DialogType.Add, refreshTables))
                      ) |]
               )
               MenuBarItem(
                   "Help",
                   [| MenuItem("About", "Info about app", (fun () -> showAsciiArt ()))
                      MenuItem(
                          "Website",
                          "Project repo",
                          (fun () ->
                              try
                                  Web.openUrl ("https://github.com/MaciekWin3/TermKeyVault") |> ignore
                              with _ ->
                                  MessageBox.ErrorQuery("Error opening website", "Cannot open url", "Ok")
                                  |> ignore)
                      ) |]
               ) |]
        )

    let updateRecordTable (name: string) =
        let records =
            match name with
            | "All" -> Repo.getRecords ()
            | _ -> Repo.getRecordsByCategory name

        recordTable.Table <- records |> convertListToDataTableOfRecords

    categoryTable.add_SelectedCellChanged (fun e ->
        let row = e.NewRow
        let name = e.Table.Rows[row][0] |> string
        updateRecordTable name)

