using System.Text.Json;
using System.Text.Json.Nodes;

namespace Rudoger.Modules.Logging.Presentation;

public sealed class SensitiveDataMasker
{
    private const string Mask = "***MASKED***";

    private readonly HashSet<string> _sensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "authorization",
        "cookie",
        "accessToken",
        "refreshToken",
        "jwt",
        "token",
    };

    public string MaskJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        try
        {
            JsonNode? node = JsonNode.Parse(value);
            MaskNode(node);
            return node?.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? value;
        }
        catch (JsonException)
        {
            return "[UNPARSEABLE BODY OMITTED]";
        }
    }

    public string MaskHeaders(IHeaderDictionary headers)
    {
        var safe = headers.ToDictionary(
            pair => pair.Key,
            pair => _sensitiveNames.Contains(pair.Key) ? Mask : pair.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);
        return JsonSerializer.Serialize(safe);
    }

    public string MaskPathAndQuery(HttpRequest request)
    {
        var query = request.Query.ToDictionary(
            pair => pair.Key,
            pair => _sensitiveNames.Contains(pair.Key) ? Mask : pair.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);
        string queryString = query.Count == 0
            ? string.Empty
            : QueryString.Create(query.Select(item => new KeyValuePair<string, string?>(item.Key, item.Value))).Value
                ?? string.Empty;
        return string.Concat(request.PathBase, request.Path, queryString);
    }

    private void MaskNode(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach ((string name, JsonNode? child) in jsonObject.ToArray())
            {
                if (_sensitiveNames.Contains(name))
                {
                    jsonObject[name] = Mask;
                }
                else
                {
                    MaskNode(child);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (JsonNode? child in array)
            {
                MaskNode(child);
            }
        }
    }
}
