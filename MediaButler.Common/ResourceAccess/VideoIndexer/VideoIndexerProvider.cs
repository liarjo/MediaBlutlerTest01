using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common.ResourceAccess.VideoIndexer
{
    internal class VideoIndexerProvider : IVideoIndexerProvider
    {
        private string _ApiKey;
        private string _EndPoint;
        HttpClient _HttpClient;
        public VideoIndexerProvider(string ApiKey, string EndPoint)
        {
            _ApiKey = ApiKey;
            _EndPoint = EndPoint;
            _HttpClient = new HttpClient();
            _HttpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _ApiKey);
            _HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public async Task<VideoIndexerAnswer> UploadVideo(Dictionary<string, string> VideoMetaData)
        {
            VideoIndexerAnswer ApiResult = new VideoIndexerAnswer();
            //Create URL with parameters
            string uri = string.Format("{0}?{1}", _EndPoint, string.Join("&", VideoMetaData.Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value))));
            HttpResponseMessage response;
            var content = new MultipartFormDataContent();
            response = await _HttpClient.PostAsync(uri, content);
            if (response.IsSuccessStatusCode)
            {
                ApiResult.IsError = false;
                ApiResult.VideoIndexId = response.Content.ReadAsStringAsync().Result;
            }

            else
            {
                ApiResult.IsError = true;
                ApiResult.Error = $"response.StatusCode= {response.StatusCode}";
                //Error
                Trace.TraceError(ApiResult.Error);
            }

            return ApiResult;
        }
        public async Task<ProcessState> GetProcessSatet(string VideoIndexerId)
        {
            ProcessState myProcessSate = new ProcessState();
            string uri = $"https://videobreakdown.azure-api.net/Breakdowns/Api/Partner/Breakdowns/{VideoIndexerId}/State";

            HttpResponseMessage response = await _HttpClient.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {

                myProcessSate = Newtonsoft.Json.JsonConvert.DeserializeObject<ProcessState>(response.Content.ReadAsStringAsync().Result);
            }
            else
            {
                myProcessSate.ErrorType = response.StatusCode.ToString();
                myProcessSate.Message = response.ReasonPhrase;
            }
            return myProcessSate;
        }
    }
}
