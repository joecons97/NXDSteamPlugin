namespace NXDSteamPlugin.WebApi.AuthenticationService
{
    public class PollAuthSessionStatusResponse
    {
        public string RefreshToken { get; set; }

        public string AccessToken { get; set; }

        public bool HadRemoteInteraction { get; set; }

        public string AccountName { get; set; }

        public ulong NewClientId { get; set; }
            
        public string NewChallengeUrl { get; set; }
    }
}