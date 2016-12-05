using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess.CognitiveServices
{
    enum FileType
    {
        VTT, TTML
    }

    enum Language
    { en, ja, de, es }
    interface IAzureMLTextAnalyticsClient
    {
        string keyPhrasesTxt(string txt, Language idiom, FileType type, string APIurl, string APIKey);
        string keyPhrases(Uri text, Language idiom, FileType type, string APIurl, string APIKey);
        string keyPhrases(string jsonText, Language idiom, string APIurl, string APIKey);
    }
}
