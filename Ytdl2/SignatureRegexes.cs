using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ytdl2
{
    class SignatureRegexes
    {
        static internal string jsVarStr = "[a-zA-Z_\\$][a-zA-Z_0-9]*";
        static internal string jsSingleQuoteStr = "'[^'\\\\]*(:?\\\\[\\s\\S][^'\\\\]*)*'";
        static internal string jsDoubleQuoteStr = "\"[^\"\\\\]*(:?\\\\[\\s\\S][^\"\\\\]*)*\"";
        static internal string jsQuoteStr = "(?:" + jsSingleQuoteStr + "|" + jsDoubleQuoteStr + ")";
        static internal string jsKeyStr = "(?:" + jsVarStr + "|" + jsQuoteStr + ")";
        static internal string jsPropStr = "(?:\\." + jsVarStr + "|\\[" + jsQuoteStr + "\\])";
        static internal string jsEmptyStr = "(?:''|\"\")";
        static internal string reverseStr = ":function\\(a\\)\\{(?:return )?a\\.reverse\\(\\)\\}";
        static internal string sliceStr = ":function\\(a,b\\)\\{return a\\.slice\\(b\\)\\}";
        static internal string spliceStr = ":function\\(a,b\\)\\{a\\.splice\\(0,b\\)\\}";
        static internal string swapStr = ":function\\(a,b\\)\\{var c=a\\[0\\];a\\[0\\]=a\\[b(?:%a\\.length)?\\];a\\[b(?:%a\\.length)?\\]=c(?:;return a)?\\}";

        static internal Regex actionsObjRegexp = new Regex("var (" + jsVarStr + ")=\\{((?:(?:" +
            jsKeyStr + reverseStr + '|' +
            jsKeyStr + sliceStr + '|' +
            jsKeyStr + spliceStr + '|' +
            jsKeyStr + swapStr +
            "),?\\r?\\n?)+)\\};");

        static internal Regex actionsFuncRegexp = new Regex("function(?: " + jsVarStr + ")?\\(a\\)\\{" +
            "a=a\\.split\\(" + jsEmptyStr + "\\);\\s*" +
            "((?:(?:a=)?" + jsVarStr +
            jsPropStr +
            "\\(a,\\d+\\);)+)" +
            "return a\\.join\\(" + jsEmptyStr + "\\)" +
            "\\}");

        static internal Regex reverseRegexp = new Regex("(?:^|,)(" + jsKeyStr + ")" + reverseStr, RegexOptions.Multiline);
        static internal Regex sliceRegexp = new Regex("(?:^|,)(" + jsKeyStr + ")" + sliceStr, RegexOptions.Multiline);
        static internal Regex spliceRegexp = new Regex("(?:^|,)(" + jsKeyStr + ")" + spliceStr, RegexOptions.Multiline);
        static internal Regex swapRegexp = new Regex("(?:^|,)(" + jsKeyStr + ")" + swapStr, RegexOptions.Multiline);

        static internal Regex quoteCleanerRegexp = new Regex("\\$|^'|^\" | '$|\"$");
    }
}
