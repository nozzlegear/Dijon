namespace Dijon.Shared

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

[<RequireQualifiedAccess>]
module Async =
    let Empty = async {()}

    let Wrap value = async { return value }

    /// <summary>
    /// Waits for the <see cref="computation" /> to complete, then calls the <see cref="fn" /> function.
    /// </summary>
    let Iter fn computation = async {
        let! result = computation 

        fn result 
        
        return result
    }

    let Map fn computation = async {
        let! result = computation 
        
        return fn result 
    }

    let SeqMap fn (computation: Async<'t seq>) = async {
        let! result = computation
        return Seq.map fn result
    }

    let SeqCollect fn (computation: Async<'t>) = async {
        let! result = computation 
        return Seq.collect fn result 
    }

    /// <summary>
    /// Iterates over a list of async computations, executing them sequentially.
    /// </summary>
    let Sequential computations = async {
        for fn in computations do
            do! fn()
    }
