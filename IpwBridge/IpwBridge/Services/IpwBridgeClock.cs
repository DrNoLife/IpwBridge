namespace IpwBridge.Services;

/// <summary>
/// The time source IpwBridge uses: the application's registered <see cref="TimeProvider"/> if there is one,
/// otherwise <see cref="TimeProvider.System"/>. Wrapping it avoids registering a global <see cref="TimeProvider"/>.
/// </summary>
internal sealed class IpwBridgeClock(TimeProvider time)
{
    public TimeProvider Time { get; } = time;
}
