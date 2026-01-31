using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using NXDSteamPlugin.Generated;
using NXDSteamPlugin.Services;
using NXDSteamPlugin.WebApi.AuthenticationService;
using ProtoBuf;
using UnityEngine;
using UnityEngine.Networking;

namespace NXDSteamPlugin.WebApi
{
    [System.Serializable]
    public class ProtobufResponseWrapper
    {
        public string response;
    }

    public class AuthenticationServiceClient
    {
        private void SetHeaders(UnityWebRequest request)
        {
            request.SetRequestHeader("user-agent", "okhttp/4.9.2");
            request.SetRequestHeader("cookie", "mobileClient=android; mobileClientVersion=777777 3.10.3");
        }
        
        public async UniTask<BeginAuthSessionViaQRResponse> BeginAuthSessionViaQRAsync(CancellationToken cancellationToken = default)
        {
            WWWForm form = new WWWForm();
            form.AddField("device_friendly_name", SystemInfo.deviceName);
            form.AddField("website_id", Application.productName);
            using UnityWebRequest request = UnityWebRequest.Post("https://api.steampowered.com/IAuthenticationService/BeginAuthSessionViaQR/v1/", form);
            SetHeaders(request);
            
            await request.SendWebRequest().WithCancellation(cancellationToken);

            var json = request.downloadHandler.text;
            var response = JsonConvert.DeserializeObject<BeginAuthSessionViaQRResponse>(json);

            return response;
        }

        public async UniTask<PollAuthSessionStatusResponse> PollAuthSessionStatusAsync(string clientId, string requestId, CancellationToken cancellationToken = default)
        {
            WWWForm form = new WWWForm();
            form.AddField("client_id", clientId);
            form.AddField("request_id", requestId);
            using UnityWebRequest request = UnityWebRequest.Post("https://api.steampowered.com/IAuthenticationService/PollAuthSessionStatus/v1/", form);
            SetHeaders(request);
            
            await request.SendWebRequest().WithCancellation(cancellationToken);

            var json = request.downloadHandler.text;
            Debug.Log(request.downloadHandler.text);

            var response = JsonConvert.DeserializeObject<PollAuthSessionStatusResponse>(json);

            return response;
        }

        public async UniTask<SteamToken> GenerateAccessTokenForAppAsync(
            SteamToken steamToken,
            CancellationToken cancellationToken = default)
        {
            // Create the protobuf request
            var request = new CAuthentication_AccessToken_GenerateForApp_Request
            {
                refresh_token = steamToken.RefreshToken,
                steamid = ulong.Parse(steamToken.SteamId),
                renewal_type = ETokenRenewalType.k_ETokenRenewalType_None
            };

            // Serialize to bytes using protobuf-net
            byte[] protobufData;
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, request);
                protobufData = stream.ToArray();
            }
            
            // Debug output
            Debug.Log($"Protobuf bytes: {BitConverter.ToString(protobufData)}");
            Debug.Log($"Protobuf length: {protobufData.Length}");

            string base64Data = Convert.ToBase64String(protobufData);
            Debug.Log($"Base64: {base64Data}");

            // Create the web request
            WWWForm form = new WWWForm();
            form.AddField("input_protobuf_encoded", base64Data);

            using UnityWebRequest webRequest = UnityWebRequest.Post(
                "https://api.steampowered.com/IAuthenticationService/GenerateAccessTokenForApp/v1/",
                form
            );
            SetHeaders(webRequest);

            // await webRequest.SendWebRequest().WithCancellation(cancellationToken);
            //
            // if (webRequest.result != UnityWebRequest.Result.Success)
            // {
            //     Debug.LogError($"Request failed: {webRequest.error}");
            //     Debug.LogError($"Response: {webRequest.downloadHandler.text}");
            //     return null;
            // }
            //
            // // Parse the response
            // string responseJson = webRequest.downloadHandler.text;
            // Debug.Log($"Response JSON: {responseJson}");
            //
            // // The response should be a JSON wrapper containing the protobuf response
            // var wrapper = JsonUtility.FromJson<ProtobufResponseWrapper>(responseJson);
            //
            // if (wrapper?.response != null)
            // {
            //     // Decode the base64 response if it's encoded
            //     Debug.Log($"Response: {wrapper.response}");
            //     byte[] responseBytes = Convert.FromBase64String(wrapper.response);
            //
            //     // Deserialize the protobuf response
            //     CAuthentication_AccessToken_GenerateForApp_Response protoResponse;
            //     using (var stream = new MemoryStream(responseBytes))
            //     {
            //         protoResponse = Serializer.Deserialize<CAuthentication_AccessToken_GenerateForApp_Response>(stream);
            //     }
            //
            //     // Return the new token
            //     return new SteamToken(protoResponse.access_token, protoResponse.refresh_token, steamToken.Username);
            // }

            return null;
        }
    }
}