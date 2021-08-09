namespace Dijon

module Seq = 
    let randomItem list = 
        Seq.sortBy (fun _ -> System.Guid.NewGuid()) list 
        |> Seq.head
