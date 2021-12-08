using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TTSDeckEditAndCreationTool.Model
{
    public class DeckCard
    {
        public string Nickname { get; set; }
        public string Cardname { get; set; }
        public int CardID { get; set; }
        public int Count { get; set; }
        public char Zone { get; set; }
        public bool BackFace { get; set; } //used for modal cards to denote this card is the BACK of the card

        public string FaceURL { get; set; }
        public string OldFaceURL { get; set; }

        List<string> PrintURLs { get; set; }

        public DeckCard(string nickname, int cardid, string faceurl, char zone = 'L', int count = 1, bool isBack = false)
        {
            Nickname = nickname;
            CardID = cardid;
            OldFaceURL = FaceURL = faceurl;
            Zone = zone;
            Count = count;
            BackFace = isBack;
        }

        public void SetPrints(List<string> printurls)
        {
            PrintURLs = printurls;
        }
    }
}
