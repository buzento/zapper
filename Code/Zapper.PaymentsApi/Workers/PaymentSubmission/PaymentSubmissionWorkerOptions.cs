using System.ComponentModel.DataAnnotations;

namespace Zapper.PaymentsApi.Workers.PaymentSubmission;

public sealed class PaymentSubmissionWorkerOptions
{
    [Required]
    public bool? IsEnabled { get; init; }
}
