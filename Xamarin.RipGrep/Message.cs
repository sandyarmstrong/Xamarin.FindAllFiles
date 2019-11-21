
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Xamarin.RipGrep
{
    public class Message
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("data")]
        public Match Data { get; set; }
    }

    public class Match
    {
        [JsonPropertyName("path")]
        public TextWrapper Path { get; set; }

        [JsonPropertyName("lines")]
        public TextWrapper Lines { get; set; }

        [JsonPropertyName("line_number")]
        public int LineNumber { get; set; }

        [JsonPropertyName("absolute_offset")]
        public int AbsoluteOffset { get; set; }

        [JsonPropertyName("submatches")]
        public IList<Submatch> Submatches { get; set; }
    }

    public class Submatch
    {
        [JsonPropertyName("match")]
        public TextWrapper Match { get; set; }

        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonPropertyName("end")]
        public int End { get; set; }
    }

    public class TextWrapper
    {
        [JsonPropertyName("bytes")]
        public string Bytes { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        public string GetText()
        {
            if (!string.IsNullOrEmpty(Bytes))
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(Bytes));
            }
            else
            {
                return Text;
            }
        }
    }
}
