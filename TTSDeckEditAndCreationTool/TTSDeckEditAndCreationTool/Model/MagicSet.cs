using System.Text.Json.Serialization;

namespace TTSDeckEditAndCreationTool.Model
{
    /// <summary>
    /// A Magic set/series as returned by Scryfall's /sets endpoint.
    /// Only the fields we need to populate the series picker are modelled.
    /// </summary>
    public class MagicSet
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("released_at")]
        public string ReleasedAt { get; set; }

        [JsonPropertyName("set_type")]
        public string SetType { get; set; }

        /// <summary>Text shown in the dropdown, e.g. "Final Fantasy (FIN)".</summary>
        [JsonIgnore]
        public string Display => $"{Name} ({Code?.ToUpper()})";
    }
}
