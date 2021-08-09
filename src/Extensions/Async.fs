namespace Dijon

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
    
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
    /// Enumerates over a <see cref="IAsyncEnumerable" />, reading all items and mapping them to an F# sequence.
    /// </summary>
    let EnumerateCollection (collection: IAsyncEnumerable<_ IReadOnlyCollection>) =
        let cancellationToken = CancellationToken()
        let asTask (task : ValueTask<_>) = task.AsTask()
        let rec iterate (enumerator: IAsyncEnumerator<_ IReadOnlyCollection>) (gathered: _ seq) = async {
            let! shouldContinue =
                enumerator.MoveNextAsync cancellationToken
                |> asTask
                |> Async.AwaitTask

            if not shouldContinue then 
                return gathered
            else
                return! iterate enumerator (Seq.concat [gathered; seq enumerator.Current])
        }
        iterate (collection.GetAsyncEnumerator()) []

    /// <summary>
    /// Iterates over a list of async computations, executing them sequentially.
    /// </summary>
    let Sequential computations = async {
        for fn in computations do
            do! fn()
    }
