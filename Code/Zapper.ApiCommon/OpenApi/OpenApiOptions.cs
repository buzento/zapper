using System.ComponentModel.DataAnnotations;

namespace Zapper.ApiCommon.OpenApi;

public sealed class OpenApiOptions
{
    [Required]
    public bool? IsEnabled { get; init; }

    [Required]
    public string Title { get; init; } = string.Empty;

    [Required]
    public string Description { get; init; } = string.Empty;
}
