using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTSDeckEditAndCreationTool.Model
{
    public class CardStyleInfo
    {
        public string SetAbbreviation { get; set; }

        public string URL { get; set; }

        public CardStyleInfo(string setAbbreviation, string url)
        {
            SetAbbreviation = setAbbreviation;
            URL = url;
        }

        public CardStyleInfo()
        {
            SetAbbreviation = "";
            URL = "";
        }
    }
}
