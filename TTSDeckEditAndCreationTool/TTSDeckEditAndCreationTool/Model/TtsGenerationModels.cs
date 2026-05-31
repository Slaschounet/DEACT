using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TTSDeckEditAndCreationTool.Model
{
    /// <summary>
    /// Write-only DTOs used to GENERATE a Tabletop Simulator "Saved Object" JSON
    /// from scratch (decklist -> JSON). These are intentionally separate from the
    /// import-side <see cref="TtsSaveFile"/>/<see cref="TtsObject"/> models: those
    /// are round-trip-safe (JsonExtensionData) for editing files we did not write,
    /// whereas here we emit a full, self-contained object with every field TTS
    /// expects and sensible defaults. Field names/shape mirror the reference
    /// implementation (tts-deckconverter, tts/structs.go).
    ///
    /// Serialize with DefaultIgnoreCondition = WhenWritingNull so the nullable
    /// members below behave like the reference's `omitempty` (e.g. a card has no
    /// DeckIDs/ContainedObjects, a deck has no CardID).
    /// </summary>
    public class GenSavedObject
    {
        [JsonPropertyName("SaveName")] public string SaveName { get; set; } = "";
        [JsonPropertyName("GameMode")] public string GameMode { get; set; } = "";
        [JsonPropertyName("Date")] public string Date { get; set; } = "";
        [JsonPropertyName("VersionNumber")] public string VersionNumber { get; set; } = "";
        [JsonPropertyName("GameType")] public string GameType { get; set; } = "";
        [JsonPropertyName("GameComplexity")] public string GameComplexity { get; set; } = "";
        [JsonPropertyName("Table")] public string Table { get; set; } = "";
        [JsonPropertyName("Sky")] public string Sky { get; set; } = "";
        [JsonPropertyName("Note")] public string Note { get; set; } = "";
        [JsonPropertyName("Rules")] public string Rules { get; set; } = "";
        [JsonPropertyName("ObjectStates")] public List<GenObject> ObjectStates { get; set; } = new List<GenObject>();
    }

    public class GenObject
    {
        /// <summary>"DeckCustom" for a pile, "CardCustom" for a single card.</summary>
        [JsonPropertyName("Name")] public string Name { get; set; }
        [JsonPropertyName("Transform")] public GenTransform Transform { get; set; }
        [JsonPropertyName("Nickname")] public string Nickname { get; set; } = "";
        [JsonPropertyName("Description")] public string Description { get; set; } = "";
        [JsonPropertyName("GMNotes")] public string GMNotes { get; set; } = "";
        [JsonPropertyName("ColorDiffuse")] public GenColorDiffuse ColorDiffuse { get; set; }
        [JsonPropertyName("Locked")] public bool Locked { get; set; }
        [JsonPropertyName("Grid")] public bool Grid { get; set; } = true;
        [JsonPropertyName("Snap")] public bool Snap { get; set; } = true;
        [JsonPropertyName("IgnoreFoW")] public bool IgnoreFoW { get; set; }
        [JsonPropertyName("MeasureMovement")] public bool MeasureMovement { get; set; }
        [JsonPropertyName("DragSelectable")] public bool DragSelectable { get; set; } = true;
        [JsonPropertyName("Autoraise")] public bool Autoraise { get; set; } = true;
        [JsonPropertyName("Sticky")] public bool Sticky { get; set; } = true;
        [JsonPropertyName("Tooltip")] public bool Tooltip { get; set; } = true;
        [JsonPropertyName("GridProjection")] public bool GridProjection { get; set; }
        [JsonPropertyName("HideWhenFaceDown")] public bool HideWhenFaceDown { get; set; } = true;
        [JsonPropertyName("Hands")] public bool Hands { get; set; } = true;

        /// <summary>Present on cards; absent (null) on the deck pile.</summary>
        [JsonPropertyName("CardID")] public int? CardID { get; set; }
        [JsonPropertyName("SidewaysCard")] public bool SidewaysCard { get; set; }

        /// <summary>Present on the deck pile (one entry per card copy); null on a card.</summary>
        [JsonPropertyName("DeckIDs")] public List<int> DeckIDs { get; set; }
        /// <summary>Image data keyed by deck id (one entry per unique image).</summary>
        [JsonPropertyName("CustomDeck")] public Dictionary<string, GenCustomDeck> CustomDeck { get; set; }

        [JsonPropertyName("XmlUI")] public string XmlUI { get; set; } = "";
        [JsonPropertyName("LuaScript")] public string LuaScript { get; set; } = "";
        [JsonPropertyName("LuaScriptState")] public string LuaScriptState { get; set; } = "";

        /// <summary>Present on the deck pile; null on a card.</summary>
        [JsonPropertyName("ContainedObjects")] public List<GenObject> ContainedObjects { get; set; }
        /// <summary>Verso of a double-faced card, keyed "2"; null otherwise.</summary>
        [JsonPropertyName("States")] public Dictionary<string, GenObject> States { get; set; }

        [JsonPropertyName("GUID")] public string GUID { get; set; } = "";
    }

    public class GenTransform
    {
        [JsonPropertyName("posX")] public double PosX { get; set; }
        [JsonPropertyName("posY")] public double PosY { get; set; }
        [JsonPropertyName("posZ")] public double PosZ { get; set; }
        [JsonPropertyName("rotX")] public double RotX { get; set; }
        [JsonPropertyName("rotY")] public double RotY { get; set; } = 180;
        [JsonPropertyName("rotZ")] public double RotZ { get; set; }
        [JsonPropertyName("scaleX")] public double ScaleX { get; set; } = 1;
        [JsonPropertyName("scaleY")] public double ScaleY { get; set; } = 1;
        [JsonPropertyName("scaleZ")] public double ScaleZ { get; set; } = 1;
    }

    public class GenColorDiffuse
    {
        [JsonPropertyName("r")] public double Red { get; set; } = 0.713235259;
        [JsonPropertyName("g")] public double Green { get; set; } = 0.713235259;
        [JsonPropertyName("b")] public double Blue { get; set; } = 0.713235259;
    }

    public class GenCustomDeck
    {
        [JsonPropertyName("FaceURL")] public string FaceURL { get; set; }
        [JsonPropertyName("BackURL")] public string BackURL { get; set; }
        [JsonPropertyName("NumWidth")] public int NumWidth { get; set; } = 1;
        [JsonPropertyName("NumHeight")] public int NumHeight { get; set; } = 1;
        [JsonPropertyName("BackIsHidden")] public bool BackIsHidden { get; set; } = true;
        [JsonPropertyName("UniqueBack")] public bool UniqueBack { get; set; }
        /// <summary>0 = rounded rectangle, 1 = rectangle (DeckShape).</summary>
        [JsonPropertyName("Type")] public int Type { get; set; }
    }
}
