namespace Zapper.ApiCommon.Managers.Cryptography;

public interface ICryptographyManager
{
    byte[] Encrypt(string plainText, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData);

    string Decrypt(ReadOnlySpan<byte> cipherText, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData);
}
