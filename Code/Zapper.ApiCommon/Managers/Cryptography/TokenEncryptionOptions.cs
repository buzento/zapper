using System.ComponentModel.DataAnnotations;

namespace Zapper.ApiCommon.Managers.Cryptography;

public sealed class TokenEncryptionOptions
{
    [Required]
    public string Key { get; init; } = string.Empty;
}
