namespace Zapper.Domain.Enums;

public enum PaymentChangeSource
{
    Send = 1,
    Query = 2,
    Worker = 3,
    PaymentAdvice = 4,
    TransactionStatus = 5
}
