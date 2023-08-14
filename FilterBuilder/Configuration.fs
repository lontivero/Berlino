module Configuration

open System.IO
open Microsoft.Extensions.Configuration
open NBitcoin

type Configuration = {
    Network: string
    DatabaseConnectionString: string
    RpcConnectionString: string
    FalsePositiveRate: int
    LogLevel: string
}

let network config =
    Network.GetNetwork(config.Network)

let load (configFile: string) (args : string[]) =
    let configurationRoot =
        ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configFile, true, true)
            .AddEnvironmentVariables("BERLINO")
            .AddCommandLine(args)
            .Build()

    configurationRoot.Get<Configuration>()
