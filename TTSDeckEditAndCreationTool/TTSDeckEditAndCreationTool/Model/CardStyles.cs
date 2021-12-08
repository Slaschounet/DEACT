using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTSDeckEditAndCreationTool.Model
{
    public class CardStyles
    {
        public string Name { get; set; }

        public List<CardStyleInfo> Prints { get; set; }

        //store in UTC
        public DateTime LastFetched { get; set; }

        public CardStyles(string name)
        {
            Name = name;
            LastFetched = DateTime.MinValue;
            Prints = new List<CardStyleInfo>();
        }

        public CardStyles()
        {
            Name = "";
            LastFetched = DateTime.MinValue;
            Prints = new List<CardStyleInfo>();
        }
    }
}
