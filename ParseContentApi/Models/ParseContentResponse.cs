namespace ParseContentApi.Models;

public sealed record ParseContentResponse(
    string Status,
    ContentType Type,
    int ProcessedCount,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Data
);

public sealed record ErrorResponse(
    string Status,
    string Error,
    string? Detail = null
);