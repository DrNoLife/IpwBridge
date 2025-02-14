namespace IpwBridge.Models;

public sealed class MetazoApiOptions
{
    public string IpwUrl { get; set; } = String.Empty;
    public string IpwUser { get; set; } = String.Empty;
    public string IpwPassword { get; set; } = String.Empty;
    public string ChecksumSecret { get; set; } = String.Empty;
}
