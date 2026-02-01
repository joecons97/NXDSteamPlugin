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
                await UniTask.WaitForSeconds(beginLoginResponse.Response.Interval, cancellationToken: cancellationToken);

                var pollResponse = await authenticationServiceClient.PollAuthSessionStatusAsync(beginLoginResponse.Response.ClientId, beginLoginResponse.Response.RequestId, cancellationToken);
                if (string.IsNullOrEmpty(pollResponse.Response.AccessToken) == false)
                {
                    tokenResponse = new SteamToken(pollResponse.Response.RefreshToken, pollResponse.Response.AccessToken, pollResponse.Response.AccountName);

                    break;
                }
                else if (string.IsNullOrEmpty(pollResponse.Response.NewChallengeUrl) == false)
                {
                    beginLoginResponse.Response.ChallengeUrl = pollResponse.Response.NewChallengeUrl;
                    beginLoginResponse.Response.ClientId = pollResponse.Response.NewClientId;
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
            if(token == null) return;
            var json = JsonConvert.SerializeObject(token);
            var path = Application.persistentDataPath + "/steam_token.json";
            
            Debug.Log($"Saving Steam Token {json} to {path}");
            
            if(Directory.Exists(Application.persistentDataPath) == false) 
                Directory.CreateDirectory(Application.persistentDataPath);
            
            File.WriteAllText(path, json);
        }

        public SteamToken LoadValidToken()
        {
            var path = Application.persistentDataPath + "/steam_token.json";
            if(File.Exists(path) == false) return null;

            try
            {
                Debug.Log($"Loading Steam Token from {path}");
                var json = File.ReadAllText(path);
                Debug.Log($"Loaded Steam Token: {json}");
                var token = JsonConvert.DeserializeObject<SteamToken>(json);
                if (token == null)
                    return null;

                var payload = JObject.Parse(token.GetPayload());
                var exp = payload["exp"].Value<long>();

                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (now > exp)
                {
                    Debug.LogWarning("Access Token is expired but no refresh implemented");
                    return null;
                }

                return token;
            }
            catch
            {
                return null;
            }
        }

    }
}