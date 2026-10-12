using System.Diagnostics.Metrics;
using DDD.Eventing.Contracts;

namespace DDD.Infrastructure.Monitoring;

public sealed class DddMetrics : IDomainEventMetrics, IDisposable
{
    public const string MeterName = "DDD.Application";

    private readonly Meter _meter = new(MeterName);

    private readonly Counter<long> _eventsProcessed;
    private readonly Counter<long> _eventsFailed;
    private readonly Counter<long> _outboxPublished;

    private long _eventClaimsInProgress;
    private long _eventClaimsExpired;

    public DddMetrics()
    {
        _eventsProcessed = _meter.CreateCounter<long>(
            "ddd_events_processed",
            description: "Successfully processed events");

        _eventsFailed = _meter.CreateCounter<long>(
            "ddd_events_failed",
            description: "Failed event processing attempts");

        _outboxPublished = _meter.CreateCounter<long>(
            "ddd_outbox_published",
            description: "Successfully published outbox messages");

        _meter.CreateObservableGauge(
            "ddd_event_claims_in_progress",
            () => Volatile.Read(ref _eventClaimsInProgress),
            description: "Events currently claimed for processing");

        _meter.CreateObservableGauge(
            "ddd_event_claims_expired",
            () => Volatile.Read(ref _eventClaimsExpired),
            description: "Unfinished events with expired claims");
    }

    public void EventProcessed() =>
        _eventsProcessed.Add(1);

    public void EventFailed() =>
        _eventsFailed.Add(1);

    public void OutboxPublished() =>
        _outboxPublished.Add(1);

    public void SetEventClaimCounts(long inProgress, long expired)
    {
        Interlocked.Exchange(ref _eventClaimsInProgress, inProgress);
        Interlocked.Exchange(ref _eventClaimsExpired, expired);
    }

    public void Dispose() => _meter.Dispose();
}