using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Zapper.IntegrationTests;

public abstract class AcceptanceTestBase
{
    protected static string Discriminator
    {
        get
        {
            // xUnit sets the test method for as long as a test runs, and only a running test reads this
            var testMethod = TestContext.Current.TestMethod!;
            var identity = $"{testMethod.TestClass.TestClassName}.{testMethod.MethodName}";

            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..12];
        }
    }
}
