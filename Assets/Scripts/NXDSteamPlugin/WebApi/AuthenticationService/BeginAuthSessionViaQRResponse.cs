using System.Collections.Generic;
using Newtonsoft.Json;

namespace NXDSteamPlugin.WebApi.AuthenticationService
{
    public class BeginAuthSessionViaQRResponse
    {
        [JsonProperty("response")]
        public ResponseData Response { get; set; }

        public class AllowedConfirmation
        {
            [JsonProperty("confirmation_type")]
            public int ConfirmationType { get; set; }
        }

        public class ResponseData
        {
            [JsonProperty("client_id")]
            public ulong ClientId { get; set; }

            [JsonProperty("challenge_url")]
            public string ChallengeUrl { get; set; }

            [JsonProperty("request_id")]
            public byte[] RequestId { get; set; }

            [JsonProperty("interval")]
            public float Interval { get; set; }

            [JsonProperty("allowed_confirmations")]
            public List<AllowedConfirmation> AllowedConfirmations { get; set; }

            [JsonProperty("version")]
            public int Version { get; set; }
        }
    }
}