namespace IpwBridge.Models;

public sealed class IpwModel
{
    public string Username { get; set; } = String.Empty;
    public string Password { get; set; } = String.Empty;
    public string ChecksumSecret { get; set; } = String.Empty;
    public Uri MetazoUri { get; set; } = new(String.Empty);
}
