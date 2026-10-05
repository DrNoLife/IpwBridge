using System.Net.Http.Headers;

namespace IpwBridge.Contracts;

/// <summary>
/// A request to attach one or more files (binfiles) to an existing object.
/// </summary>
/// <remarks>
/// IpwBridge reads the streams but never disposes them; the caller owns them. Seekable streams are uploaded from
/// the beginning (position 0), whatever their current position. Non-seekable streams are first copied to a
/// temporary file, so they can be hashed, sized and resent if the token has to be refreshed.
/// </remarks>
/// <example>
/// <code language="csharp"><![CDATA[
/// await using var file = File.OpenRead("report.pdf");
/// var request = new BinfileUploadRequest { ParentId = 2605115 }
///     .AddFile("file_1", file, contentType: "application/pdf");
/// var response = await client.UploadBinfileAsync(request);
/// ]]></code>
/// </example>
public sealed class BinfileUploadRequest
{
    private static readonly string[] ReservedKeys = ["parentid", "token", "checksum"];

    /// <summary>Gets or sets the id of the object the files are attached to.</summary>
    public int ParentId { get; set; }

    /// <summary>Gets the files to upload.</summary>
    public IList<BinfileUploadFile> Files { get; } = [];

    /// <summary>Adds a file and returns this request, so files can be chained.</summary>
    /// <param name="key">A unique key for the file, for example <c>file_1</c>. The response maps keys to new binfile ids.</param>
    /// <param name="content">The file content.</param>
    /// <param name="fileName">The file name stored in Metazo. Defaults to the name of a <see cref="FileStream"/>, otherwise the key.</param>
    /// <param name="contentType">The MIME type. Defaults to <c>application/octet-stream</c>.</param>
    /// <returns>This request.</returns>
    public BinfileUploadRequest AddFile(string key, Stream content, string? fileName = null, string? contentType = null)
    {
        Files.Add(new BinfileUploadFile(key, content) { FileName = fileName, ContentType = contentType });
        return this;
    }

    /// <summary>Throws if the request cannot be sent.</summary>
    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ParentId, nameof(ParentId));

        if (Files.Count == 0)
        {
            throw new ArgumentException("At least one file must be added.", nameof(Files));
        }

        HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Files)
        {
            ArgumentNullException.ThrowIfNull(file, nameof(Files));
            ArgumentException.ThrowIfNullOrWhiteSpace(file.Key, nameof(Files));
            ArgumentNullException.ThrowIfNull(file.Content, nameof(Files));

            if (!file.Content.CanRead)
            {
                throw new ArgumentException($"The stream for '{file.Key}' is not readable.", nameof(Files));
            }

            if (ReservedKeys.Contains(file.Key, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"'{file.Key}' is a reserved query parameter and cannot be used as a file key.", nameof(Files));
            }

            if (file.ContentType is not null && !MediaTypeHeaderValue.TryParse(file.ContentType, out _))
            {
                throw new ArgumentException($"'{file.ContentType}' is not a valid content type for '{file.Key}'.", nameof(Files));
            }

            if (!keys.Add(file.Key))
            {
                throw new ArgumentException($"The file key '{file.Key}' is used more than once.", nameof(Files));
            }
        }
    }
}

/// <summary>
/// One file in a <see cref="BinfileUploadRequest"/>.
/// </summary>
/// <param name="Key">A unique key for the file, for example <c>file_1</c>.</param>
/// <param name="Content">The file content. The caller owns and disposes the stream.</param>
public sealed record BinfileUploadFile(string Key, Stream Content)
{
    /// <summary>Gets the file name stored in Metazo. Defaults to the name of a <see cref="FileStream"/>, otherwise the key.</summary>
    public string? FileName { get; init; }

    /// <summary>Gets the MIME type. Defaults to <c>application/octet-stream</c>.</summary>
    public string? ContentType { get; init; }
}
