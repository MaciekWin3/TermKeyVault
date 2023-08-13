module Program

open Terminal.Gui
open Windows


[<EntryPoint>]
let initApp _ = 
    Repo.createDb()
    //Repo.insertTestData()
    Application.Init()
    Colors.Base <- Colors.TopLevel
    Application.Top.Add(mainWindow)
    Application.Top.Add(Components.menu)
    Application.Run()
    Application.Shutdown();
    0 
