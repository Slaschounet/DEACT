using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TTSDeckEditAndCreationTool.Model;

namespace TTSDeckEditAndCreationTool.Store
{
    /// <summary>
    /// Loads and caches the list of Magic sets from Scryfall's /sets endpoint.
    /// /sets is a "light" endpoint and a single call returns every set, so we fetch
    /// once and cache to disk for 24h (per Scryfall's caching guidance). Used to
    /// populate the series picker on the import and deck-builder screens.
    /// </summary>
    public static class SetCatalog
    {
        private static readonly HttpClient _http = CreateClient();
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

        private static readonly string CachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DEACT", "DEACT_Sets.json");

        private static List<MagicSet> _sets;
        private static readonly System.Threading.SemaphoreSlim _loadGate = new System.Threading.SemaphoreSlim(1, 1);

        public static IReadOnlyList<MagicSet> Sets => _sets ?? new List<MagicSet>();

        private static HttpClient CreateClient()
        {
            var c = new HttpClient();
            c.DefaultRequestHeaders.Add("User-Agent", "DEACT/1.0 (contact@example.com)");
            c.DefaultRequestHeaders.Add("Accept", "application/json");
            return c;
        }

        /// <summary>
        /// Ensures the set list is available: returns the in-memory copy, else a fresh
        /// disk cache (&lt;24h), else fetches from Scryfall. Safe to call repeatedly.
        /// </summary>
        public static async Task EnsureLoadedAsync()
        {
            if (_sets != null) return;

            await _loadGate.WaitAsync();
            try
            {
                if (_sets != null) return;

                if (TryLoadFromDisk(out List<MagicSet> cached))
                {
                    _sets = cached;
                    return;
                }

                _sets = await FetchFromScryfallAsync() ?? new List<MagicSet>();
            }
            finally
            {
                _loadGate.Release();
            }
        }

        private static bool TryLoadFromDisk(out List<MagicSet> sets)
        {
            sets = null;
            try
            {
                if (!File.Exists(CachePath)) return false;
                if (DateTime.UtcNow - File.GetLastWriteTimeUtc(CachePath) > CacheTtl) return false;

                string json = File.ReadAllText(CachePath);
                sets = JsonSerializer.Deserialize<List<MagicSet>>(json);
                return sets != null && sets.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<List<MagicSet>> FetchFromScryfallAsync()
        {
            try
            {
                using HttpResponseMessage res = await _http.GetAsync("https://api.scryfall.com/sets");
                if (!res.IsSuccessStatusCode) return null;

                string data = await res.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(data);
                if (!doc.RootElement.TryGetProperty("data", out JsonElement arr) || arr.ValueKind != JsonValueKind.Array)
                    return null;

                var list = new List<MagicSet>();
                foreach (JsonElement e in arr.EnumerateArray())
                {
                    list.Add(new MagicSet
                    {
                        Code = e.TryGetProperty("code", out var c) ? c.GetString() : null,
                        Name = e.TryGetProperty("name", out var n) ? n.GetString() : null,
                        ReleasedAt = e.TryGetProperty("released_at", out var r) ? r.GetString() : null,
                        SetType = e.TryGetProperty("set_type", out var t) ? t.GetString() : null
                    });
                }

                // newest first so the most relevant series sit at the top of the picker
                list = list.OrderByDescending(s => s.ReleasedAt ?? "").ToList();

                TrySaveToDisk(list);
                return list;
            }
            catch
            {
                return null;
            }
        }

        private static void TrySaveToDisk(List<MagicSet> sets)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CachePath));
                File.WriteAllText(CachePath, JsonSerializer.Serialize(sets));
            }
            catch
            {
                // non-fatal: catalog will simply be refetched next time
            }
        }
    }
}
