namespace DDD.Application.Common;

public interface IFailureSimulator
{
    bool Enabled { get; set; }
    bool ConsumeFailure();
}
public sealed class FailureSimulator : IFailureSimulator
{
    private int _enabled;

    public bool Enabled
    {
        get => Volatile.Read(ref _enabled) == 1;
        set => Interlocked.Exchange(ref _enabled, value ? 1 : 0);
    }

    public bool ConsumeFailure() => Interlocked.Exchange(ref _enabled, 0) == 1;
}