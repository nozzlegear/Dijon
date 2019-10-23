namespace Dijon
open System.Collections.Generic
open System.Threading

[<AutoOpen>]
module Extensions = 
    type Async with 
        static member Empty = async {()}

        static member Wrap value = async { return value }

        /// <summary>
        /// Waits for the <see cref="computation" /> to complete, then calls the <see cref="fn" /> function.
        /// </summary>
        static member Iter fn computation = async {
            let! result = computation 

            fn result 
            
            return result
        }

        static member Map fn computation = async {
            let! result = computation 
            
            return fn result 
        }

        static member SeqMap fn (computation: Async<'t seq>) = async {
            let! result = computation
            return Seq.map fn result
        }

        static member SeqCollect fn (computation: Async<'t>) = async {
            let! result = computation 
            return Seq.collect fn result 
        }
    
        /// <summary>
        /// Enumerates over a <see cref="IAsyncEnumerable" />, reading all items and mapping them to an F# sequence.
        /// </summary>
        static member EnumerateCollection (collection: IAsyncEnumerable<_ IReadOnlyCollection>) = 
            let cancellationToken = CancellationToken()
            let rec iterate (enumerator: IAsyncEnumerator<_ IReadOnlyCollection>) (gathered: _ seq) = async {
                let! shouldContinue = enumerator.MoveNext cancellationToken |> Async.AwaitTask

                if not shouldContinue then 
                    return gathered
                else
                    return! iterate enumerator (Seq.concat [gathered; seq enumerator.Current])
            }
            iterate (collection.GetEnumerator()) []

        /// <summary>
        /// Iterates over a list of async computations, executing them sequentially.
        /// </summary>
        static member Sequential computations = async {
            for fn in computations do
                do! fn()
        }

module Seq = 
    let randomItem list = 
        Seq.sortBy (fun _ -> System.Guid.NewGuid()) list 
        |> Seq.head
    
module StringUtils = 
    let startsWith (a: string) (b: string) = a.StartsWith(b, System.StringComparison.OrdinalIgnoreCase)
    let startsWithAny (a: string) = Seq.exists (startsWith a)
    let contains (a: string) (b: string) = a.Contains(b, System.StringComparison.OrdinalIgnoreCase)
    let containsAny (a: string) = Seq.exists (contains a)
    let lower (a: string) = a.ToLower()
    let trim (a: string) = a.Trim()
    let stripFirstWord (a: string) = a.Substring (a.IndexOf " " + 1) 
    let newlineJoin (list: string seq) = System.String.Join(System.Environment.NewLine, list)