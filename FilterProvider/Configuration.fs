module Configuration

open System.IO
open Microsoft.Extensions.Configuration
open NBitcoin

type Configuration = {
    Network: string
    DatabaseConnectionString: string
    LogLevel: string
}

let network config =
    Network.GetNetwork(config.Network)

let load (configFile: string) (args : string[]) =
    let configurationRoot =
        ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configFile, true, true)
            .AddEnvironmentVariables("BERLINO_FILTER_PROVIDER")
            .AddCommandLine(args)
            .Build()

    configurationRoot.Get<Configuration>()
