using System.Collections.Generic;

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

        // --- Generation mode (decklist -> JSON) extras ---
        // Optional precise print requested from the decklist line "Name (SET) NUMBER".
        public string SetCode { get; set; }
        public string CollectorNumber { get; set; }
        // Back-face image for double-faced/modal cards. When set, the generator emits
        // this card as a single object carrying its verso in States["2"] (see TtsDeckBuilder).
        // null for ordinary single-faced cards (their physical back is the deck BackURL).
        public string BackFaceURL { get; set; }

        List<string> PrintURLs { get; set; }

        public DeckCard(string nickname, int cardid, string faceurl, bool isBack = false, char zone = 'L', int count = 1)
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
