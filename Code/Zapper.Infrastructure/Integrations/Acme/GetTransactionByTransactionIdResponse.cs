using Zapper.Domain.Payment;

namespace Zapper.Infrastructure.Integrations.Acme;

// Outcome is null when no call left the process, which is what an open circuit means
public sealed record GetTransactionByTransactionIdResponse(
    AcmeAnswer Answer,
    AcmeInteractionOutcome? Outcome,
    int? HttpStatus,
    string? Body,
    DateTimeOffset StartedAt,
    TimeSpan Duration);
