module Orchestrator

open Terminal.Gui
open Components
open Repo

module MainWindow =
    open Categories.CategoryTable
    open RecordTable
    open DetailsFrame

    let mainWindow =
        let window =
            new Window(Title = "TermKeyVault", X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill(1))

        window.Add(categoryTable)
        window.Add(recordTable)
        window.Add(frameView)
        window

module ScreenOrchestrator =
    open Navbar
    open StatusBar

    let switchWindow (newWindow: Window) (showMenu: bool) (includeNavbar: bool) (includeStatusBar: bool) =
        Application.Top.RemoveAll()

        if includeNavbar then
            Application.Top.Add(menu)

        if includeStatusBar then
            Application.Top.Add(statusBar)

        Application.Top.Add(newWindow)

        if showMenu then
            Application.Top.Add(menu)


module LoginWindow =
    open ScreenOrchestrator
    open MainWindow

    let loginWindow =
        let loginLabel =
            new Label(Text = "Enter Master Password: ", X = Pos.Center(), Y = Pos.Center())

        let passwordField =
            let field =
                new TextField(X = Pos.Center(), Y = Pos.Bottom(loginLabel), Width = 30, Secret = true)

            field.add_KeyPress (fun e ->
                if (e.KeyEvent.Key = Key.Enter) then
                    e.Handled <- true

                    let checkIfPasswordIsValid = field.Text |> string |> checkPassword

                    if (checkIfPasswordIsValid) then
                        switchWindow mainWindow true true true
                    else
                        field.Text <- ""
                        MessageBox.ErrorQuery("Error", "Wrong password", "Ok") |> ignore)

            field

        let window =
            new Window(Title = "Login", X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill())

        window.Add(loginLabel)
        window.Add(passwordField)
        window

module CreateDatabaseWizard =
    open ScreenOrchestrator
    open LoginWindow

    let showCreateDbWizard () =
        let wizard =
            new Wizard(
                X = Pos.Center(),
                Y = Pos.Center(),
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                Title = "TermKeyVault Setup"
            )

        wizard.Modal <- false

        let firstStep = new Wizard.WizardStep("Create database")

        wizard.AddStep(firstStep)

        // Step 1
        firstStep.HelpText <-
            "To use TermKeyVault you need to create a SQLite3 database. You can do it by clicking the button below."

        firstStep.NextButtonText <- "Accept!"
        firstStep.BackButtonText <- "Back"

        // Step 2
        let secondStep = new Wizard.WizardStep("Create master password")

        wizard.AddStep(secondStep)

        let passwordLabel = new Label(Text = "Enter master password:  ", X = 1, Y = 2)

        let password =
            new TextField(X = Pos.Right(passwordLabel), Y = Pos.Top(passwordLabel), Width = 30, Secret = true)

        let passwordRepeatLabel =
            new Label(Text = "Repeat master password: ", X = 1, Y = Pos.Bottom(passwordLabel))

        let passwordRepeat =
            new TextField(
                X = Pos.Right(passwordRepeatLabel),
                Y = Pos.Top(passwordRepeatLabel),
                Width = 30,
                Secret = true
            )

        secondStep.NextButtonText <- "Create database"
        secondStep.Add(passwordLabel, password, passwordRepeatLabel, passwordRepeat)

        wizard.add_Finished (fun _ ->
            if (password.Text = passwordRepeat.Text) then
                prepareDb (password.Text |> string)
                wizard.Enabled <- false
                switchWindow loginWindow false false false
                MessageBox.Query("Success!", "Successfuly created database", "Ok") |> ignore
            else
                MessageBox.ErrorQuery("Error", "Passwords do not match", "Ok") |> ignore)


        Application.Top.Add(wizard)
        Application.Run(Application.Top)

