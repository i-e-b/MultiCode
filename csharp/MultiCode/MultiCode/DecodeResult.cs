namespace MultiCode;

/// <summary>
/// Result from RsDecode
/// </summary>
internal class DecodeResult
{
    public bool ok { get; set; }
    public bool errs { get; set; }
    public FlexArray? result { get; set; }
    public string info { get; set; } = "";
}