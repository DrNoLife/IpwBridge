using Microsoft.Extensions.Options;

namespace IpwBridge.Models;

/// <summary>Validates <see cref="MetazoApiOptions"/> at startup.</summary>
internal sealed class MetazoApiOptionsValidator : IValidateOptions<MetazoApiOptions>
{
    public ValidateOptionsResult Validate(string? name, MetazoApiOptions options)
    {
        List<string> failures = [];

        if (!Uri.TryCreate(options.IpwUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            failures.Add($"{nameof(MetazoApiOptions.IpwUrl)} must be an absolute http(s) URL.");
        }

        if (string.IsNullOrWhiteSpace(options.IpwUser))
        {
            failures.Add($"{nameof(MetazoApiOptions.IpwUser)} is required.");
        }

        if (string.IsNullOrEmpty(options.IpwPassword))
        {
            failures.Add($"{nameof(MetazoApiOptions.IpwPassword)} is required.");
        }

        if (string.IsNullOrEmpty(options.ChecksumSecret))
        {
            failures.Add($"{nameof(MetazoApiOptions.ChecksumSecret)} is required.");
        }

        if (options.Site <= 0)
        {
            failures.Add($"{nameof(MetazoApiOptions.Site)} must be a positive site id.");
        }

        if (!IsValidTimeout(options.RequestTimeout))
        {
            failures.Add($"{nameof(MetazoApiOptions.RequestTimeout)} must be between 1 ms and 24 days, or infinite.");
        }

        if (!IsValidTimeout(options.BinfileTimeout))
        {
            failures.Add($"{nameof(MetazoApiOptions.BinfileTimeout)} must be between 1 ms and 24 days, or infinite.");
        }

        if (options.MaxRetryAttempts is < 0 or > 10)
        {
            failures.Add($"{nameof(MetazoApiOptions.MaxRetryAttempts)} must be between 0 and 10.");
        }

        if (options.RetryBaseDelay < TimeSpan.Zero || options.RetryBaseDelay > TimeSpan.FromMinutes(1))
        {
            failures.Add($"{nameof(MetazoApiOptions.RetryBaseDelay)} must be between 0 and 1 minute.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    // CancellationTokenSource.CancelAfter accepts at most int.MaxValue milliseconds.
    private static bool IsValidTimeout(TimeSpan timeout)
        => timeout == Timeout.InfiniteTimeSpan
            || (timeout >= TimeSpan.FromMilliseconds(1) && timeout <= TimeSpan.FromMilliseconds(int.MaxValue));
}
