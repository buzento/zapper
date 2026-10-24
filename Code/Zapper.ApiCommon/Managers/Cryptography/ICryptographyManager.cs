namespace Zapper.ApiCommon.Managers.Cryptography;

public interface ICryptographyManager
{
    byte[] Encrypt(string plainText, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData);

    string Decrypt(ReadOnlySpan<byte> cipherText, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData);

    byte[] GetSha256Hash(string text);

    byte[] GetHmacSha256Signature(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key);
}
