using System.Text.Json;
using System.Text.Json.Nodes;

namespace SampleLightrApp;

internal static class RecordedMappingSanitizer
{
    private static readonly string[] RedactedProperties =
    [
        "address",
        "city",
        "company",
        "email",
        "external_number",
        "first_name",
        "label",
        "last_name",
        "name",
        "phone_number",
        "postal_code"
    ];

    private static readonly string[] NumericProperties =
    [
        "amount",
        "annual_volume",
        "annual_volume_shipped",
        "lower_limit",
        "pay_on_account_limit"
    ];

    public static void Sanitize(string mappingsDirectory)
    {
        if (!Directory.Exists(mappingsDirectory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(mappingsDirectory, "*.json"))
        {
            var mapping = JsonNode.Parse(File.ReadAllText(file))
                ?? throw new InvalidOperationException($"Could not parse recorded mapping '{file}'.");

            SanitizeNode(mapping, false);
            File.WriteAllText(file, mapping.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static void SanitizeNode(JsonNode node, bool insideTextVariables)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var (propertyName, value) in jsonObject.ToList())
            {
                if (value is null)
                {
                    continue;
                }

                if (propertyName.Equals("text_variables", StringComparison.OrdinalIgnoreCase))
                {
                    SanitizeNode(value, true);
                    continue;
                }

                if (propertyName.Equals("headers", StringComparison.OrdinalIgnoreCase)
                    && value is JsonArray headers)
                {
                    RemoveContentLengthMatcher(headers);
                    SanitizeNode(headers, false);
                    continue;
                }

                if (propertyName.Equals("name", StringComparison.OrdinalIgnoreCase)
                    && (jsonObject.ContainsKey("Pattern") || jsonObject.ContainsKey("Matchers")))
                {
                    continue;
                }

                if (IsSensitiveProperty(propertyName) || insideTextVariables)
                {
                    jsonObject[propertyName] = RedactedValue(value);
                    continue;
                }

                if (NumericProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase))
                {
                    jsonObject[propertyName] = 0;
                    continue;
                }

                if (propertyName.Contains("url", StringComparison.OrdinalIgnoreCase))
                {
                    jsonObject[propertyName] = "https://fixtures.invalid/resource";
                    continue;
                }

                SanitizeNode(value, false);
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                if (item is not null)
                {
                    SanitizeNode(item, insideTextVariables);
                }
            }
        }
    }

    private static bool IsSensitiveProperty(string propertyName)
    {
        return RedactedProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase)
            || propertyName.Contains("credential", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("signature", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("token", StringComparison.OrdinalIgnoreCase);
    }

    private static JsonNode RedactedValue(JsonNode value)
    {
        return value.GetValueKind() switch
        {
            JsonValueKind.Number => JsonValue.Create(0)!,
            JsonValueKind.True or JsonValueKind.False => JsonValue.Create(false)!,
            _ => JsonValue.Create("[redacted]")!
        };
    }

    private static void RemoveContentLengthMatcher(JsonArray headers)
    {
        foreach (var header in headers.OfType<JsonObject>().ToList())
        {
            if (header["Name"]?.GetValue<string>()
                .Equals("Content-Length", StringComparison.OrdinalIgnoreCase) == true)
            {
                headers.Remove(header);
            }
        }
    }
}
