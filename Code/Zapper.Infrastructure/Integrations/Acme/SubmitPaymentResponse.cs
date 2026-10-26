using Zapper.Domain.Enums;
using Zapper.Domain.Models;

namespace Zapper.Infrastructure.Integrations.Acme;

// Outcome is null when no call left the process, which is what an open circuit means
public sealed record SubmitPaymentResponse(
    AcmeAnswer Answer,
    AcmeInteractionOutcome? Outcome,
    int? HttpStatus,
    string? Body,
    DateTimeOffset StartedAt,
    TimeSpan Duration);
