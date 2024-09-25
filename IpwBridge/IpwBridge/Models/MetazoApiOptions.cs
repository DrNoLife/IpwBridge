namespace IpwBridge.Models;

public class MetazoApiOptions
{
    public string IpwUrl { get; set; } = String.Empty;
    public string IpwUser { get; set; } = String.Empty;
    public string IpwPassword { get; set; } = String.Empty;
    public string ChecksumSecret { get; set; } = String.Empty;
}
