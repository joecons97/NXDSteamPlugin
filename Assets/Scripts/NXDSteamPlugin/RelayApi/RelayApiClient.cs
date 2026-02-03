using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace NXDSteamPlugin.RelayApi
{
    public class RelayApiClient
    {
        public const string BASE_URL = "https://nxe-steam-api-relay.pages.dev";

        public static string GetRelayUrl(string randomCode)
        {
            return $"{BASE_URL}?key={GetKey(randomCode)}";
        }

        private static string GetKey(string randomCode)
        {
            var deviceId = SystemInfo.deviceUniqueIdentifier;
            
            var rawToken = deviceId + randomCode;
            var token = BitConverter.ToString(
                System.Security.Cryptography.SHA256.Create()
                    .ComputeHash(Encoding.UTF8.GetBytes(rawToken))
            ).Replace("-", "").ToLower();
            
            return token;
        }

        public async UniTask<PollRelayResponse?> PollRelay(string code, CancellationToken cancellationToken = default)
        {
            var url = $"{BASE_URL}/poll?key={GetKey(code)}";

            var request = UnityWebRequest.Get(url);
            
            try
            {
                await request.SendWebRequest().WithCancellation(cancellationToken);
            }
            catch (UnityWebRequestException ex)
            {
                if (ex.ResponseCode != 401)
                    throw;
                
                return null;
            }

            if (request.result == UnityWebRequest.Result.Success)
                return JsonConvert.DeserializeObject<PollRelayResponse>(request.downloadHandler.text);
            
            Debug.LogError(request.error);
            return null;
        }
    }

    public class PollRelayResponse
    {
        [JsonProperty("apikey")]
        public string ApiKey { get; set; }
        [JsonProperty("userId")]
        public string UserId { get; set; }
    }
}
