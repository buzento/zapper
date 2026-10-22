using System.Security.Cryptography.X509Certificates;
using Npgsql;

namespace Zapper.Infrastructure.Persistence;

public static class DatabaseDataSourceFactory
{
    private const string CertificatesFolderName = "Certificates";

    private const string AuthorityFileName = "ca.crt";

    private const string ContainerCertificatesFolder = "/certificates";

    public static NpgsqlDataSource Get(string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.ConnectionStringBuilder.GssEncryptionMode = GssEncryptionMode.Disable;

        var folder = GetCertificatesFolder();

        var role = new NpgsqlConnectionStringBuilder(connectionString).Username
            ?? throw new InvalidOperationException(
                "ConnectionString must name this host's own database role in Username.");

        var authorityFile = Path.Combine(folder, AuthorityFileName);
        var certificateFile = Path.Combine(folder, $"{role}.crt");
        var keyFile = Path.Combine(folder, $"{role}.key");

        var missingFile = new[] { authorityFile, certificateFile, keyFile }
            .FirstOrDefault(file => !File.Exists(file));

        if (missingFile is not null)
        {
            throw new InvalidOperationException($"The database certificate file {missingFile} is missing.");
        }

        var authority = X509CertificateLoader.LoadCertificateFromFile(authorityFile);
        var roleCertificate = X509Certificate2.CreateFromPemFile(certificateFile, keyFile);

        builder.UseRootCertificate(authority);
        builder.UseSslClientAuthenticationOptionsCallback(
            sslOptions => sslOptions.ClientCertificates = [roleCertificate]);

        return builder.Build();
    }

    private static string GetCertificatesFolder()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);

        while (folder is not null)
        {
            if (folder.EnumerateFiles("*.slnx").Any())
            {
                return Path.Combine(folder.FullName, CertificatesFolderName);
            }

            folder = folder.Parent;
        }

        return ContainerCertificatesFolder;
    }
}
