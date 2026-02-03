using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NXDSteamPlugin.RelayApi;
using UnityEngine;

namespace NXDSteamPlugin.Services
{
    public class SteamToken
    {
        public string ApiKey { get; }
        public string SteamId { get; }

        public SteamToken(string steamId, string apiKey)
        {
            SteamId = steamId;
            ApiKey = apiKey;
        }
    }

    public class SteamAuthService
    {
        private readonly RelayApiClient relayApiClient = new();

        public async UniTask<SteamToken> PollForTokenAsync(string code, CancellationToken cancellationToken)
        {
            PollRelayResponse response = null;

            while (!cancellationToken.IsCancellationRequested && response == null)
            {            
                await UniTask.WaitForSeconds(2, cancellationToken: cancellationToken);
                response = await relayApiClient.PollRelay(code, cancellationToken);
            }
            
            return response == null 
                ? null 
                : new SteamToken(response.UserId, response.ApiKey);
        }
        
        public SteamToken LoadValidToken()
        {
            var path = Application.persistentDataPath + "/steam_token.json";
            try
            {
                var json = File.ReadAllText(path);
                var jObject = JObject.Parse(json);
                var token = new SteamToken(
                    apiKey: jObject[nameof(SteamToken.ApiKey)].Value<string>() ?? throw new Exception("Api Key is null!"),
                    steamId: jObject[nameof(SteamToken.SteamId)].Value<string>() ?? throw new Exception("SteamId Token is null!")
                );

                return token;
            }
            catch (Exception ex)
            {
                Debug.Log("Steam: Token file not found or invalid.");
                Debug.LogException(ex);
                return null;
            }
        }
        
        public void SaveToken(SteamToken token)
        {
            if(token == null) return;
            
            var json = JsonConvert.SerializeObject(token);
            var path = Application.persistentDataPath + "/steam_token.json";
            
            if(Directory.Exists(Application.persistentDataPath) == false) 

                if (Directory.Exists(Application.persistentDataPath) == false)
                    Directory.CreateDirectory(Application.persistentDataPath);
            

            File.WriteAllText(path, json);
        }
    }
}