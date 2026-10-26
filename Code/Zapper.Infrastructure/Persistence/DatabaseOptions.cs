using System.ComponentModel.DataAnnotations;

namespace Zapper.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    [Required]
    public string ConnectionString { get; init; } = string.Empty;
}
