namespace Dijon

module String = 
    let startsWith (a: string) (b: string) = a.StartsWith(b, System.StringComparison.OrdinalIgnoreCase)
    let startsWithAny (a: string) = Seq.exists (startsWith a)
    let contains (a: string) (b: string) = a.Contains(b, System.StringComparison.OrdinalIgnoreCase)
    let containsAny (a: string) = Seq.exists (contains a)
    let lower (a: string) = a.ToLower()
    let trim (a: string) = a.Trim()
    let stripFirstWord (a: string) = a.Substring (a.IndexOf " " + 1) 
    let newlineJoin (list: string seq) = System.String.Join(System.Environment.NewLine, list)

