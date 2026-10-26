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

    // Appendix A's worked example, as the Transaction Status contract document quotes it, and the secret the brief
    // signs it with
    private const string WorkedExampleBody =
        "{\"transactionUUID\":\"7f3a9c12-4b8e-4d21-9f0a-6c5e2d81b4a7\",\"transactionType\":\"Reversal\","
        + "\"transactionDateTime\":\"2026-03-04T08:02:11.417\",\"rrn\":\"MR43851259\",\"transactionAmount\":1999,"
        + "\"currencyCode\":\"ZAR\",\"txResultCode\":\"00\",\"isApproved\":true}";

    private const string WorkedExampleSecret = "k7Qf2mZr9XpL4vNcA8sTgH3wYbE6dJ5u";

    private const string WorkedExampleSignature = "KURAJlPrV3BWvWzsGBgr0EzClUSfu3itG62dM5Lqy/Y=";

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

    [Fact]
    public void GetSha256Hash_KnownText_ReturnsItsKnownHash()
    {
        var manager = new CryptographyManager();

        var result = manager.GetSha256Hash("abc");

        Convert.ToHexStringLower(result).ShouldBe("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
    }

    [Fact]
    public void GetHmacSha256Signature_AppendixAWorkedExample_ReturnsItsSignature()
    {
        var manager = new CryptographyManager();

        var result = manager.GetHmacSha256Signature(
            Encoding.UTF8.GetBytes(WorkedExampleBody),
            Encoding.UTF8.GetBytes(WorkedExampleSecret));

        Convert.ToBase64String(result).ShouldBe(WorkedExampleSignature);
    }

    [Fact]
    public void GetHmacSha256Signature_BodyChangedByOneByte_ReturnsAnotherSignature()
    {
        var manager = new CryptographyManager();
        var body = Encoding.UTF8.GetBytes(WorkedExampleBody);
        body[^2] = (byte)'E';

        var result = manager.GetHmacSha256Signature(body, Encoding.UTF8.GetBytes(WorkedExampleSecret));

        Convert.ToBase64String(result).ShouldNotBe(WorkedExampleSignature);
    }
}
