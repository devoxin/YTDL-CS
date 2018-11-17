using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Ytdl2
{
    public class Ytdl
    {

        private const string VIDEO_EURL = "https://youtube.googleapis.com/v/";
        private const string INFO_HOST = "www.youtube.com";
        private const string INFO_PATH = "/get_video_info";

        private SignatureCipherManager signatureCipherManager = new SignatureCipherManager();

        public VideoInfo GetVideoInfo(string videoId)
        {
            // Maybe consider extracting video_id from this parameter so links can be passed?

            NameValueCollection info = GetVideoInfoRaw(videoId);

            if (info == null)
            {
                return null;
                // TODO: Throw errors on specific operations.
            }

            string title = info["title"];
            string video_id = info["video_id"];
            string author = info["author"];
            int length = int.Parse(info["length_seconds"]);

            Format[] fmts = ParseFormats(info);
            string[] tokens = signatureCipherManager.GetTokens(info);

            Format[] decipheredFormats = DecipherFormats(fmts, tokens);

            return new VideoInfo(title, video_id, author, length, decipheredFormats);
        }

        public NameValueCollection GetVideoInfoRaw(string videoId)
        {
            JObject config = GetPageConfig(videoId);

            if (config == null)
            {
                return null;
            }

            var sts = (string)config.GetValue("sts");

            WebClient httpClient2 = new WebClient();
            httpClient2.QueryString.Add("video_id", videoId);
            httpClient2.QueryString.Add("eurl", VIDEO_EURL + videoId);
            httpClient2.QueryString.Add("ps", "default");
            httpClient2.QueryString.Add("gl", "US");
            httpClient2.QueryString.Add("hl", "en");
            httpClient2.QueryString.Add("sts", sts);

            string videoInfo = httpClient2.DownloadString("https://" + INFO_HOST + "/" + INFO_PATH);
            NameValueCollection info = HttpUtility.ParseQueryString(videoInfo);

            string html5player = (string)((JObject)config.GetValue("assets")).GetValue("js");
            info.Add("html5player", html5player);
            return info;
        }

        public JObject GetPageConfig(string videoId)
        {
            WebClient httpClient = new WebClient();
            string result = httpClient.DownloadString("https://youtube.com/watch?v=" + videoId);
            string configJson = DataFormatTools.ExtractBetween(result, "ytplayer.config = ", ";ytplayer.load");

            if (configJson == null)
            {
                return null;
            }

            return JObject.Parse(configJson);
        }

        private Format[] ParseFormats(NameValueCollection videoInfo)
        {
            List<string> formats = new List<string>();
            
            if (videoInfo["url_encoded_fmt_stream_map"] != null)
            {
                formats.AddRange(videoInfo["url_encoded_fmt_stream_map"].Split(','));
            }

            if (videoInfo["adaptive_fmts"] != null)
            {
                formats.AddRange(videoInfo["adaptive_fmts"].Split(','));
            }

            return formats.Select(x =>
                {
                    NameValueCollection properties = HttpUtility.ParseQueryString(x);
                    string[] types = properties["type"].Split(';');
                    string type = types[0].Trim();
                    string codecs = HttpUtility.ParseQueryString(types[1].Trim())["codecs"].Replace("\"", "");
                    return new Format(properties["url"],
                        properties["itag"],
                        properties["quality"],
                        type,
                        codecs,
                        properties["s"]);
                }
            ).ToArray();
        }

        private Format[] DecipherFormats(Format[] formats, string[] tokens)
        {
            List<Format> deciphered = new List<Format>();

            foreach (Format fmt in formats)
            {
                if (fmt.Url == null)
                {
                    continue;
                }

                string signature = fmt.Sig != null ? signatureCipherManager.Decipher(tokens, fmt.Sig) : null;
                Uri decodedUrl = new Uri(HttpUtility.UrlDecode(fmt.Url));
                string baseUrl = decodedUrl.GetLeftPart(UriPartial.Path);
                NameValueCollection urlParams = HttpUtility.ParseQueryString(decodedUrl.Query);
                urlParams.Remove("search");
                urlParams.Set("ratebypass", "yes");

                if (signature != null)
                {
                    urlParams.Set("signature", signature);
                    fmt.Sig = signature;
                }

                string finalUrl = baseUrl + "?" + urlParams;
                fmt.Url = finalUrl;

                deciphered.Add(fmt);
            }

            return deciphered.ToArray();
            
        }
    }
}
