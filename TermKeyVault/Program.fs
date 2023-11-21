module Program

open Terminal.Gui
open Repo
open Orchestrator.LoginWindow
open Orchestrator.CreateDatabaseWizard

let setupSQLite() =
    SQLitePCL.Batteries.Init()
    SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlcipher())

let setupCache() = ()

[<EntryPoint>]
let initApp _ =
    setupSQLite()
    Application.Init()
    Colors.Base <- Colors.TopLevel
    let isDbCreated = checkIfDbExists ()

    match isDbCreated with
    | true -> 
        Application.Top.Add(loginWindow())
    | false -> 
        showCreateDbWizard ()

    Application.Run()
    Application.Shutdown()
    0
