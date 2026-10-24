using System.Security.Cryptography;
using System.Text;

namespace Zapper.ApiCommon.Managers.Cryptography;

public sealed class CryptographyManager : ICryptographyManager
{
    // a new algorithm or a new key is a new version here, so every stored value stays readable
    private const byte AesGcmVersion = 1;

    private const int VersionSize = 1;

    private const int NonceSize = 12;

    private const int TagSize = 16;

    public byte[] Encrypt(string plainText, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var stored = new byte[VersionSize + NonceSize + plainBytes.Length + TagSize];
        var nonce = stored.AsSpan(VersionSize, NonceSize);
        var cipherBytes = stored.AsSpan(VersionSize + NonceSize, plainBytes.Length);
        var tag = stored.AsSpan(VersionSize + NonceSize + plainBytes.Length, TagSize);

        stored[0] = AesGcmVersion;
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag, associatedData);

        return stored;
    }

    public string Decrypt(ReadOnlySpan<byte> cipherText, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData)
    {
        if (cipherText.Length < VersionSize + NonceSize + TagSize || cipherText[0] != AesGcmVersion)
        {
            throw new CryptographicException("The stored value is not in a form this manager reads.");
        }

        var nonce = cipherText.Slice(VersionSize, NonceSize);
        var cipherBytes = cipherText[(VersionSize + NonceSize)..^TagSize];
        var tag = cipherText[^TagSize..];
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes, associatedData);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
