namespace Ytdl2
{
    public class Format
    {
        public string Url { get; internal set; }
        public short Itag { get; internal set; }
        public string Quality { get; internal set; }
        public string Type { get; internal set; }
        public string Codecs { get; internal set; }
        public string Sig { get; internal set; }

        internal Format(string url, string itag, string quality, string type, string codecs, string sig)
        {
            Url = url;
            Itag = short.Parse(itag);
            Quality = quality;
            Type = type;
            Codecs = codecs;
            Sig = sig;
        }
    }
}
