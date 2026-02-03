using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace NXDSteamPlugin.WebApi
{
    public class AppDetailsClient
    {
        public async UniTask<AppDetailsDto> GetAppDetailsAsync(string appId, CancellationToken token)
        {
            using UnityWebRequest request = UnityWebRequest.Get($"https://store.steampowered.com/api/appdetails?appids={appId}");
            await request.SendWebRequest()
                .WithCancellation(token);

            var jObject = JObject.Parse(request.downloadHandler.text);
            var appDetails = jObject[appId];

            var data = appDetails?["data"];
            if (data == null)
                return null;
            
            var dataJson = data.ToString();
            
            return JsonConvert.DeserializeObject<AppDetailsDto>(dataJson);
        }
    }
    
    public class AppDetailsDto
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("steam_appid")]
        public int SteamAppid { get; set; }

        [JsonProperty("required_age")]
        public int RequiredAge { get; set; }

        [JsonProperty("is_free")]
        public bool IsFree { get; set; }

        [JsonProperty("controller_support")]
        public string ControllerSupport { get; set; }

        [JsonProperty("dlc")]
        public List<int> Dlc { get; set; } = new();

        [JsonProperty("detailed_description")]
        public string DetailedDescription { get; set; }

        [JsonProperty("about_the_game")]
        public string AboutTheGame { get; set; }

        [JsonProperty("short_description")]
        public string ShortDescription { get; set; }

        [JsonProperty("supported_languages")]
        public string SupportedLanguages { get; set; }

        [JsonProperty("reviews")]
        public string Reviews { get; set; }

        [JsonProperty("header_image")]
        public string HeaderImage { get; set; }

        [JsonProperty("capsule_image")]
        public string CapsuleImage { get; set; }

        [JsonProperty("capsule_imagev5")]
        public string CapsuleImagev5 { get; set; }

        [JsonProperty("website")]
        public string Website { get; set; }

        [JsonProperty("legal_notice")]
        public string LegalNotice { get; set; }

        [JsonProperty("ext_user_account_notice")]
        public string ExtUserAccountNotice { get; set; }

        [JsonProperty("developers")]
        public List<string> Developers { get; set; } = new();

        [JsonProperty("publishers")]
        public List<string> Publishers { get; set; } = new();

        [JsonProperty("genres")]
        public List<GenreDto> Genres { get; set; } = new();

        [JsonProperty("screenshots")] 
        public List<ScreenshotDto> Screenshots { get; set; } = new();

        [JsonProperty("release_date")]
        public ReleaseDateDto ReleaseDate { get; set; }

        public class GenreDto
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }
        }

        public class ReleaseDateDto
        {
            [JsonProperty("coming_soon")]
            public bool ComingSoon { get; set; }

            [JsonProperty("date")]
            public string Date { get; set; }
        }
        
        public class ScreenshotDto
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("path_thumbnail")]
            public string PathThumbnail { get; set; }

            [JsonProperty("path_full")]
            public string PathFull { get; set; }
        }
    }
}