module Components

open Terminal.Gui.App
open Terminal.Gui.Views
open Terminal.Gui.ViewBase
open Terminal.Gui.Input
open Terminal.Gui.Drawing
open System.Data
open System
open System.Drawing

open Types
open Repo
open Cryptography
open Utils
open Utils.AppContext

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

    let runTimer (timer: Timer, statusShortcut: Shortcut, elapsedTime: byref<float>, _) =
        layoutAndDraw true
        elapsedTime <- elapsedTime + 0.8

        let timeString =
            (TimeSpan.FromSeconds(8.0) - TimeSpan.FromSeconds(elapsedTime))
                .ToString(@"hh\:mm\:ss")

        statusShortcut.Text <- message timeString

        if elapsedTime >= 8.0 then
            resetTimer (timer)
            (clipboard ()).TrySetClipboardData("") |> ignore
            elapsedTime <- 0.0
            statusShortcut.Text <- ""
            layoutAndDraw true
            isTimerRunning <- false


    let setClipboardTimer (statusShortcut: Shortcut) =
        invoke (fun () ->
            let newCancellationTokenSource = new CancellationTokenSource()
            let timer = new Timer(800.0)
            timer.AutoReset <- true
            statusShortcut.Text <- message "00:00:08"
            let mutable timeLeftInClipboard: float = 0.0

            if isTimerRunning then
                (cancellationTokenSource.Value).Cancel()

            lock timerLock (fun () ->
                cancellationTokenSource.contents <- newCancellationTokenSource
                isTimerRunning <- true

                timer.Elapsed.Add(fun _ ->
                    if not newCancellationTokenSource.Token.IsCancellationRequested then
                        invoke (fun () ->
                            runTimer (timer, statusShortcut, &timeLeftInClipboard, newCancellationTokenSource.Token)))

                timer.Start()))

module StatusBar =
    let clipboardStatusShortcut = new Shortcut(Key.Empty, "", (fun () -> ()), "")

    let statusBar =
        let clipboardSupport = if (clipboard ()).IsSupported then "Available" else "Unavailable"

        let bar =
            new StatusBar(
            [| new Shortcut(Key.C.WithCtrl, "~Ctrl+C~ Quit", (fun () -> requestStopTop ()), "")
               new Shortcut(Key.Empty, $"Clipboard: {clipboardSupport}", (fun () -> ()), "")
               clipboardStatusShortcut |]
            )

        bar.X <- 0
        bar.Y <- Pos.AnchorEnd(1)
        bar.Width <- Dim.Fill()
        bar.Height <- 1
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
            let table =
                new TableView(
                    X = 0,
                    Y = 0,
                    Width = Dim.Percent(25),
                    Height = Dim.Percent(70),
                    FullRowSelect = true
                )

            table.Style.AlwaysShowHeaders <- true
            table.Style.ShowVerticalCellLines <- true
            table.Style.ShowVerticalHeaderLines <- true
            table.Style.ShowHorizontalHeaderOverline <- true
            table.Style.ShowHorizontalHeaderUnderline <- true
            table.Style.ShowHorizontalBottomline <- true
            table.Table <- DataTableSource(convertListToDataTableOfCategories (Repo.getCategories ()))
            table

module DetailsFrame =
    open Categories.CategoryTable

    let textFieldDetails =
        let tv =
            new TextView(X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true)

        tv.WordWrap <- true
        tv

    let frameView =
        let fv =
            new FrameView(Title = "Details", X = 0, Y = Pos.Bottom(categoryTable), Width = Dim.Fill(), Height = Dim.Fill())

        fv.Add(textFieldDetails) |> ignore
        fv

module RecordDialog =
    open Utils.Configuration

    let showCreateRecordDialog (r: Record option, dialogType: DialogType, onFinish) =
        let title = if r = None then "Add Record" else "Edit Record"
        let dialog = new Dialog(Title = title, Width = 62, Height = 22)

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

        let categoryTextField =
            let initialCategory =
                if dialogType = DialogType.Edit then
                    record.Category
                else
                    ""

            new TextField(Text = initialCategory, X = 0, Y = Pos.Bottom(categoryLabel), Width = Dim.Fill())

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
            categoryTextField
        )

        // TODO: make better validation
        let validate (password: string, confirmation: string) =
            password = confirmation

        (* Exit button *)
        let exitButton = new Button(Text = "Cancel")
        exitButton.add_Accepted (fun _ _ -> dialog.RequestStop())

        (* Action button *)
        let actionButtonTitle =
            match dialogType with
            | Add -> "Create"
            | Edit -> "Update"

        let actionButton = new Button(Text = actionButtonTitle, IsDefault = true)

        actionButton.add_Accepted (fun _ _ ->
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
                        Category = categoryTextField.Text |> string
                        CreationDate = DateTime.Now
                        LastModifiedDate = DateTime.Now }

                match dialogType with
                | Add -> createRecord (updatedRecord)
                | Edit -> updateRecord (record.Title, updatedRecord)

                onFinish ()
                dialog.RequestStop()
            | false -> MessageBox.ErrorQuery(getApp (), "Error", "Passwords do not match.", "OK") |> ignore)

        dialog.AddButton(actionButton)
        dialog.AddButton(exitButton)
        titleTextField.SetFocus() |> ignore
        run dialog

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

    let action (e: CellActivatedEventArgs) =
        let recordRow = e.Row
        let name = e.Table.[recordRow, 0]

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
        let table =
            new TableView(
                X = Pos.Right(categoryTable),
                Y = 0,
                Width = Dim.Percent(75),
                Height = Dim.Percent(70),
                FullRowSelect = true
            )

        let refreshTables () =
            let category = string categoryTable.Table.[categoryTable.SelectedRow, 0]
            categoryTable.Table <- DataTableSource(convertListToDataTableOfCategories (Repo.getCategories ()))

            if category <> "All" then
                table.Table <- DataTableSource(convertListToDataTableOfRecords (Repo.getRecordsByCategory (category)))
            else
                table.Table <- DataTableSource(convertListToDataTableOfRecords (Repo.getRecords ()))

        let copyPasswordToClipboard (record: Record) =
            let preparedPassword = xorDecrypt (record.Password |> string, getEncryptionKey ())
            let isCopingSuccessfull = (clipboard ()).TrySetClipboardData(preparedPassword)

            match isCopingSuccessfull with
            | true -> setClipboardTimer (clipboardStatusShortcut) |> ignore
            | false -> MessageBox.ErrorQuery(getApp (), "Clipboard", "Failed to copy to clipboard.", "OK") |> ignore

        let deleteItem (title: string) =
            let decision =
                MessageBox.Query(getApp (), "Delete", "Are you sure you want to delete this record?", "Yes", "No")

            if decision.HasValue && decision.Value = 0 then
                Repo.deleteRecord (title)
                refreshTables ()

        let showContextMenu (screenPoint: Point, record: Record) =
            let contextItems: View array =
                [| new MenuItem("Copy Password", "Copy password to clipboard", (fun () -> copyPasswordToClipboard (record)), Key.Empty)
                   :> View
                   new MenuItem(
                       "Edit",
                       "Edit selected record",
                       (fun () -> showCreateRecordDialog (Some record, DialogType.Edit, (fun () -> refreshTables ()))),
                       Key.Empty
                   )
                   :> View
                   new MenuItem("Delete", "Delete selected record", (fun () -> deleteItem (record.Title)), Key.Empty)
                   :> View |]

            let popover = new PopoverMenu(contextItems)
            (getApp ()).Popover.Register(popover) |> ignore
            popover.MakeVisible(screenPoint)

        let records = Repo.getRecords ()
        table.Style.AlwaysShowHeaders <- true
        table.Style.ShowVerticalCellLines <- true
        table.Style.ShowVerticalHeaderLines <- true
        table.Style.ShowHorizontalHeaderOverline <- true
        table.Style.ShowHorizontalHeaderUnderline <- true
        table.Style.ShowHorizontalBottomline <- true
        table.Table <- DataTableSource(convertListToDataTableOfRecords (records))

        table.add_CellActivated (fun _ (e: CellActivatedEventArgs) ->
            let name = e.Table.[e.Row, 0]
            match name with
            | :? string as str ->
                let record = Repo.getRecordByTitle (str)

                match record with
                | Some record -> showCreateRecordDialog (Some record, DialogType.Edit, (fun () -> refreshTables ()))
                | None -> MessageBox.ErrorQuery(getApp (), "Error", "Unable to open selected record.", "OK") |> ignore
            | _ -> MessageBox.ErrorQuery(getApp (), "Error", "Unable to find selected record.", "OK") |> ignore)

        table.add_MouseEvent (fun _ (mouse: Mouse) ->
            if mouse.Flags.HasFlag(MouseFlags.RightButtonClicked) || mouse.Flags.HasFlag(MouseFlags.RightButtonPressed) then
                mouse.Handled <- true
                let cell = table.ScreenToCell(mouse.ScreenPosition)

                if cell.HasValue then
                    let row = cell.Value.Y

                    if row >= 0 && row < table.Table.Rows then
                        table.SetSelection(0, row, false)
                        let title = string table.Table.[row, 0]

                        match Repo.getRecordByTitle (title) with
                        | Some record ->
                            let screenPoint = Point(mouse.ScreenPosition.X, mouse.ScreenPosition.Y)
                            showContextMenu (screenPoint, record)
                        | None ->
                            MessageBox.ErrorQuery(getApp (), "Error", "Unable to show context menu.", "OK")
                            |> ignore)

        table.add_SelectedCellChanged (fun _ (e: SelectedCellChangedEventArgs) ->
            let row = e.NewRow

            if row < 0 then
                textFieldDetails.Text <- ""
            else
                let title = e.Table.[row, 0]
                let username = e.Table.[row, 1]
                let password = e.Table.[row, 2]
                let category = e.Table.[row, 3]
                let url = e.Table.[row, 4]
                let notes = e.Table.[row, 5]

                let text =
                    $"Title: {title}\nUsername: {username}\nPassword: {password}\nCategory: {category}\nUrl: {url}\nNotes: {notes}"

                textFieldDetails.Text <- text)

        table.add_Accepted (fun _ _ ->
            let row = table.SelectedRow
            if row >= 0 then
                let title = table.Table.[row, 0] |> string
                match Repo.getRecordByTitle title with
                | Some record -> copyPasswordToClipboard record
                | None -> ())

        table

    let refreshTables () =
        let category = string categoryTable.Table.[categoryTable.SelectedRow, 0]
        categoryTable.Table <- DataTableSource(convertListToDataTableOfCategories (Repo.getCategories ()))
        if category <> "All" then
            recordTable.Table <- DataTableSource(convertListToDataTableOfRecords (Repo.getRecordsByCategory (category)))
        else
            recordTable.Table <- DataTableSource(convertListToDataTableOfRecords (Repo.getRecords ()))

module PasswordGenerator =
    let openPasswordGeneratorDialog () =
        let dialog = new Dialog(Title = "Password Generator", Width = 60, Height = 20)

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
            new CheckBox(Text = "Allow numbers", X = 0, Y = Pos.Bottom(passwordField), Value = CheckState.Checked)

        let uppercaseCheckBox =
            new CheckBox(Text = "Allow uppercase", X = 0, Y = Pos.Bottom(numbersCheckBox), Value = CheckState.Checked)

        let lowercaseCheckBox =
            new CheckBox(Text = "Allow lowercase", X = 0, Y = Pos.Bottom(uppercaseCheckBox), Value = CheckState.Checked)

        let specialCheckBox =
            new CheckBox(Text = "Allow special characters", X = 0, Y = Pos.Bottom(lowercaseCheckBox), Value = CheckState.Checked)

        let excludeSimilarCheckBox =
            new CheckBox(Text = "Exclude similar characters", X = 0, Y = Pos.Bottom(specialCheckBox), Value = CheckState.Checked)

        let generateButton = new Button(Text = "Regenerate", IsDefault = true)

        generateButton.add_Accepted (fun _ _ ->
            let password =
                generatePassword
                    { Length = 16
                      Numbers = numbersCheckBox.Value = CheckState.Checked
                      Uppercase = uppercaseCheckBox.Value = CheckState.Checked
                      Lowercase = lowercaseCheckBox.Value = CheckState.Checked
                      Special = specialCheckBox.Value = CheckState.Checked
                      ExcludeSimilar = excludeSimilarCheckBox.Value = CheckState.Checked }

            passwordField.Text <- password)

        let exitButton = new Button(Text = "Close")
        exitButton.add_Accepted (fun _ _ -> dialog.RequestStop())

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
        run dialog

module Navbar =
    open System.Text
    open PasswordGenerator
    open RecordDialog
    open Categories.CategoryTable
    open RecordTable

    let mutable private menuPopoversRegistered = false

    let showAsciiArt () =
        let sb = new StringBuilder()
        sb.AppendLine ("Simple terminal password manager") |> ignore
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
        MessageBox.Query(getApp (), "About TermKeyVault", sb.ToString(), "OK") |> ignore


    let menu =
        let appItems: View array =
            [| new MenuItem("Open Config", "Open config file", (fun () -> Configuration.openConfigFile () |> ignore), Key.Empty)
               :> View
               new MenuItem("Quit", "Quit application", (fun () -> requestStopTop ()), Key.Empty)
               :> View |]

        let toolsItems: View array =
            [| new MenuItem("Password Generator", "Generate new password", (fun () -> openPasswordGeneratorDialog ()), Key.Empty)
               :> View |]

        let recordItems: View array =
            [| new MenuItem("Add", "Add new record", (fun () -> showCreateRecordDialog (None, DialogType.Add, refreshTables)), Key.Empty)
               :> View |]

        let helpItems: View array =
            [| new MenuItem("About", "Info about app", (fun () -> showAsciiArt ()), Key.Empty) :> View
               new MenuItem(
                   "Website",
                   "Project repo",
                   (fun () ->
                       try
                           Web.openUrl ("https://github.com/MaciekWin3/TermKeyVault") |> ignore
                       with _ ->
                           MessageBox.ErrorQuery(getApp (), "Error opening website", "Cannot open URL.", "OK")
                           |> ignore),
                   Key.Empty
               )
               :> View |]

        let bar =
            new MenuBar(
            [| new MenuBarItem("App", appItems)
               new MenuBarItem("Tools", toolsItems)
               new MenuBarItem("Records", recordItems)
               new MenuBarItem("Help", helpItems) |]
            )

        bar.X <- 0
        bar.Y <- 0
        bar.Width <- Dim.Fill()
        bar.Height <- 1
        bar

    let ensureMenuPopoversRegistered () =
        if not menuPopoversRegistered then
            menu.SubViews
            |> Seq.iter (fun item ->
                match item with
                | :? MenuBarItem as menuItem when not (isNull menuItem.PopoverMenu) ->
                    (getApp ()).Popover.Register(menuItem.PopoverMenu) |> ignore
                | _ -> ())

            menuPopoversRegistered <- true

    let updateRecordTable (name: string) =
        let records =
            match name with
            | "All" -> Repo.getRecords ()
            | _ -> Repo.getRecordsByCategory name

        recordTable.Table <- records |> convertListToDataTableOfRecords |> DataTableSource

    categoryTable.add_SelectedCellChanged (fun _ (e: SelectedCellChangedEventArgs) ->
        let row = e.NewRow
        if row >= 0 then
            let name = e.Table.[row, 0] |> string
            updateRecordTable name)

