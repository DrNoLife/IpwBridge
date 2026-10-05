namespace IpwBridge.Models;

/// <summary>
/// The configuration-bindable subset of <see cref="MetazoApiOptions"/> (everything except
/// <see cref="MetazoApiOptions.JsonTypeInfoResolver"/>), so binding can use the configuration source generator and
/// stays trimming- and AOT-safe. Properties left out of the configuration keep the option defaults.
/// </summary>
internal sealed class MetazoApiSettings
{
    public string? IpwUrl { get; set; }
    public string? IpwUser { get; set; }
    public string? IpwPassword { get; set; }
    public string? ChecksumSecret { get; set; }
    public int? Site { get; set; }
    public string? Language { get; set; }
    public string? DatasourceToken { get; set; }
    public TimeSpan? RequestTimeout { get; set; }
    public TimeSpan? BinfileTimeout { get; set; }
    public int? MaxRetryAttempts { get; set; }
    public TimeSpan? RetryBaseDelay { get; set; }
    public bool? UseLegacyGetAuthentication { get; set; }

    public void ApplyTo(MetazoApiOptions options)
    {
        options.IpwUrl = IpwUrl ?? options.IpwUrl;
        options.IpwUser = IpwUser ?? options.IpwUser;
        options.IpwPassword = IpwPassword ?? options.IpwPassword;
        options.ChecksumSecret = ChecksumSecret ?? options.ChecksumSecret;
        options.Site = Site ?? options.Site;
        options.Language = Language ?? options.Language;
        options.DatasourceToken = DatasourceToken ?? options.DatasourceToken;
        options.RequestTimeout = RequestTimeout ?? options.RequestTimeout;
        options.BinfileTimeout = BinfileTimeout ?? options.BinfileTimeout;
        options.MaxRetryAttempts = MaxRetryAttempts ?? options.MaxRetryAttempts;
        options.RetryBaseDelay = RetryBaseDelay ?? options.RetryBaseDelay;
        options.UseLegacyGetAuthentication = UseLegacyGetAuthentication ?? options.UseLegacyGetAuthentication;
    }
}
