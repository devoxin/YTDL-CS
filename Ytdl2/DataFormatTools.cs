using System;
using System.Collections.Generic;
using System.Text;

namespace Ytdl2
{
    class DataFormatTools
    {
        public static string ExtractBetween(string content, string start, string end)
        {
            int startMatch = content.IndexOf(start);

            if (startMatch >= 0)
            {
                int startPosition = startMatch + start.Length;
                int endPosition = content.IndexOf(end, startPosition);

                if (endPosition >= 0)
                {
                    int length = endPosition - startPosition;
                    return content.Substring(startPosition, length);
                }
            }

            return null;
        }

        public static string CleanQuotations(string content)
        {
            return SignatureRegexes.quoteCleanerRegexp.Replace(content, "");
        }
    }
}
