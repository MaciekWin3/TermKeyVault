module Program

open Terminal.Gui
open Components
open Repo

[<EntryPoint>]
let initApp _ = 
    //Repo.prepareDb("test")
    //Repo.insertTestData()
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
