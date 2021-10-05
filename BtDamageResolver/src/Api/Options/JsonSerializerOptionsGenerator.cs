using System.Text.Json;
using System.Text.Json.Serialization;

namespace Faemiyah.BtDamageResolver.Api.Options
{
    public static class JsonSerializerOptionsGenerator
    {
        public static JsonSerializerOptions Generate()
        {
            var jsonSerializerOptions = new JsonSerializerOptions();
            jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            
            return jsonSerializerOptions;
        }
    }
}