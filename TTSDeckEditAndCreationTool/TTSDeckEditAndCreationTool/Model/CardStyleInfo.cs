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

        /// <summary>
        /// Release date of this particular printing. This is used to sort prints
        /// by their release dates so that the newest card is shown first in the
        /// style selection window.
        /// </summary>
        public DateTime ReleaseDate { get; set; }

        public CardStyleInfo(string setAbbreviation, string url, DateTime releaseDate)
        {
            SetAbbreviation = setAbbreviation;
            URL = url;
            ReleaseDate = releaseDate;
        }

        public CardStyleInfo(string setAbbreviation, string url)
            : this(setAbbreviation, url, DateTime.MinValue)
        { }

        public CardStyleInfo()
        {
            SetAbbreviation = "";
            URL = "";
            ReleaseDate = DateTime.MinValue;
        }
    }
}
