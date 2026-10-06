using System.ComponentModel.DataAnnotations;
using IndiaWorks.Domain;

namespace IndiaWorks.Api.Options;

public sealed class IndiaWorksApiOptions
{
    [Required]
    public string ServiceName { get; init; } = ProductInfo.Name;

    [Required]
    public string ArchitectureBaseline { get; init; } = ProductInfo.ArchitectureBaseline;
}
