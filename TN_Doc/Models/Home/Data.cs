using TN.DocData;

namespace TN_Doc.Models.Home
{
    public class Data
    {
        public int IdDevice { get; set; }
        public IdDoc IdDoc { get; set; }
        public string DTBegin { get; set; }
        public string DTEnd { get; set; }
    }
}