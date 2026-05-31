using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TTSDeckEditAndCreationTool.Model
{
    /// <summary>A single line of a decklist, resolved to a quantity, a card name
    /// and (optionally) the precise print requested via "(SET) NUMBER".</summary>
    public class ParsedDeckLine
    {
        public int Count { get; set; }
        public string Name { get; set; }
        public string SetCode { get; set; }          // null when no set was given
        public string CollectorNumber { get; set; }  // null when no collector number was given
    }

    /// <summary>
    /// Parses a pasted Magic decklist (Moxfield / Archidekt / MTGA / EDHRec text export)
    /// into <see cref="ParsedDeckLine"/>s.
    ///
    /// Accepted shapes per line (quantity optional, set/number optional):
    ///   "2 Lightning Bolt (2X2) 117"
    ///   "1x Sol Ring (C21) 263"
    ///   "1 Sol Ring"
    ///   "Sol Ring"                (assumed quantity 1)
    /// Foil/flag markers (e.g. "*F*") and bracket tags (e.g. "[Ramp]") are stripped.
    /// Blank lines, comments (// or #) and section headers ("Commander", "Deck:",
    /// "Sideboard (15)") are ignored. The set/number suffix is only recognised at the
    /// very end of the line, so card names that themselves contain parentheses survive.
    /// </summary>
    public static class DecklistParser
    {
        // Leading "12 " / "12x " / "12X " quantity.
        private static readonly Regex QuantityRegex =
            new Regex(@"^\s*(\d+)\s*[xX]?\s+(.+)$", RegexOptions.Compiled);

        // Trailing "(SET) 123" / "(SET) 123a" / "(SET)" suffix. Set code is 2-6 alphanumerics.
        private static readonly Regex SetSuffixRegex =
            new Regex(@"^(.*?)\s*\(([A-Za-z0-9]{2,6})\)(?:\s+([A-Za-z0-9\-★]+))?\s*$", RegexOptions.Compiled);

        private static readonly HashSet<string> KnownHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "deck", "commander", "commanders", "sideboard", "maybeboard",
            "companion", "tokens", "planes", "schemes", "about"
        };

        public static List<ParsedDeckLine> Parse(string text)
        {
            var result = new List<ParsedDeckLine>();
            if (string.IsNullOrWhiteSpace(text)) return result;

            foreach (string rawLine in text.Replace("\r\n", "\n").Split('\n'))
            {
                ParsedDeckLine line = ParseLine(rawLine);
                if (line != null) result.Add(line);
            }
            return result;
        }

        private static ParsedDeckLine ParseLine(string rawLine)
        {
            string line = rawLine?.Trim();
            if (string.IsNullOrEmpty(line)) return null;
            if (line.StartsWith("//") || line.StartsWith("#")) return null;

            // Strip trailing foil/flag markers ("*F*") and bracket tags ("[Ramp]").
            line = Regex.Replace(line, @"\s*\*[^*]*\*\s*$", "").Trim();
            line = Regex.Replace(line, @"\s*\[[^\]]*\]\s*$", "").Trim();
            if (line.Length == 0) return null;

            int count = 1;
            string rest = line;

            Match qty = QuantityRegex.Match(line);
            if (qty.Success)
            {
                count = int.Parse(qty.Groups[1].Value);
                rest = qty.Groups[2].Value.Trim();
            }
            else
            {
                // No leading quantity: could be "Sol Ring" (a card) or a section header.
                // Strip a trailing "(n)" count and ":" then match against known headers,
                // so "Sideboard (15)", "Commander", "Deck:" are all recognised and skipped
                // — while a real card like "Sol Ring" (not a known header) falls through.
                string headerProbe = Regex.Replace(line, @"\s*\(\d+\)\s*$", "").TrimEnd(':').Trim();
                if (KnownHeaders.Contains(headerProbe)) return null;
            }

            if (count <= 0) return null;

            string name = rest;
            string setCode = null;
            string collector = null;

            Match suffix = SetSuffixRegex.Match(rest);
            if (suffix.Success && suffix.Groups[1].Value.Trim().Length > 0)
            {
                name = suffix.Groups[1].Value.Trim();
                setCode = suffix.Groups[2].Value.Trim();
                collector = suffix.Groups[3].Success ? suffix.Groups[3].Value.Trim() : null;
                if (collector != null && collector.Length == 0) collector = null;
            }

            if (string.IsNullOrWhiteSpace(name)) return null;

            return new ParsedDeckLine
            {
                Count = count,
                Name = name,
                SetCode = setCode,
                CollectorNumber = collector
            };
        }
    }
}
