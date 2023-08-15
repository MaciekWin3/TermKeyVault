module Program

open Terminal.Gui
open Components

[<EntryPoint>]
let initApp _ = 
    //Repo.prepareDb("test")
    //Repo.insertTestData()
    Application.Init()
    Colors.Base <- Colors.TopLevel
    Application.Top.Add(loginWindow)
    Application.Top.Add(menu)
    Application.Run()
    Application.Shutdown();
    0 
