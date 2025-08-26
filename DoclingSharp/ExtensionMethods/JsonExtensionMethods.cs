using System.Text.Json;

namespace DoclingSharp.ExtensionMethods
{
    /// <summary>
    /// Extension method for JSON elements.
    /// </summary>
    internal static class JsonExtensionMethods
    {
        public static JsonElement? GetPropertyOrNull(this JsonElement el, string name)
            => el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) ? v : null;

        /// <summary>
        /// Find the first string value.
        /// </summary>
        /// <param name="element">The Json element to check.</param>
        /// <param name="name">The name to look for.</param>
        /// <returns></returns>
        public static string? FindFirstString(this JsonElement element, string name)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in element.EnumerateObject())
                {
                    if (p.NameEquals(name) && p.Value.ValueKind == JsonValueKind.String)
                        return p.Value.GetString();
                    var inner = p.Value.FindFirstString(name);
                    if (inner != null) return inner;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var inner = item.FindFirstString(name);
                    if (inner != null) return inner;
                }
            }
            return null;
        }

        /// <summary>
        /// Find the first int value.
        /// </summary>
        /// <param name="element">The Jsom element to check</param>
        /// <param name="name">The name to look for.</param>
        /// <returns></returns>
        public static int? FindFirstInt(this JsonElement element, string name)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in element.EnumerateObject())
                {
                    if (p.NameEquals(name) && p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetInt32(out var i))
                        return i;
                    var inner = p.Value.FindFirstInt(name);
                    if (inner != null) return inner;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var inner = item.FindFirstInt(name);
                    if (inner != null) return inner;
                }
            }
            return null;
        }
    }
}
