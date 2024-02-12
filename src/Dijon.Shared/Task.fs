namespace Dijon.Shared

open System.Runtime.InteropServices
open System.Threading

[<AutoOpen>]
module Extensions =
    open System.Threading.Tasks

    type Task with
        static member empty = task { () }

        static member toEmpty (job: Task) = task {
            do! job
            return! Task.empty
        }

        static member wrap value =
            Task.FromResult value

        static member map fn job = task {
            let! result = job
            return fn result
        }

        static member ignore job = task {
            let! _ = job
            ()
        }

        static member catch (job: Task<_>) = task {
            try
                let! result = job
                return Choice1Of2 result
            with e ->
                return Choice2Of2 e
        }

        static member catch (job: Task) =
            Task.toEmpty job
            |> Task.catch

        /// Run the task synchronously and await its result.
        static member runSynchronously (job: Task<_>) =
            Async.AwaitTask job
            |> Async.RunSynchronously

        static member runSynchronously (job: Task) =
            Async.AwaitTask job
            |> Async.RunSynchronously

        /// Iterates over a list of tasks, executing them sequentially. Uses <see cref="Task.WhenAll" /> under the hood.
        static member sequential (tasks: Task seq) =
            Task.WhenAll(tasks)

        /// Iterates over a list of tasks, executing them sequentially. Uses <see cref="Task.WhenAll" /> under the hood.
        static member sequential (tasks: Task<unit> seq) =
            let tasks =
                Seq.cast<Task> tasks
                |> Array.ofSeq
            Task.WhenAll(tasks)

        /// Maps the source enumerable to an array of tasks and executes them in parallel with <paramref name="maxDegreeOfParallelism"/>.
        /// Uses <see cref="Parallel.ForEachAsync"/> under the hood.
        static member runInParallel<'t> (maxDegreeOfParallelism: int, cancellationToken: CancellationToken, mapFn: 't -> CancellationToken -> Task) (source: 't seq) =
            let options = ParallelOptions(
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            )
            let mapper = System.Func<'t, CancellationToken, ValueTask>(fun item ct -> ValueTask (mapFn item ct))
            Parallel.ForEachAsync (source, options, mapper)

        /// Starts the asynchronous computation in the thread pool. Do not await its result
        static member start (task: Task<_>, ?cancellationToken: CancellationToken): unit =
            let cancellationToken =
                defaultArg cancellationToken Async.DefaultCancellationToken
            Async.Start(async {
                let! _ = Async.AwaitTask task
                ()
            }, cancellationToken)

        /// Starts the asynchronous computation in the thread pool. Do not await its result
        static member start (task: Task, ?cancellationToken: CancellationToken): unit =
            let cancellationToken =
                defaultArg cancellationToken Async.DefaultCancellationToken
            Async.Start(async {
                do! Async.AwaitTask task
            }, cancellationToken)
