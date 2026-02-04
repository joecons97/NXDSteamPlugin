using Newtonsoft.Json;

namespace NXDSteamPlugin.RelayApi
{
    public class PollRelayResponse
    {
        [JsonProperty("apikey")]
        public string ApiKey { get; set; }
        [JsonProperty("userId")]
        public string UserId { get; set; }
    }
}