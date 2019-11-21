
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Xamarin.RipGrep
{
    public class RipGrepMessage
    {
        [JsonPropertyName("type")]
        public string type { get; set; }

        [JsonPropertyName("data")]
        public RipGrepMatch data { get; set; }
    }

    public class RipGrepMatch
    {
        [JsonPropertyName("path")]
        public RipGrepBytesOrText path { get; set; }

        [JsonPropertyName("lines")]
        public RipGrepBytesOrText lines { get; set; }

        [JsonPropertyName("line_number")]
        public int line_number { get; set; }

        [JsonPropertyName("absolute_offset")]
        public int absolute_offset { get; set; }

        [JsonPropertyName("submatches")]
        public IList<RipGrepSubMatch> submatches { get; set; }
    }

    public class RipGrepSubMatch
    {
        [JsonPropertyName("match")]
        public RipGrepBytesOrText match { get; set; }

        [JsonPropertyName("start")]
        public int start { get; set; }

        [JsonPropertyName("end")]
        public int end { get; set; }
    }

    public class RipGrepBytesOrText
    {
        [JsonPropertyName("bytes")]
        public string bytes { get; set; }

        [JsonPropertyName("text")]
        public string text { get; set; }

        public string GetText()
        {
            if (!string.IsNullOrEmpty(bytes))
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(bytes));
            }
            else
            {
                return text;
            }
        }
    }
}
