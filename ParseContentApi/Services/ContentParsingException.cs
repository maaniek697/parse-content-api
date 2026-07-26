using System.Text;
using System.Text.Json;
using ParseContentApi.Models;

namespace ParseContentApi.Services;

public sealed class ContentParserService
{
    public string DecodeBase64(string base64Content)
    {
        if (string.IsNullOrWhiteSpace(base64Content))
        {
            throw new ContentParsingException("Pole 'content' nie może być puste.");
        }

        try
        {
            var bytes = Convert.FromBase64String(base64Content);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException ex)
        {
            throw new ContentParsingException(
                "Nieprawidłowy format Base64 w polu 'content'.", ex);
        }
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Parse(ContentType type, string decodedText)
    {
        return type switch
        {
            ContentType.CSV => ParseCsv(decodedText),
            ContentType.INTERNAL_JSON => ParseInternalJson(decodedText),
            _ => throw new ContentParsingException($"Nieobsługiwany typ zawartości: {type}.")
        };
    }

    private IReadOnlyList<IReadOnlyDictionary<string, object?>> ParseCsv(string csvText)
    {
        var rows = SplitCsvIntoRows(csvText);

        if (rows.Count == 0)
        {
            throw new ContentParsingException("Zawartość CSV jest pusta.");
        }

        var headers = rows[0];
        if (headers.Count == 0 || headers.All(string.IsNullOrWhiteSpace))
        {
            throw new ContentParsingException("Nagłówki CSV są puste lub nieprawidłowe.");
        }

        var result = new List<IReadOnlyDictionary<string, object?>>();

        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var fields = rows[rowIndex];

            if (fields.Count == 1 && string.IsNullOrWhiteSpace(fields[0]))
            {
                continue;
            }

            if (fields.Count != headers.Count)
            {
                throw new ContentParsingException(
                    $"Wiersz {rowIndex + 1} zawiera {fields.Count} kolumn, oczekiwano {headers.Count}.");
            }

            var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var col = 0; col < headers.Count; col++)
            {
                record[headers[col].Trim()] = fields[col];
            }

            result.Add(record);
        }

        return result;
    }

    private static List<List<string>> SplitCsvIntoRows(string csvText)
    {
        var rows = new List<List<string>>();
        var currentRow = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < csvText.Length; i++)
        {
            var c = csvText[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < csvText.Length && csvText[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;
                case ',':
                    currentRow.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    currentRow.Add(field.ToString());
                    field.Clear();
                    rows.Add(currentRow);
                    currentRow = new List<string>();
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        if (field.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(field.ToString());
            rows.Add(currentRow);
        }

        while (rows.Count > 0 && rows[^1].Count == 1 && string.IsNullOrWhiteSpace(rows[^1][0]))
        {
            rows.RemoveAt(rows.Count - 1);
        }

        return rows;
    }

    private IReadOnlyList<IReadOnlyDictionary<string, object?>> ParseInternalJson(string jsonText)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(jsonText);
        }
        catch (JsonException ex)
        {
            throw new ContentParsingException("Zdekodowana treść nie jest poprawnym JSON-em.", ex);
        }

        using (document)
        {
            var root = document.RootElement;

            JsonElement arrayElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                arrayElement = root;
            }
            else if (root.ValueKind == JsonValueKind.Object &&
                     (root.TryGetProperty("records", out arrayElement) ||
                      root.TryGetProperty("items", out arrayElement)))
            {
                if (arrayElement.ValueKind != JsonValueKind.Array)
                {
                    throw new ContentParsingException(
                        "Pole 'records'/'items' w INTERNAL_JSON musi być tablicą.");
                }
            }
            else
            {
                throw new ContentParsingException(
                    "INTERNAL_JSON musi być tablicą obiektów lub obiektem zawierającym pole 'records'/'items'.");
            }

            var result = new List<IReadOnlyDictionary<string, object?>>();
            var index = 0;

            foreach (var element in arrayElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    throw new ContentParsingException(
                        $"Element o indeksie {index} w INTERNAL_JSON nie jest obiektem.");
                }

                var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in element.EnumerateObject())
                {
                    record[property.Name] = ConvertJsonElement(property.Value);
                }

                result.Add(record);
                index++;
            }

            return result;
        }
    }

    private static object? ConvertJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
        JsonValueKind.Object => element.EnumerateObject()
            .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
        _ => element.GetRawText()
    };
}