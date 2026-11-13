using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    /// <summary>
    /// Body of POST api/Main/TradeWebBatch.
    /// Each item carries the exact XML payload that would otherwise be POSTed to api/Main/TradeWeb.
    /// </summary>
    public class TradeWebBatchRequest
    {
        [JsonProperty("requests")]
        public List<TradeWebBatchItem> Requests { get; set; }
    }

    public class TradeWebBatchItem
    {
        /// <summary>Client-chosen id, echoed back so the client can match responses to callers.</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>The &lt;dsXml&gt;...&lt;/dsXml&gt; payload.</summary>
        [JsonProperty("body")]
        public string Body { get; set; }
    }

    public class TradeWebBatchResult
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>HTTP status the item would have returned from api/Main/TradeWeb.</summary>
        [JsonProperty("status")]
        public int Status { get; set; }

        /// <summary>The JSON body the item would have returned from api/Main/TradeWeb.</summary>
        [JsonProperty("body")]
        public JToken Body { get; set; }
    }

    public class TradeWebBatchResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("results")]
        public List<TradeWebBatchResult> Results { get; set; }
    }
}
