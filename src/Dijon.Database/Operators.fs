[<AutoOpen>]
module private Dijon.Database.Operators

/// Boxes the key and value into a KeyValuePair<string, obj>
let (=>) a b = a, box b
