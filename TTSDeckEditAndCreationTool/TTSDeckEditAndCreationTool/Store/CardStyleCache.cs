using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using TTSDeckEditAndCreationTool.Model;
using System.IO;

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
    }
}
