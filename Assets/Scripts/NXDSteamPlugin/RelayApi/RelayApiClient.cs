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
            
            var request = UnityWebRequest.Get(url);
            await request.SendWebRequest().WithCancellation(cancellationToken);
            
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
