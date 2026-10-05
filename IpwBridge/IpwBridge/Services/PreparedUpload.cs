using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using IpwBridge.Contracts;
using IpwBridge.Exceptions;

namespace IpwBridge.Services;

/// <summary>
/// The files of a <see cref="BinfileUploadRequest"/>, hashed and made replayable, so the upload can be resent
/// after a token refresh without buffering whole files in memory and without disposing the caller's streams.
/// </summary>
internal sealed class PreparedUpload : IAsyncDisposable
{
    private readonly List<Part> _parts;

    private PreparedUpload(List<Part> parts) => _parts = parts;

    /// <summary>Gets the checksum of each file, keyed by file key.</summary>
    public IEnumerable<KeyValuePair<string, string>> FileChecksums
        => _parts.Select(p => KeyValuePair.Create(p.Key, p.Checksum));

    public static async Task<PreparedUpload> PrepareAsync(BinfileUploadRequest request, CancellationToken cancellationToken)
    {
        List<Part> parts = [];
        try
        {
            foreach (var file in request.Files)
            {
                parts.Add(await PreparePartAsync(file, cancellationToken).ConfigureAwait(false));
            }

            return new PreparedUpload(parts);
        }
        catch
        {
            foreach (var part in parts)
            {
                await part.DisposeAsync().ConfigureAwait(false);
            }

            throw;
        }
    }

    /// <summary>Creates the multipart body. Each call rewinds the files, so it can be used once per attempt.</summary>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The parts are owned by the returned content, which the request disposes.")]
    public MultipartFormDataContent CreateContent()
    {
        MultipartFormDataContent content = new();
        foreach (var part in _parts)
        {
            part.Source.Position = part.StartPosition;
            StreamContent streamContent = new(new NonDisposingStream(part.Source));
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(part.ContentType);
            content.Add(streamContent, part.Key, part.FileName);
        }

        return content;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var part in _parts)
        {
            await part.DisposeAsync().ConfigureAwait(false);
        }
    }

    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms",
        Justification = "SHA1 of the first 256 bytes is mandated by the Metazo upload protocol.")]
    private static async Task<Part> PreparePartAsync(BinfileUploadFile file, CancellationToken cancellationToken)
    {
        Stream source = file.Content;
        FileStream? temporaryCopy = null;
        string? temporaryPath = null;

        try
        {
            if (!source.CanSeek)
            {
                // Spool to a temporary file: it is hashed, sized and replayed from disk instead of memory.
                // Only temporary-file failures are wrapped; read errors from the caller's stream propagate as they are,
                // exactly as they do for seekable streams.
                temporaryPath = SpoolFileAction(file, Path.GetTempFileName);
                temporaryCopy = SpoolFileAction(file, () => new FileStream(
                    temporaryPath,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 81920,
                    FileOptions.Asynchronous | FileOptions.DeleteOnClose));

                byte[] buffer = new byte[81920];
                int chunk;
                while ((chunk = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    FileStream target = temporaryCopy;
                    await SpoolFileActionAsync(file, () => target.WriteAsync(buffer.AsMemory(0, chunk), cancellationToken)).ConfigureAwait(false);
                }

                temporaryCopy.Position = 0;
                source = temporaryCopy;
            }

            // Uploads start at the beginning of the stream, as in 1.x, so a freshly written MemoryStream works.
            const long start = 0;
            source.Position = start;
            byte[] prefix = new byte[Constants.FileChecksumPrefixLength];
            int read = await source.ReadAtLeastAsync(prefix, prefix.Length, throwOnEndOfStream: false, cancellationToken)
                .ConfigureAwait(false);
            source.Position = start;

            string fileName = file.FileName
                ?? (file.Content is FileStream fileStream ? Path.GetFileName(fileStream.Name) : file.Key);

            return new Part(
                file.Key,
                fileName,
                file.ContentType ?? "application/octet-stream",
                source,
                start,
                Hex.ToLower(SHA1.HashData(prefix.AsSpan(0, read))),
                temporaryCopy);
        }
        catch
        {
            if (temporaryCopy is not null)
            {
                await temporaryCopy.DisposeAsync().ConfigureAwait(false);
            }
            else if (temporaryPath is not null)
            {
                File.Delete(temporaryPath);
            }

            throw;
        }
    }

    private static T SpoolFileAction<T>(BinfileUploadFile file, Func<T> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw SpoolFailed(file, ex);
        }
    }

    private static async Task SpoolFileActionAsync(BinfileUploadFile file, Func<ValueTask> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw SpoolFailed(file, ex);
        }
    }

    private static IpwBridgeException SpoolFailed(BinfileUploadFile file, Exception ex)
        => new($"The non-seekable stream for '{file.Key}' could not be copied to a temporary file: {ex.Message}", ex);

    private sealed record Part(
        string Key,
        string FileName,
        string ContentType,
        Stream Source,
        long StartPosition,
        string Checksum,
        FileStream? TemporaryCopy) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => TemporaryCopy?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}
