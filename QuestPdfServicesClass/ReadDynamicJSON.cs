using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace TradeWeb.API.QuestServices
{
    public class ReadDynamicJSON
    {

        private readonly Newtonsoft.Json.JsonSerializer _serializer;

        public ReadDynamicJSON()
        {
            _serializer = new Newtonsoft.Json.JsonSerializer
            {
                Formatting = (Newtonsoft.Json.Formatting)System.Xml.Formatting.Indented
            };
        }

        // Read from string
        public JToken Parse(string json)
        {
            return JToken.Parse(json);
        }

        // Read from file
        public async Task<JToken> ReadFromFileAsync(string filePath)
        {
            var json = await File.ReadAllTextAsync(filePath);
            return Parse(json);
        }

        // Read from stream
        public async Task<JToken> ReadFromStreamAsync(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader))
            {
                return await JToken.ReadFromAsync(jsonReader);
            }
        }

        // Convert dynamic JSON to typed object
        public T ToObject<T>(JToken token)
        {
            return token.ToObject<T>();
        }

        // Get value safely using dot path
        public T GetValue<T>(JToken token, string path)
        {
            var current = token;

            foreach (var part in path.Split('.'))
            {
                current = current?[part];
                if (current == null)
                    return default;
            }

            return current.ToObject<T>();
        }
    }
}
