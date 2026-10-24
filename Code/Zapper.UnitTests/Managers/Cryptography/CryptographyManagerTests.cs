using System.Security.Cryptography;
using System.Text;
using Zapper.ApiCommon.Managers.Cryptography;
using Shouldly;
using Xunit;

namespace Zapper.UnitTests.Managers.Cryptography;

public sealed class CryptographyManagerTests
{
    private const string PlainText = "test-token-0001";

    private static readonly byte[] Key = [.. Enumerable.Range(0, 32).Select(value => (byte)value)];

    private static readonly byte[] OtherKey = [.. Enumerable.Range(100, 32).Select(value => (byte)value)];

    private static readonly byte[] AssociatedData = Encoding.UTF8.GetBytes("CUS-0001");

    [Fact]
    public void Decrypt_ValueEncryptedWithSameKeyAndAssociatedData_ReturnsPlainText()
    {
        var manager = new CryptographyManager();
        var cipherText = manager.Encrypt(PlainText, Key, AssociatedData);

        var result = manager.Decrypt(cipherText, Key, AssociatedData);

        result.ShouldBe(PlainText);
    }

    [Fact]
    public void Encrypt_AnyValue_WritesVersionNonceCipherTextAndTag()
    {
        var manager = new CryptographyManager();

        var result = manager.Encrypt(PlainText, Key, AssociatedData);

        result[0].ShouldBe((byte)1);
        result.Length.ShouldBe(1 + 12 + Encoding.UTF8.GetByteCount(PlainText) + 16);
    }

    [Fact]
    public void Encrypt_SameValueTwice_ReturnsDifferentCipherTexts()
    {
        var manager = new CryptographyManager();
        var first = manager.Encrypt(PlainText, Key, AssociatedData);

        var result = manager.Encrypt(PlainText, Key, AssociatedData);

        result.ShouldNotBe(first);
    }

    [Fact]
    public void Decrypt_OneByteChanged_ThrowsCryptographicException()
    {
        var manager = new CryptographyManager();
        var cipherText = manager.Encrypt(PlainText, Key, AssociatedData);
        cipherText[14] ^= 1;

        var decrypt = () => manager.Decrypt(cipherText, Key, AssociatedData);

        decrypt.ShouldThrow<CryptographicException>();
    }

    [Fact]
    public void Decrypt_WrongAssociatedData_ThrowsCryptographicException()
    {
        var manager = new CryptographyManager();
        var cipherText = manager.Encrypt(PlainText, Key, AssociatedData);

        var decrypt = () => manager.Decrypt(cipherText, Key, Encoding.UTF8.GetBytes("CUS-0002"));

        decrypt.ShouldThrow<CryptographicException>();
    }

    [Fact]
    public void Decrypt_WrongKey_ThrowsCryptographicException()
    {
        var manager = new CryptographyManager();
        var cipherText = manager.Encrypt(PlainText, Key, AssociatedData);

        var decrypt = () => manager.Decrypt(cipherText, OtherKey, AssociatedData);

        decrypt.ShouldThrow<CryptographicException>();
    }

    [Fact]
    public void Decrypt_UnknownVersion_ThrowsCryptographicException()
    {
        var manager = new CryptographyManager();
        var cipherText = manager.Encrypt(PlainText, Key, AssociatedData);
        cipherText[0] = 2;

        var decrypt = () => manager.Decrypt(cipherText, Key, AssociatedData);

        decrypt.ShouldThrow<CryptographicException>();
    }
}
