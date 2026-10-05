using System.Text.Json.Serialization.Metadata;

namespace IpwBridge.Models;

/// <summary>
/// Configuration for IpwBridge. Bind it from configuration or set it in <c>AddIpwBridge</c>.
/// </summary>
/// <remarks>The options are validated when the application starts; a missing or invalid value fails fast.</remarks>
/// <example>
/// <code language="json"><![CDATA[
/// "IpwBridge": {
///   "IpwUrl": "https://your.metazo.domain/metazo/api/v1/",
///   "IpwUser": "apiuser",
///   "IpwPassword": "secret",
///   "ChecksumSecret": "from the Metazo configuration menu"
/// }
/// ]]></code>
/// </example>
public sealed class MetazoApiOptions
{
    /// <summary>Gets or sets the absolute base URL of the API, for example <c>https://your.metazo.domain/metazo/api/v1/</c>. Required.</summary>
    public string IpwUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the Metazo user name used to authenticate. Required.</summary>
    public string IpwUser { get; set; } = string.Empty;

    /// <summary>Gets or sets the password of <see cref="IpwUser"/>. Required.</summary>
    public string IpwPassword { get; set; } = string.Empty;

    /// <summary>Gets or sets the checksum secret from the Metazo configuration menu. Required.</summary>
    public string ChecksumSecret { get; set; } = string.Empty;

    /// <summary>Gets or sets the Metazo site id. Defaults to 1.</summary>
    public int Site { get; set; } = 1;

    /// <summary>Gets or sets the language code sent when authenticating, for example <c>EN</c> or <c>DA</c>. Optional.</summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the datasource token (<c>usertoken</c>) used by the quickfilter endpoints. Only required for
    /// <c>GetFilterAsync</c> and <c>GetFilterCountAsync</c>.
    /// </summary>
    public string? DatasourceToken { get; set; }

    /// <summary>
    /// Gets or sets the timeout for one JSON request, including reading the response body. Defaults to 100 seconds.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>
    /// Gets or sets the timeout for one binfile upload or download, including the transfer. Defaults to 10 minutes.
    /// </summary>
    public TimeSpan BinfileTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets how many times a GET request (or the login) is retried after a transient failure: a network
    /// error, a timeout, HTTP 408, 429, 502, 503 or 504, or HTTP 500 without a structured Metazo error. Model runs
    /// and uploads are never retried this way. Defaults to 2; 0 disables retries.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 2;

    /// <summary>Gets or sets the delay before the first retry; it doubles for each further retry. Defaults to 500 ms.</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets or sets whether to authenticate with GET and credentials in the query string, as older Metazo
    /// versions did. Defaults to <see langword="false"/> (POST with a form body, as currently documented). Only
    /// enable this if your server rejects the POST login.
    /// </summary>
    public bool UseLegacyGetAuthentication { get; set; }

    /// <summary>
    /// Gets or sets an optional JSON metadata resolver (typically a <c>JsonSerializerContext</c>) for your own
    /// item types used with <c>GetListAsync&lt;T&gt;</c> and <c>GetItemAsync&lt;T&gt;</c>. When set, those types are
    /// deserialized without reflection, which is needed for trimming and Native AOT.
    /// </summary>
    public IJsonTypeInfoResolver? JsonTypeInfoResolver { get; set; }
}
