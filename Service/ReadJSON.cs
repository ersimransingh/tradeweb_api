using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TradeWeb.API.Service
{


    public class ReadJSON
    {
        private readonly JsonSerializer _serializer;

        public ReadJSON()
        {
            _serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented
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
