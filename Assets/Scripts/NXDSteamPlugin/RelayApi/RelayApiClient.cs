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

        public async UniTask<PollRelayResponse?> PollRelay(string code, CancellationToken cancellationToken = default)
        {
            var url = $"{BASE_URL}/poll?code={code}";
            
            Debug.Log($"Polling {url}");
            
            var request = UnityWebRequest.Get(url);
            await request.SendWebRequest().WithCancellation(cancellationToken);
            
            return JsonConvert.DeserializeObject<PollRelayResponse>(request.downloadHandler.text);
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
