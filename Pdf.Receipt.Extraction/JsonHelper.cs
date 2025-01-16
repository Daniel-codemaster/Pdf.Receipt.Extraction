using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Pdf.Receipt.Extraction
{
    public static class JsonHelper
    {
        public static JsonSerializerSettings Options => new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };
    }
}
