using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NXDSteamPlugin.WebApi;
using NXDSteamPlugin.WebApi.AuthenticationService;
using UnityEngine;

namespace NXDSteamPlugin.Services
{
    public class SteamToken
    {
        public string RefreshToken { get; }
        public string AccessToken { get; }
        public string SteamId { get; }
        public string Username { get; }

        public SteamToken()
        {
        }

        public SteamToken(string refreshToken, string accessToken, string username)
        {
            RefreshToken = refreshToken;
            AccessToken = accessToken;
            Username = username;

            var payload = JObject.Parse(GetPayload());
            SteamId = payload["sub"].Value<string>();
        }

        public string GetPayload()
        {
            var encodedPayload = AccessToken.Split('.')[1];

            //Convert from bs jwt base64 to actual base64
            encodedPayload = encodedPayload.Replace('-', '+').Replace('_', '/');

            // Add padding if needed
            switch (encodedPayload.Length % 4)
            {
                case 2: encodedPayload += "=="; break;
                case 3: encodedPayload += "="; break;
            }

            byte[] data = Convert.FromBase64String(encodedPayload);
            return System.Text.Encoding.UTF8.GetString(data);
        }
    }

    public class SteamAuthService
    {
        private readonly AuthenticationServiceClient authenticationServiceClient = new();

        public async UniTask<BeginAuthSessionViaQRResponse> BeginLoginAsync(CancellationToken cancellationToken = default)
        {
            var beginLoginResponse = await authenticationServiceClient.BeginAuthSessionViaQRAsync(cancellationToken);

            return beginLoginResponse;
        }

        public async UniTask<SteamToken> AwaitLoginCompletionAsync(BeginAuthSessionViaQRResponse beginLoginResponse, Action<PollAuthSessionStatusResponse> onNewChallengeReceived, CancellationToken cancellationToken = default)
        {
            SteamToken tokenResponse = null;

            while (cancellationToken.IsCancellationRequested == false)
            {
                await UniTask.WaitForSeconds(beginLoginResponse.Interval, cancellationToken: cancellationToken);

                var pollResponse = await authenticationServiceClient.PollAuthSessionStatusAsync(beginLoginResponse.ClientId, beginLoginResponse.RequestId, cancellationToken);
                if (string.IsNullOrEmpty(pollResponse.AccessToken) == false)
                {
                    tokenResponse = new SteamToken(pollResponse.RefreshToken, pollResponse.AccessToken, pollResponse.AccountName);

                    break;
                }
                else if (string.IsNullOrEmpty(pollResponse.NewChallengeUrl) == false)
                {
                    beginLoginResponse.ChallengeUrl = pollResponse.NewChallengeUrl;
                    beginLoginResponse.ClientId = pollResponse.NewClientId;
                    onNewChallengeReceived?.Invoke(pollResponse);
                }
            }

            return tokenResponse;
        }

        public async UniTask<SteamToken> RefreshTokenAsync(SteamToken token, CancellationToken cancellationToken = default)
        {
            var newToken = await authenticationServiceClient.GenerateAccessTokenForAppAsync(token, cancellationToken);
            return newToken;
        }

        public void SaveToken(SteamToken token)
        {
            if (token == null) return;

            var json = JsonConvert.SerializeObject(token);
            var path = Application.persistentDataPath + "/steam_token.json";

            if (Directory.Exists(Application.persistentDataPath) == false)
                Directory.CreateDirectory(Application.persistentDataPath);

            File.WriteAllText(path, json);
        }

        public SteamToken LoadValidToken()
        {
            var path = Application.persistentDataPath + "/steam_token.json";
            try
            {
                var json = File.ReadAllText(path);
                var jObject = JObject.Parse(json);
                var token = new SteamToken(
                    refreshToken: jObject[nameof(SteamToken.RefreshToken)].Value<string>() ?? throw new Exception("Refresh Token is null!"),
                    accessToken: jObject[nameof(SteamToken.AccessToken)].Value<string>() ?? throw new Exception("Access Token is null!"),
                    username: jObject[nameof(SteamToken.Username)].Value<string>() ?? throw new Exception("Username is null!")
                );

                var payload = JObject.Parse(token.GetPayload());
                var exp = payload["exp"].Value<long>();

                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (now > exp)
                {
                    Debug.LogWarning("Access Token is expired but it should have already been refreshed!");
                    return null;
                }

                return token;
            }
            catch (Exception ex)
            {
                Debug.Log("Steam: Token file not found or invalid.");
                Debug.LogException(ex);
                return null;
            }
        }
    }
}