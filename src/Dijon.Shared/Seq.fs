namespace Dijon.Shared

[<RequireQualifiedAccess>]
module Seq = 
    let randomItem list = 
        Seq.sortBy (fun _ -> System.Guid.NewGuid()) list 
        |> Seq.head
