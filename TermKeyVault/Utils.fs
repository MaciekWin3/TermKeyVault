module Utils

open System.Runtime.InteropServices
open System.Diagnostics
open System.Xml.Linq
open System.IO
open System

open Types

module Web =
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

module Configuration =
    let createConfigFile () =
        let appDataPath =
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)

        let configDir = Path.Combine(appDataPath, "termkeyvault")
        let configPath = Path.Combine(configDir, "config.xml")

        let config: Config =
            { ShouldCreateDatabase = true
              DatabasePath = Path.Combine(configDir, "termkeyvault.db")
              EncryptionKey = 42069 }

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
            createConfigFile ()

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

