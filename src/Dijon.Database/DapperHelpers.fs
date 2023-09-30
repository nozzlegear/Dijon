namespace Dijon.Database

open System.Data.Common
open System.Threading.Tasks
open Dijon.Database

open Dapper
open Microsoft.Data.SqlClient
open Microsoft.Extensions.Options

type IDapperHelpers =
    abstract member Query: sql: string * data: SqlParams -> Task<'t seq>
    abstract member QuerySingle: sql: string * data: SqlParams -> Task<'t>
    abstract member QuerySingleOrNone: sql: string * data: SqlParams -> Task<'t option>
    abstract member Execute: sql: string * data: SqlParams -> Task<int>
    abstract member ExecuteReader: sql: string * data: SqlParams -> Task<DbDataReader>
    abstract member IgnoreResult: job: Task<_> -> Task<unit>

type DapperHelpers (options: IOptions<ConnectionStrings>) =
    let connectionString = options.Value.DefaultConnection

    interface IDapperHelpers with
        member _.Query(sql, data) = task {
            use conn = new SqlConnection(connectionString)
            return! conn.QueryAsync<_>(sql, data)
        }
        member _.QuerySingle(sql, data) = task {
            use conn = new SqlConnection(connectionString)
            return! conn.QuerySingleAsync<_>(sql, data)
        }
        member _.QuerySingleOrNone(sql, data) = task {
            use conn = new SqlConnection(connectionString)
            let! result = conn.QuerySingleOrDefault<_>(sql, data)
            if isNull (box result) then return None else return Some result
        }
        member _.Execute(sql, data) = task {
            use conn = new SqlConnection(connectionString)
            return! conn.ExecuteAsync(sql, data)
        }
        member _.ExecuteReader(sql, data) = task {
            use conn = new SqlConnection(connectionString)
            return! conn.ExecuteReaderAsync(sql, data)
        }
        member _.IgnoreResult(job) = task {
            let! _ = job
            ()
        }
    end
