using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using LibraryPlugin;
using NXDSteamPlugin.WebApi;

namespace NXDSteamPlugin.Services
{
    public class AdditionalMetadataService
    {
        private AppDetailsClient client = new();

        public async UniTask<AdditionalMetadata> GetAdditionalMetadata(string appId, CancellationToken cancellationToken)
        {
            var data = await client.GetAppDetailsAsync(appId, cancellationToken);
            if (data == null)
                return null;

            var screenshots = await data.Screenshots
                .Select(x => UniTask.FromResult(x.PathFull)) ?? Array.Empty<string>();

            var genres = await data.Genres
                .Select(x => UniTask.FromResult(x.Description)) ?? Array.Empty<string>();

            var result = new AdditionalMetadata(
                data.ShortDescription,
                screenshots,
                (data.Developers ?? new List<string>(0)).ToArray(),
                (data.Publishers ?? new List<string>(0)).ToArray(),
                genres,
                data.ReleaseDate?.ComingSoon == true
                    ? null
                    : DateTime.TryParse(
                        data.ReleaseDate?.Date,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowWhiteSpaces,
                        out var releaseDate)
                        ? releaseDate
                        : null
            );

            return result;
        }
    }
}