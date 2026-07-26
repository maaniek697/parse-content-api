using System.Text.Json;
using System.Text.Json.Serialization;
using ParseContentApi.Models;
using ParseContentApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<ContentParserService>();


var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (JsonException ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new ErrorResponse(
            "error",
            "INVALID_PAYLOAD",
            $"Nie udało się odczytać żądania: {ex.Message}"));
    }
});

app.MapPost("/api/v1/parse-content", (
    ParseContentRequest request,
    ContentParserService parser) =>
{
    if (request.Type is null)
    {
        return Results.BadRequest(new ErrorResponse(
            "error",
            "UNSUPPORTED_TYPE",
            "Pole 'type' jest wymagane i musi być jedną z wartości: CSV, INTERNAL_JSON."));
    }

    if (string.IsNullOrWhiteSpace(request.Content))
    {
        return Results.BadRequest(new ErrorResponse(
            "error",
            "EMPTY_CONTENT",
            "Pole 'content' jest wymagane i musi zawierać dane zakodowane w Base64."));
    }

    var type = request.Type.Value;

    try
    {
        var decoded = parser.DecodeBase64(request.Content);
        var parsedData = parser.Parse(type, decoded);

        var response = new ParseContentResponse(
            "success",
            type,
            parsedData.Count,
            parsedData);

        return Results.Ok(response);
    }
    catch (ContentParsingException ex)
    {
        return Results.BadRequest(new ErrorResponse(
            "error",
            "PARSING_FAILED",
            ex.Message));
    }
});
app.Run();