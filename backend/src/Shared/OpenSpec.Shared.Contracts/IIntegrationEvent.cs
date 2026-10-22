namespace OpenSpec.Shared.Contracts;

public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredAt { get; }
}
