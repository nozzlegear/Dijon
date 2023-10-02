namespace Dijon.Bot.Tests

open System.Threading.Tasks
open Faqt
open Faqt.AssertionHelpers
open System.Runtime.CompilerServices

[<Extension>]
type FunctionAssertions =
    /// Asserts that the subject does not throw.
    [<Extension>]
    static member NotThrowAsync(t: Testable<unit -> Task<'result>>, ?because) : AndDerived<unit -> Task<'result>, 'result> =
        use _ = t.Assert()

        try
            let result = t.Subject()
                         |> Async.AwaitTask
                         |> Async.RunSynchronously
            AndDerived<unit -> Task<'result>, 'result>(t, result)
        with ex ->
            t.With("But threw", ex).Fail(because)
