using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MediaButler.BaseProcess.CognitiveServices
{
    internal class AzureMLTextAnalyticsClient : IAzureMLTextAnalyticsClient
    {
        private string responseBody;
        private async Task call(string myKey, string uri, string body)
        {
            HttpResponseMessage response;
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", myKey);
            byte[] byteData = Encoding.UTF8.GetBytes(body);
            try
            {
                using (var content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await client.PostAsync(uri, content);
                    responseBody = await response.Content.ReadAsStringAsync();
                    Trace.TraceInformation(responseBody);
                }
            }
            catch (Exception X)
            {
                Trace.TraceError("AzureMLTextAnalyticsClient Error CALL " + X.Message);
                throw X;
            }
        }
        public string keyPhrases(string jsonText, Language idiom, string APIurl, string APIKey)
        {

            string answer = "{}";
            call(APIKey, APIurl, jsonText).Wait();
            answer = responseBody;
            return answer;
        }
        private List<string> parseWEBVTT(string txt)
        {
            string[] stringSeparators = new string[] { "\r\n" };
            List<string> prhases = txt.Split(stringSeparators, StringSplitOptions.None).ToList();
            List<string> info = new List<string>();
            for (int i = 5; i < prhases.Count; i += 3)
            {
                info.Add(prhases[i]);
            }
            Trace.TraceInformation("# phrases " + info.Count);
            return info;
        }
        public string keyPhrasesTxt(string txt, Language idiom, FileType type, string APIurl, string APIKey)
        {
            List<string> lines = null;
            switch (type)
            {
                case FileType.VTT:
                    lines = parseWEBVTT(txt);
                    break;
                case FileType.TTML:
                    break;
                default:
                    break;
            }
            int id = 0;
            string acc = "";
            List<string> docs = new List<string>();
            foreach (string line in lines)
            {
                acc += line;
                if (acc.Length >= 5000)
                {
                    docs.Add("{\"language\": \"" + idiom.ToString() + "\",\"id\": \"" + id.ToString() + "\",\"text\": \"" + acc + "\"}");
                    acc = "";
                    id += 1;
                }
            }
            docs.Add("{\"language\": \"" + idiom.ToString() + "\",\"id\": \"" + id.ToString() + "\",\"text\": \"" + acc + "\"}");

            string myBody = "{\"documents\": [";
            myBody += string.Join(",", docs.ToArray());
            myBody += "]}";

            return keyPhrases(myBody, idiom, APIurl, APIKey);
        }
        public string keyPhrases(Uri text, Language idiom, FileType type, string APIurl, string APIKey)
        {
            string httpTXT;
            using (WebClient client = new WebClient())
            {
                httpTXT = client.DownloadString(text.AbsoluteUri);
            }
            return keyPhrasesTxt(httpTXT, idiom, FileType.VTT, APIurl, APIKey);
        }
    }
}
