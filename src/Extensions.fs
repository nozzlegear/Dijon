namespace Dijon

[<AutoOpen>]
module Extensions = 
    type Async with 
        static member Empty = async {()}

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