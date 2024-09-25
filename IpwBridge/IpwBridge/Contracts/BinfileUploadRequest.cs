namespace IpwBridge.Contracts;

public class BinfileUploadRequest
{
    public int ParentId { get; set; }
    public Dictionary<string, Stream> Files { get; set; } = [];
}
