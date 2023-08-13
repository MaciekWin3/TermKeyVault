module Program
open Repo
open Terminal.Gui
open System.Data

let colors =
    new ColorScheme(
        Normal = Attribute.Make(Color.White, Color.Black),
        HotNormal = Attribute.Make(Color.White, Color.Black),
        HotFocus = Attribute.Make(Color.White, Color.Black),
        Focus = Attribute.Make(Color.White, Color.Black)
    )



Windows.mainWindow.Add(Components.categoryTable)
Windows.mainWindow.Add(Components.recordTable)
Windows.mainWindow.Add(Components.frameView)

[<EntryPoint>]
let initApp _ = 
    Repo.createDb()
    Application.Init()
    Colors.Base.Normal <- Application.Driver.MakeAttribute(Color.Green, Color.Black);
    Application.Top.Add(Windows.mainWindow)
    Application.Top.Add(Components.menu)
    Application.Run()
    Application.Shutdown();
    0 
