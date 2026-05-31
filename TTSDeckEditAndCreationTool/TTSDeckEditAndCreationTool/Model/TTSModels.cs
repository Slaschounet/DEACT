using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TTSDeckEditAndCreationTool.Model
{
    /// <summary>
    /// Typed, round-trip-safe representation of a Tabletop Simulator save file
    /// (the JSON the app imports/exports).
    ///
    /// Only the fields the app actually reads are modelled. Every other field
    /// TTS writes (Transform, ColorDiffuse, LuaScript, GUID, Tags, ...) is
    /// captured verbatim through <see cref="JsonExtensionData"/> so it survives
    /// a deserialize/serialize round trip without loss. This lets us navigate
    /// the save with strong typing instead of brute-force JsonElement probing,
    /// while keeping unknown data intact.
    ///
    /// Note: TTS uses PascalCase property names, but this app's own marker
    /// (<c>isUpdated</c>) is camelCase, so every property declares its JSON
    /// name explicitly rather than relying on a global naming policy.
    /// </summary>
    public class TtsSaveFile
    {
        [JsonPropertyName("ObjectStates")]
        public List<TtsObject> ObjectStates { get; set; }

        /// <summary>App-specific marker: set once a deck has been processed/saved.</summary>
        [JsonPropertyName("isUpdated")]
        public bool? IsUpdated { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtraData { get; set; }
    }

    /// <summary>
    /// A TTS object inside ObjectStates (or nested in a deck pile). The same
    /// shape covers a single card and a deck pile of cards — a pile is simply
    /// an object that carries <see cref="ContainedObjects"/>.
    /// </summary>
    public class TtsObject
    {
        [JsonPropertyName("Nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("CardID")]
        public int? CardID { get; set; }

        /// <summary>Custom image data keyed by deck id; usually a single entry.</summary>
        [JsonPropertyName("CustomDeck")]
        public Dictionary<string, TtsCustomDeckEntry> CustomDeck { get; set; }

        /// <summary>Present on deck piles; the cards contained in the pile.</summary>
        [JsonPropertyName("ContainedObjects")]
        public List<TtsObject> ContainedObjects { get; set; }

        /// <summary>
        /// Alternate faces keyed by state id; present on cards with content on
        /// the back face (modal / flip cards).
        /// </summary>
        [JsonPropertyName("States")]
        public Dictionary<string, TtsObject> States { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtraData { get; set; }

        /// <summary>True when this object is a pile of cards rather than a single card.</summary>
        [JsonIgnore]
        public bool IsDeck => ContainedObjects != null;
    }

    /// <summary>A single CustomDeck entry: the front/back image URLs for a card.</summary>
    public class TtsCustomDeckEntry
    {
        [JsonPropertyName("FaceURL")]
        public string FaceURL { get; set; }

        [JsonPropertyName("BackURL")]
        public string BackURL { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtraData { get; set; }
    }
}
