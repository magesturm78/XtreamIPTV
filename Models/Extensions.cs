using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace XtreamIPTV
{
    public static class Extensions
    {
        public static string GetJsonString(this JsonElement m, string propertyName)
        {
            m.TryGetProperty(propertyName, out var prop);
            if (prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString() ?? "";
            } 
            return  prop.ToString() ?? "";
        }
    }
}
