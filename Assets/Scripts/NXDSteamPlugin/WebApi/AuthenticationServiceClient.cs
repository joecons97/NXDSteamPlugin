using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using NXDSteamPlugin.Generated;
using NXDSteamPlugin.Services;
using NXDSteamPlugin.WebApi.AuthenticationService;
using ProtoBuf;
using UnityEngine;
using UnityEngine.Networking;

namespace NXDSteamPlugin.WebApi
{
    public class AuthenticationServiceClient
    {
        private const string USER_AGENT = "okhttp/4.9.2";
        private const string MOBILE_COOKIE = "mobileClient=android; mobileClientVersion=777777 3.10.3";

        private Dictionary<string, string> cookies = new();

        public async UniTask<BeginAuthSessionViaQRResponse> BeginAuthSessionViaQRAsync(CancellationToken cancellationToken = default)
        {
            var requestModel = new CAuthentication_BeginAuthSessionViaQR_Request()
            {
                device_details = new CAuthentication_DeviceDetails()
                {
                    device_friendly_name = $"NXD-{SystemInfo.deviceName}",
                    platform_type = EAuthTokenPlatformType.k_EAuthTokenPlatformType_MobileApp,
                    os_type = -500,
                    gaming_device_type = 528
                }
            };
            
            // Serialize to bytes using protobuf-net
            byte[] protobufData;
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, requestModel);
                protobufData = stream.ToArray();
            }

            string base64Data = Convert.ToBase64String(protobufData);

            // Create the web request
            WWWForm form = new WWWForm();
            form.AddField("input_protobuf_encoded", base64Data);

            using UnityWebRequest request = UnityWebRequest.Post("https://api.steampowered.com/IAuthenticationService/BeginAuthSessionViaQR/v1/", form);

            SetHeaders(request);

            await request.SendWebRequest().WithCancellation(cancellationToken);
            StoreCookies(request);

            var response = Serializer.Deserialize<CAuthentication_BeginAuthSessionViaQR_Response>(request.downloadHandler.data.AsSpan());
            return new BeginAuthSessionViaQRResponse()
            {
                ChallengeUrl = response.challenge_url,
                ClientId = response.client_id,
                RequestId = response.request_id,
                Interval = response.interval,
                Version = response.version
            };
        }

        public async UniTask<PollAuthSessionStatusResponse> PollAuthSessionStatusAsync(ulong clientId, byte[] requestId, CancellationToken cancellationToken = default)
        {
            var model = new CAuthentication_PollAuthSessionStatus_Request()
            {
                client_id = clientId,
                request_id = requestId
            };
            
            byte[] protobufData;
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, model);
                protobufData = stream.ToArray();
            }

            string base64Data = Convert.ToBase64String(protobufData);

            // Create the web request
            WWWForm form = new WWWForm();
            form.AddField("input_protobuf_encoded", base64Data);

            using UnityWebRequest request = UnityWebRequest.Post("https://api.steampowered.com/IAuthenticationService/PollAuthSessionStatus/v1/", form);

            SetHeaders(request);

            await request.SendWebRequest().WithCancellation(cancellationToken);
            StoreCookies(request);

            var response = Serializer.Deserialize<CAuthentication_PollAuthSessionStatus_Response>(request.downloadHandler.data.AsSpan());
            
            return new PollAuthSessionStatusResponse()
            {
                AccountName = response.account_name,
                AccessToken = response.access_token,
                RefreshToken = response.refresh_token,
                NewChallengeUrl = response.new_challenge_url,
                NewClientId = response.new_client_id,
                HadRemoteInteraction = response.had_remote_interaction
            };
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

            string base64Data = Convert.ToBase64String(protobufData);

            // Create the web request
            WWWForm form = new WWWForm();
            form.AddField("input_protobuf_encoded", base64Data);

            using UnityWebRequest webRequest = UnityWebRequest.Post(
                "https://api.steampowered.com/IAuthenticationService/GenerateAccessTokenForApp/v1/",
                form
            );
            SetHeaders(webRequest);

            await webRequest.SendWebRequest().WithCancellation(cancellationToken);
            
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Request failed: {webRequest.error}");
                Debug.LogError($"Response: {webRequest.downloadHandler.text}");
                return null;
            }

            try
            {
                var response = Serializer.Deserialize<CAuthentication_AccessToken_GenerateForApp_Response>(webRequest.downloadHandler.data.AsSpan());
                return new SteamToken(response.access_token, response.refresh_token, steamToken.Username);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        private void SetHeaders(UnityWebRequest request)
        {
            request.SetRequestHeader("User-Agent", USER_AGENT);

            // Combine mobile client cookie with any session cookies
            string cookieHeader = MOBILE_COOKIE;
            if (cookies.Count > 0)
            {
                string sessionCookies = string.Join("; ", System.Linq.Enumerable.Select(cookies, kvp => $"{kvp.Key}={kvp.Value}"));
                cookieHeader += "; " + sessionCookies;
            }

            request.SetRequestHeader("Cookie", cookieHeader);
        }

        private void StoreCookies(UnityWebRequest request)
        {
            var headers = request.GetResponseHeaders();
            if (headers != null && headers.ContainsKey("Set-Cookie"))
            {
                string setCookieHeader = headers["Set-Cookie"];
                if (!string.IsNullOrEmpty(setCookieHeader))
                {
                    string[] parts = setCookieHeader.Split(';');
                    if (parts.Length > 0)
                    {
                        string[] nameValue = parts[0].Split('=');
                        if (nameValue.Length == 2)
                        {
                            cookies[nameValue[0].Trim()] = nameValue[1].Trim();
                        }
                    }
                }
            }
        }

        public void ClearCookies()
        {
            cookies.Clear();
        }
    }
}