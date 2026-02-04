using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NXDSteamPlugin.Extensions;
using UnityEngine;
using UnityEngine.Networking;

namespace NXDSteamPlugin.RelayApi
{
    public class RelayApiClient
    {
        public class RelayApiClientToken
        {
            public RelayApiClientToken(string publicKey, string sessionToken)
            {
                PublicKey = publicKey;
                SessionToken = sessionToken;
            }

            public string SessionToken { get; }
            public string PublicKey { get; }
        }
        
        public const string BASE_URL = "https://nxe-steam-api-relay.pages.dev";
        
        private static RSACryptoServiceProvider rsa;
        private static RelayApiClientToken cachedToken;

        public static string GetRelayUrl()
        {
            var token = GetToken();
            return $"{BASE_URL}?token={token.SessionToken}&pubkey={Uri.EscapeDataString(token.PublicKey)}";
        }

        public static RelayApiClientToken GetToken()
        {
            if(cachedToken != null) 
                return cachedToken;
            
            var sessionToken = Guid.NewGuid().ToString();
            rsa = new RSACryptoServiceProvider(2048);
            
            var publicKeyPem = rsa.ExportPublicKeyToPem();
            var publicKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKeyPem));

            cachedToken = new RelayApiClientToken(publicKeyBase64, sessionToken);
            
            return cachedToken;
        }
        

        public async UniTask<PollRelayResponse?> PollRelay(CancellationToken cancellationToken = default)
        {
            var token = GetToken();
            var url = $"{BASE_URL}/poll?token={token.SessionToken}";

            var request = UnityWebRequest.Get(url);
            
            try
            {
                Debug.Log("Polling relay");
                await request.SendWebRequest().WithCancellation(cancellationToken);
            }
            catch (UnityWebRequestException ex)
            {
                if (ex.ResponseCode != 401)
                    throw;
                
                return null;
            }

            var jObject = JObject.Parse(request.downloadHandler.text);
            Debug.Log(jObject);
            var data = jObject["encryptedData"];
            Debug.Log("\n" + data);
            if(data == null)
                return null;
            
            var decryptedData = DecryptWithPrivateKey(data.ToString());
            Debug.Log("\n" + decryptedData);
            
            if (request.result == UnityWebRequest.Result.Success)
                return JsonConvert.DeserializeObject<PollRelayResponse>(decryptedData);
            
            Debug.LogError(request.error);
            return null;
        }
        
        private string DecryptWithPrivateKey(string encryptedBase64)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);
            byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, false); // false = PKCS#1 padding
            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}
