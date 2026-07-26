using System.ComponentModel.DataAnnotations;

namespace ParseContentApi.Models;

public sealed class ParseContentRequest
{
    [Required]
    public ContentType? Type { get; set; }

    [Required]
    public string? Content { get; set; }
}