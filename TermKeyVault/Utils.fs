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

module Cache =
    open Microsoft.Extensions.Caching.Memory

    let mutable private cacheInstance = Unchecked.defaultof<IMemoryCache>

    let private initializeCache () =
        let cacheOptions = MemoryCacheOptions()
        new MemoryCache(cacheOptions) :> IMemoryCache

    let private getCache () =
        if cacheInstance = Unchecked.defaultof<IMemoryCache> then
            cacheInstance <- initializeCache()
        cacheInstance

    let addValueToCache (key: string, value: string) =
        let cache = getCache()
        cache.Set(key, value, DateTimeOffset.Now.AddMonths(12)) |> ignore

    let getValueFromCache (key: string): string option =
        let cache = getCache()

        match cache.TryGetValue key with
        | true, value -> Some(value |> string)
        | _ -> None 

module Configuration =
    open Terminal.Gui

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

    let openConfigFile () =
        let appDataPath =
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)

        let configDir = Path.Combine(appDataPath, "termkeyvault")
        let configPath = Path.Combine(configDir, "config.xml")

        let psi = new ProcessStartInfo()
        psi.FileName <- configPath

        let isWindows = System.Environment.OSVersion.Platform = PlatformID.Win32NT

        if isWindows then
            psi.UseShellExecute <- true
            psi.Verb <- "edit"
        elif
            System.Environment.OSVersion.Platform = PlatformID.Unix
            || System.Environment.OSVersion.Platform = PlatformID.MacOSX
        then
            psi.UseShellExecute <- false
            psi.FileName <- "xdg-open"

        let proc = new Process()
        proc.StartInfo <- psi

        proc.Start()

    let showConfig () =
        let config = getConfig ()

        let appDataPath =
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)

        let configDir = Path.Combine(appDataPath, "termkeyvault")

        MessageBox.Query(
            "Config",
            $"""
Config localization: {configDir}
Db path: {config.DatabasePath}
Create: {config.ShouldCreateDatabase}
Config: {config.EncryptionKey}
            """
        )
        |> ignore

    let getEncryptionKey () =
        let config = getConfig ()
        let key = config.EncryptionKey

        let encryptionKey =
            match key with
            | 0 -> 32
            | key -> key

        encryptionKey


