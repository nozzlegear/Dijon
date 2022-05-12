namespace Dijon
open System.IO
open Microsoft.Extensions.Configuration

type ConfigurationSecrets(configManager: IConfiguration) =
    /// Attempts to load a secret value from a secret file at /run/secrets/{key}` (set by Docker or Podman).
    member _.GetSecretFromFile (key: string): string option =
        let path = sprintf "/run/secrets/%s" key

        match File.Exists path with
        | false -> 
            None
        | true ->
            Some (File.ReadAllText path)

    /// Attempts to load a value from environment variables, falling back to run secrets (set by Docker or Podman) if the environment variable cannot be found.
    member x.GetValue (key: string): string option =
        match configManager.GetValue<string>(key) with
        | null
        | "" ->
            // The key was not found in environment variables, fall back to run secrets
            x.GetSecretFromFile key
        | value ->
            Some value

    /// Attempts to load a value from environment variables, falling back to run secrets (set by Docker or Podman) if the environment variable cannot be found. If the run secret also does not exist, an exception is thrown.
    member x.GetValueOrFail (key: string): string =
        match x.GetValue key with
        | Some value -> 
            value
        | None -> 
            failwithf "Could not find environment variable or run secret \"%s\"" key
