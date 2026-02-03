using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LibraryPlugin;
using Newtonsoft.Json;
using NXDSteamPlugin.WebApi;
using UnityEngine;

namespace NXDSteamPlugin.Services
{
    public class AdditionalMetadataService
    {
        private AppDetailsClient client = new();

        public async UniTask<AdditionalMetadata> GetAdditionalMetadata(string appId, CancellationToken cancellationToken)
        {
            var data = await client.GetAppDetailsAsync(appId, cancellationToken);
            if(data == null)
                return null;
            
            Debug.Log(JsonConvert.SerializeObject(data));
            
            var screenshots = await data.Screenshots
                .Select(x => UniTask.FromResult(x.PathFull));

            var genres = await data.Genres
                .Select(x => UniTask.FromResult(x.Description));
            
            var result = new AdditionalMetadata(
                data.ShortDescription,
                screenshots,
                data.Developers.ToArray(),
                data.Publishers.ToArray(),
                genres,
                data.ReleaseDate.ComingSoon
                    ? null
                    : DateTime.Parse(data.ReleaseDate.Date)
            );
            
            return result;
        }
    }
}