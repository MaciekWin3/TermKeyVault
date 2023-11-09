module Program

open Terminal.Gui
open Repo
open Orchestrator.LoginWindow
open Orchestrator.CreateDatabaseWizard

[<EntryPoint>]
let initApp _ =
    Application.Init()
    Colors.Base <- Colors.TopLevel
    let isDbCreated = checkIfDbExists ()

    match isDbCreated with
    | true -> 
        Application.Top.Add(loginWindow)
    | false -> 
        showCreateDbWizard ()

    Application.Run()
    Application.Shutdown()
    0

