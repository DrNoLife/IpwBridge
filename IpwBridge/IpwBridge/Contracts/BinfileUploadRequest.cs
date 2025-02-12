namespace IpwBridge.Contracts;

/// <summary>
/// Represents a request to upload binary files (binfiles) to the IPW Metazo API.
/// </summary>
/// <remarks>
/// The <see cref="BinfileUploadRequest"/> class contains the parent object ID to which the files are associated,
/// along with a collection of file streams identified by a key.
/// </remarks>
public class BinfileUploadRequest
{
    /// <summary>
    /// Gets or sets the parent identifier with which the binary files are associated.
    /// </summary>
    public int ParentId { get; set; }

    /// <summary>
    /// Gets or sets the collection of files to be uploaded.
    /// </summary>
    /// <remarks>
    /// The dictionary key is used to identify the file on the server, and the value is a stream containing the file data.
    /// </remarks>
    public Dictionary<string, Stream> Files { get; set; } = new Dictionary<string, Stream>();
}