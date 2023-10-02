module Utils

open System.Runtime.InteropServices
open System.Diagnostics

let openUrl (url: string) =
    try
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            let modifiedUrl = url.Replace("&", "^&")
            let psi = ProcessStartInfo("cmd", sprintf "/c start %s" modifiedUrl)
            psi.CreateNoWindow <- true
            Process.Start(psi)
        elif RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
            let psi = ProcessStartInfo("xdg-open", url)
            psi.RedirectStandardError <- true
            psi.RedirectStandardOutput <- true
            psi.CreateNoWindow <- true
            psi.UseShellExecute <- false
            Process.Start(psi)
        elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
            Process.Start("open", url) 
        else
             raise (System.NotSupportedException("OS not supported"))
    with
    | ex -> raise ex
        