module Windows

open Terminal.Gui
open Components

let loginWindow =
    let window = new Window(
        Title = "Login",
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill()
    )
    window

let mainWindow = 
    let window = new Window(
        Title = "TermKeyVault",
        X = 0,
        Y = 1,
        Width = Dim.Fill(),
        Height = Dim.Fill()
    )
    window.Add(categoryTable)
    window.Add(recordTable)
    window.Add(frameView)
    window
