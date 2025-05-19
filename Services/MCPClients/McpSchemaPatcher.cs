using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services.MCPClients
{
    public static class McpSchemaPatcher
    {
        public static JsonElement PatchExclusiveBounds(JsonElement schema)
        {
            // Convert JsonElement to JObject
            var jObj = JObject.Parse(schema.GetRawText());
            PatchExclusiveBoundsRecursive(jObj);
            // Convert back to JsonElement
            var patched = JsonDocument.Parse(jObj.ToString());
            return patched.RootElement.Clone();
        }

        private static void PatchExclusiveBoundsRecursive(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                // Patch properties
                var props = obj.Properties().ToList(); // ToList to avoid modifying during enumeration
                foreach (var prop in props)
                {
                    if (prop.Name == "exclusiveMinimum")
                    {
                        int min = prop.Value.Value<int>() + 1;
                        obj.Remove("exclusiveMinimum");
                        obj["minimum"] = min;
                    }
                    else if (prop.Name == "exclusiveMaximum")
                    {
                        int max = prop.Value.Value<int>() - 1;
                        obj.Remove("exclusiveMaximum");
                        obj["maximum"] = max;
                    }
                    else
                    {
                        PatchExclusiveBoundsRecursive(prop.Value);
                    }
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var item in (JArray)token)
                {
                    PatchExclusiveBoundsRecursive(item);
                }
            }
        }
    }
}
