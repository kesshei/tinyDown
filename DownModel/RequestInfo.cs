using System.Diagnostics.SymbolStore;

namespace DownModel
{
    public class RequestInfo
    {
        public string LoalPath { get; set; }
        public string ServerPath { get; set; }
        public string Hash256 { get; set; }
        public long FileLength { get; set; }
    }
}
