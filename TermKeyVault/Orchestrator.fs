module Orchestrator

open Terminal.Gui.App
open Terminal.Gui.Views
open Terminal.Gui.ViewBase

open Components
open Repo
open Cryptography
open Utils.AppContext

module MainWindow =
    open Categories.CategoryTable
    open RecordTable
    open DetailsFrame

    let mainWindow () =
        let window =
            new Window(Title = "TermKeyVault", X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill())

        window.Add(categoryTable) |> ignore
        window.Add(recordTable) |> ignore
        window.Add(frameView) |> ignore
        window

module ScreenOrchestrator =
    open Navbar
    open StatusBar

    let appRoot =
        new Runnable(X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), Arrangement = ViewArrangement.Overlapped)

    let switchWindow (newWindow: View, showMenu: bool, includeNavbar: bool, includeStatusBar: bool) =
        appRoot.RemoveAll() |> ignore

        if includeNavbar || showMenu then
            ensureMenuPopoversRegistered ()
            appRoot.Add(menu) |> ignore

        if includeStatusBar then
            appRoot.Add(statusBar) |> ignore

        newWindow.X <- 0
        newWindow.Y <- if includeNavbar || showMenu then 1 else 0
        newWindow.Width <- Dim.Fill()
        newWindow.Height <- Dim.Fill(if includeStatusBar then 1 else 0)

        appRoot.Add(newWindow) |> ignore

        layoutAndDraw true

module LoginWindow =
    open ScreenOrchestrator
    open MainWindow
    open Utils
    open Utils.Configuration

    let loginWindow () =
        let loginLabel =
            new Label(Text = "Master password:", X = Pos.Center(), Y = Pos.Center())

        let passwordField =
            let field =
                new TextField(X = Pos.Center(), Y = Pos.Bottom(loginLabel), Width = 30, Secret = true)

            let tryLogin () =
                let password = field.Text |> string

                let isValidDb =
                    match password with
                    | "" -> false
                    | p -> p |> checkIfDbIsValid

                let isValidPassword =
                    match password with
                    | "" -> false
                    | p -> p |> checkPassword

                match isValidDb, isValidPassword with
                | true, true ->
                    let encryptedPassword = xorEncrypt (password |> string, getEncryptionKey ())
                    Cache.addValueToCache("password", encryptedPassword)
                    switchWindow(mainWindow (), true, true, true)
                | true, false ->
                    MessageBox.ErrorQuery(getApp (), "Error", "Wrong password.", "OK") |> ignore
                    field.Text <- ""
                | false, true ->
                    MessageBox.ErrorQuery(getApp (), "Error", "Invalid database schema.", "OK") |> ignore
                    field.Text <- ""
                | false, false ->
                    MessageBox.ErrorQuery(getApp (), "Error", "Wrong password and invalid database.", "OK")
                    |> ignore
                    field.Text <- ""

            field.add_Accepted (fun _ _ -> tryLogin ())
            field.add_KeyDown (fun _ (key: Terminal.Gui.Input.Key) ->
                if key = Terminal.Gui.Input.Key.Enter then
                    key.Handled <- true
                    tryLogin ())

            field

        let window =
            new Window(Title = "Sign In", X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill())

        window.Add(loginLabel) |> ignore
        window.Add(passwordField) |> ignore
        passwordField.SetFocus() |> ignore
        window

module CreateDatabaseWizard =
    open ScreenOrchestrator
    open LoginWindow

    let showCreateDbWizard () =
        let dialog =
            new Dialog(Title = "TermKeyVault Setup", X = Pos.Center(), Y = Pos.Center(), Width = 64, Height = 14)

        let infoLabel =
            new Label(
                Text = "Create your encrypted SQLite database to start using TermKeyVault.",
                X = 1,
                Y = 1,
                Width = Dim.Fill(2),
                Height = 2
            )

        let passwordLabel = new Label(Text = "Master password:", X = 1, Y = 4)

        let password =
            new TextField(X = Pos.Right(passwordLabel), Y = Pos.Top(passwordLabel), Width = 30, Secret = true)

        let passwordRepeatLabel =
            new Label(Text = "Repeat password:", X = 1, Y = Pos.Bottom(passwordLabel))

        let passwordRepeat =
            new TextField(
                X = Pos.Right(passwordRepeatLabel),
                Y = Pos.Top(passwordRepeatLabel),
                Width = 30,
                Secret = true
            )

        let createButton = new Button(Text = "Create Database", IsDefault = true)

        createButton.add_Accepted (fun _ _ ->
            if password.Text = passwordRepeat.Text then
                prepareDb (password.Text |> string)
                dialog.RequestStop()
                switchWindow(loginWindow (), false, false, false)
                MessageBox.Query(getApp (), "Success", "Database created successfully.", "OK") |> ignore
            else
                MessageBox.ErrorQuery(getApp (), "Error", "Passwords do not match.", "OK") |> ignore)

        let cancelButton = new Button(Text = "Cancel")
        cancelButton.add_Accepted (fun _ _ -> dialog.RequestStop())

        dialog.Add(infoLabel, passwordLabel, password, passwordRepeatLabel, passwordRepeat) |> ignore
        dialog.AddButton(createButton)
        dialog.AddButton(cancelButton)
        password.SetFocus() |> ignore
        run dialog

