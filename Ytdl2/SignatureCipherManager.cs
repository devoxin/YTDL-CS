using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Ytdl2
{
    internal class SignatureCipherManager
    {

        private const string BASE_URL = "https://www.youtube.com";
        private Regex rs = new Regex("(?:html5)?player[-_]([a-zA-Z0-9\\-_]+)(?:\\.js|\\/)");
        private Dictionary<string, string[]> cache = new Dictionary<string, string[]>();

        internal string[] GetTokens(NameValueCollection info)
        {
            string html5player = info["html5player"];
            string key = null;

            Match m = rs.Match(html5player);

            if (m.Success)
            {
                key = m.Groups[1].Value;

                if (cache.ContainsKey(key))
                {
                    return cache[key];
                }
            }

            string absoluteUrl = BASE_URL + html5player;

            WebClient httpClient = new WebClient();
            string body = httpClient.DownloadString(absoluteUrl);

            string[] tokens = ExtractActions(body);

            if (key != null)
            {
                cache[key] = tokens;
            }

            return tokens;
        }

        internal string[] ExtractActions(string content)
        {
            Match objResult = SignatureRegexes.actionsObjRegexp.Match(content);
            Match funcResult = SignatureRegexes.actionsFuncRegexp.Match(content);

            if (!objResult.Success || !funcResult.Success)
            {
                return null;
            }

            string obj = objResult.Groups[1].Value.Replace("\\$", "\\\\$");
            string objBody = objResult.Groups[2].Value.Replace("\\$", "\\\\$");
            string funcBody = funcResult.Groups[1].Value.Replace("\\$", "\\\\$");

            Match reverseResult = SignatureRegexes.reverseRegexp.Match(objBody);
            Match sliceResult = SignatureRegexes.sliceRegexp.Match(objBody);
            Match spliceResult = SignatureRegexes.spliceRegexp.Match(objBody);
            Match swapResult = SignatureRegexes.swapRegexp.Match(objBody);

            string reverseKey = DataFormatTools.CleanQuotations(reverseResult.Groups[1].Value.Replace("\\$", "\\\\$"));
            string sliceKey = DataFormatTools.CleanQuotations(sliceResult.Groups[1].Value.Replace("\\$", "\\\\$"));
            string spliceKey = DataFormatTools.CleanQuotations(spliceResult.Groups[1].Value.Replace("\\$", "\\\\$"));
            string swapKey = DataFormatTools.CleanQuotations(swapResult.Groups[1].Value.Replace("\\$", "\\\\$"));

            string keys = "(" + string.Join("|", reverseKey, sliceKey, spliceKey, swapKey) + ")";
            string keyStr = "(?:a=)?" + obj + "(?:\\." + keys + "|\\['" + keys + "'\\]|\\[\"" + keys + "\"\\])\\(a,(\\d+)\\)";
            Regex keyRegex = new Regex(keyStr);

            List<string> tokens = new List<string>();

            MatchCollection matches = keyRegex.Matches(funcBody);

            foreach (Match m in matches)
            {
                string key = m.Groups[1].Value ?? m.Groups[2].Value ?? m.Groups[3].Value;

                if (key == swapKey)
                {
                    tokens.Add("w" + m.Groups[4].Value);
                }
                else if (key == reverseKey)
                {
                    tokens.Add("r");
                }
                else if (key == sliceKey) {
                    tokens.Add("s" + m.Groups[4].Value);
                }
                else if (key == spliceKey)
                {
                    tokens.Add("p" + m.Groups[4].Value);
                }
            }

            return tokens.ToArray();
        }

        internal string Decipher(string[] tokens, string signature)
        {
            char[] sig = signature.ToCharArray();
            int len = tokens.Length;

            for (int i = 0; i < len; i++)
            {
                string token = tokens[i];

                if (token[0] == 'r')
                {
                    Array.Reverse(sig);
                }
                else if (token[0] == 'w') {
                    int position = ~~int.Parse(token.Substring(1));
                    swapHeadAndPosition(ref sig, position);
                }
                else if (token[0] == 's')
                {
                    int position = ~~int.Parse(token.Substring(1));
                    sig = slice(sig, position);
                }
                else if (token[0] == 'p')
                {
                    int position = ~~int.Parse(token.Substring(1));
                    sig = slice(sig, position);
                }
            }

            return string.Join("", sig);
        }

        internal void swapHeadAndPosition(ref char[] array, int position)
        {
            char first = array[0];
            array[0] = array[position % array.Length];
            array[position] = first;
        }

        internal char[] slice(char[] array, int amount)
        {
            List<char> sliced = new List<char>();

            for (int i = amount; i < array.Length; i++)
            {
                sliced.Add(array[i]);
            }

            return sliced.ToArray();
        }

    }
}
