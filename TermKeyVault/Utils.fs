module Utils

open System.Runtime.InteropServices
open System.Diagnostics
open System.Xml.Linq
open System.IO
open System

open Types

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
    with ex ->
        raise ex

let createConfigFile (config: Config) =
    let appDataPath =
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)

    let configDir = Path.Combine(appDataPath, "termkeyvault")
    let configPath = Path.Combine(configDir, "config.xml")

    if not <| Directory.Exists(configDir) then
        Directory.CreateDirectory(configDir) |> ignore

    let xml =
        XDocument(
            XElement(
                "config",
                XElement("create_db", config.ShouldCreateDatabase.ToString()),
                XElement("db_path", config.DatabasePath),
                XElement("encryption_key", config.EncryptionKey.ToString())
            )
        )

    xml.Save(configPath)

let getConfig () =
    let appDataPath =
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)

    let configDir = Path.Combine(appDataPath, "termkeyvault")
    let configPath = Path.Combine(configDir, "config.xml")

    if File.Exists(configPath) = false then
        let defaultConfig: Config =
            { ShouldCreateDatabase = true
              DatabasePath = "default_database_path"
              EncryptionKey = 12345 // Replace with your default encryption key
            }

        createConfigFile (defaultConfig)

    let xml = XDocument.Load(configPath)

    let parseXml (xml: XDocument) =
        let configElement = xml.Element("config")
        let createDbElement = configElement.Element("create_db")
        let dbPathElement = configElement.Element("db_path")
        let encryptionKeyElement = configElement.Element("encryption_key")

        { ShouldCreateDatabase = createDbElement.Value.ToLower() = "true"
          DatabasePath = dbPathElement.Value
          EncryptionKey = int encryptionKeyElement.Value }

    let config = parseXml xml
    config

