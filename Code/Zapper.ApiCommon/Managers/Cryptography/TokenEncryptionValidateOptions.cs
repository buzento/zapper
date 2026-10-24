using Microsoft.Extensions.Options;

namespace Zapper.ApiCommon.Managers.Cryptography;

public sealed class TokenEncryptionValidateOptions : IValidateOptions<TokenEncryptionOptions>
{
    private const int KeySize = 32;

    public ValidateOptionsResult Validate(string? name, TokenEncryptionOptions options)
    {
        // one byte more than the key, so a value that decodes to more than 32 bytes does not fit and is refused
        var decoded = new byte[KeySize + 1];

        return Convert.TryFromBase64String(options.Key, decoded, out var written) && written == KeySize
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("Key must be 32 bytes in Base64.");
    }
}
