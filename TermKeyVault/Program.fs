module Program

open Terminal.Gui.App
open Terminal.Gui.Views
open Repo
open Utils.AppContext
open Orchestrator.LoginWindow
open Orchestrator.CreateDatabaseWizard
open Orchestrator.ScreenOrchestrator

let setupSQLite() =
    SQLitePCL.Batteries.Init()
    SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlcipher())

[<EntryPoint>]
let initApp _ =
    setupSQLite()

    let app = initialize ()
    let isDbCreated = checkIfDbExists ()

    match isDbCreated with
    | true -> 
        appRoot.Add(loginWindow()) |> ignore
    | false -> 
        showCreateDbWizard ()

    app.Run(appRoot, null) |> ignore
    0
