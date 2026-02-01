using System.Collections.Generic;

namespace NXDSteamPlugin.WebApi.AuthenticationService
{
    public class BeginAuthSessionViaQRResponse
    {
        public ulong ClientId { get; set; }

        public string ChallengeUrl { get; set; }

        public byte[] RequestId { get; set; }

        public float Interval { get; set; }

        public List<AllowedConfirmation> AllowedConfirmations { get; set; }

        public int Version { get; set; }

        public class AllowedConfirmation
        {
            public int ConfirmationType { get; set; }
        }
    }
}