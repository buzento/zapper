namespace Zapper.Domain.Customer;

public sealed class StoredCardEntity
{
    // the token is encrypted before the card is made, with the customer reference as its associated data
    public StoredCardEntity(string customerReference, byte[] encryptedToken, string emailAddress)
    {
        Id = Guid.CreateVersion7();
        CustomerReference = customerReference;
        EncryptedToken = encryptedToken;
        EmailAddress = emailAddress;
    }

    public Guid Id { get; private set; }

    public string CustomerReference { get; private set; }

    public byte[] EncryptedToken { get; private set; }

    public string EmailAddress { get; private set; }
}
