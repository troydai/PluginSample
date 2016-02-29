using System;
using Newtonsoft.Json;
using Plugin.Abstractions;

namespace OutOfProcessPluginContainer
{
    internal class PluginDataConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // This is late bound so that we don't need to load the type just yet
            return objectType.Name == "IPluginData";
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<PluginData>(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}