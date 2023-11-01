module Program

open Terminal.Gui
open Repo
open Components.CreateDatabaseWizard
open Components.LoginWindow

[<EntryPoint>]
let initApp _ = 
    Application.Init()
    Colors.Base <- Colors.TopLevel
    let isDbCreated = checkIfDbExists()
    match isDbCreated with
    | true -> ()
    | false -> 
        showCreateDbWizard()

    Application.Top.Add(loginWindow)
    Application.Run()
    Application.Shutdown();
    0
