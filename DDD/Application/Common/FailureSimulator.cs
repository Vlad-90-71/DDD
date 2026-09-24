namespace DDD.Application.Common;

public interface IFailureSimulator
{
    bool Enabled { get; set; }
}
public sealed class FailureSimulator : IFailureSimulator
{
    public bool Enabled { get; set; }
}