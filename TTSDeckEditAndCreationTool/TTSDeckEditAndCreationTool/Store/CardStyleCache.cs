using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using TTSDeckEditAndCreationTool.Model;
using System.IO;
using System.Net;

namespace TTSDeckEditAndCreationTool.Store
{
    public static class CardStyleCache
    {
        public static Dictionary<string, CardStyles> CardList = new Dictionary<string, CardStyles>();

        static string systemPath = System.Environment.
                             GetFolderPath(
                                 Environment.SpecialFolder.CommonApplicationData
                             );
        static string FolderPath = Path.Combine(systemPath, "DEACT");
        static string SavePath = Path.Combine(systemPath, "DEACT\\DEACT_CardStyleCache.txt");

        // --- Face URL cache: (cardname, lang, face) -> resolved Scryfall image url ---
        // Scryfall asks clients to cache results for >=24h. This lets re-imports and
        // Re-DL of the same cards skip the network (and the 2 req/s search rate limit)
        // entirely. Only successful lookups are cached.
        static string FaceCachePath = Path.Combine(systemPath, "DEACT\\DEACT_FaceCache.json");
        static readonly TimeSpan FaceCacheTtl = TimeSpan.FromHours(24);
        static Dictionary<string, CachedFace> FaceCache = new Dictionary<string, CachedFace>();

        public static void Initialize()
        {
            //Create folder
            if(!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            //Create File
            if (!File.Exists(SavePath))
            {
                FileStream newFile = File.Create(SavePath);
                newFile.Close();
                SaveList();
            }
            else
            {
                try
                {
                    string storedList = File.ReadAllText(SavePath);

                    CardList = JsonSerializer.Deserialize<Dictionary<string, CardStyles>>(storedList);
                }
                catch(Exception ex)
                {
                    File.Delete(SavePath);
                }
            }

            //Load the face cache (best effort; a corrupt file is just discarded)
            try
            {
                if (File.Exists(FaceCachePath))
                {
                    string stored = File.ReadAllText(FaceCachePath);
                    FaceCache = JsonSerializer.Deserialize<Dictionary<string, CachedFace>>(stored)
                                ?? new Dictionary<string, CachedFace>();
                }
            }
            catch
            {
                FaceCache = new Dictionary<string, CachedFace>();
            }
        }

        // setCode is part of the key: art from set "fin" must not collide with the
        // default (any-set) lookup for the same card/language/face.
        private static string FaceKey(string cardName, string lang, bool isBack, string setCode)
            => $"{lang}|{(isBack ? "B" : "F")}|{setCode ?? ""}|{cardName}";

        /// <summary>Returns a non-expired cached image url for this card/lang/face/set, or null.</summary>
        public static string GetCachedFace(string cardName, string lang, bool isBack, string setCode = null)
        {
            if (FaceCache.TryGetValue(FaceKey(cardName, lang, isBack, setCode), out CachedFace entry))
            {
                if (DateTime.UtcNow - entry.FetchedUtc < FaceCacheTtl)
                {
                    return entry.Url;
                }
            }
            return null;
        }

        /// <summary>Caches a successful image url lookup (in memory; call SaveFaceCache to persist).</summary>
        public static void StoreCachedFace(string cardName, string lang, bool isBack, string url, string setCode = null)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            FaceCache[FaceKey(cardName, lang, isBack, setCode)] = new CachedFace
            {
                Url = url,
                FetchedUtc = DateTime.UtcNow
            };
        }

        /// <summary>Persists the face cache to disk. Call once after a bulk fetch.</summary>
        public static void SaveFaceCache()
        {
            try
            {
                File.WriteAllText(FaceCachePath, JsonSerializer.Serialize(FaceCache));
            }
            catch
            {
                // non-fatal: cache is an optimization, not source of truth
            }
        }

        public static void SaveList()
        {
            string convertedList = JsonSerializer.Serialize(CardList);

            File.WriteAllText(SavePath, convertedList);
        }

        public static void DeleteCacheFile()
        {
            File.Delete(SavePath);
        }

        /// <summary>One cached face-url lookup with the time it was fetched (for TTL).</summary>
        public class CachedFace
        {
            public string Url { get; set; }
            public DateTime FetchedUtc { get; set; }
        }

        public static string DownloadImageIfNeeded(string url)
        {
            string hash = Convert.ToBase64String(System.Security.Cryptography.MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(url)))
                            .Replace("/", "").Replace("+", "");
            string localPath = Path.Combine(FolderPath, $"{hash}.jpg");

            if (!File.Exists(localPath))
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(url, localPath);
                }
            }

            return localPath;
        }
    }
}
