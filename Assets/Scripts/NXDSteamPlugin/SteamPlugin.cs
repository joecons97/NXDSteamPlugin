using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LibraryPlugin;
using NXDSteamPlugin.RelayApi;
using NXDSteamPlugin.Services;
using NXDSteamPlugin.Services.Artwork;
using NXDSteamPlugin.Services.GameDetection;
using NXDSteamPlugin.Services.Processes;
using QRCoder;
using QRCoder.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace NXDSteamPlugin
{
    public class SteamPlugin : LibraryPlugin.LibraryPlugin
    {
        public override string Name => "Steam";

        public override string Description => "Steam";

        private ModalService modalService { get; } = new();
        private ArtworkService artworkService { get; } = new();
        private StartEntryService startEntryService { get; } = new();
        private SteamAuthService steamAuthService { get; } = new();
        private StartClientService startClientService { get; } = new();
        private SteamOwnedGamesService steamOwnedGamesService => new(steamAuthService);
        private InstallEntryService installEntryService { get; } = new();
        private UninstallEntryService uninstallEntryService { get; } = new();
        private AdditionalMetadataService additionalMetadataService { get; } = new();

        public override async UniTask<AdditionalMetadata> GetAdditionalMetadata(string entryId, CancellationToken cancellationToken)
        {
            return await additionalMetadataService.GetAdditionalMetadata(entryId, cancellationToken);
        }

        public override async UniTask<ArtworkCollection> GetArtworkCollection(string entryId, CancellationToken cancellationToken)
        {
            var collection = await artworkService.GetArtworkAsync(entryId, cancellationToken);

            return collection;
        }

        public override async UniTask<List<LibraryEntry>> GetEntriesAsync(CancellationToken cancellationToken)
        {
            var installedGames = await SteamLocalService.GetInstalledGamesAsync(cancellationToken);
            var ownedGames = await steamOwnedGamesService.GetOwnedGamesAsync(cancellationToken);

            var finalList = new List<LibraryEntry>(installedGames);

            foreach (var game in ownedGames)
            {
                if (finalList.Any(x => x.EntryId == game.EntryId))
                    continue;

                finalList.Add(game);
            }

            return finalList;
        }

        public override UniTask<GameActionResult> TryStartEntryAsync(LibraryEntry entry, CancellationToken cancellationToken)
        {
            return UniTask.FromResult(startEntryService.StartEntry(this, entry, cancellationToken));
        }

        public override UniTask<GameActionResult> TryInstallEntryAsync(LibraryEntry entry, CancellationToken cancellationToken)
        {
            return UniTask.FromResult(installEntryService.InstallEntry(this, entry, cancellationToken));
        }

        public override UniTask<GameActionResult> TryUninstallEntryAsync(LibraryEntry entry, CancellationToken cancellationToken)
        {
            return UniTask.FromResult(uninstallEntryService.UninstallEntry(this, entry, cancellationToken));
        }

        public override UniTask OpenLibraryApplication(LibraryLocation location)
        {
            Debug.Log("Opening Steam at " + location);
            startClientService.StartClient(location);
            return UniTask.CompletedTask;
        }

        public override List<LibraryPluginButton> GetButtons()
        {
            var list = new List<LibraryPluginButton>();
            var result = steamAuthService.LoadValidToken();
            if (result == null)
                list.Add(new LibraryPluginButton()
                {
                    Name = "Authenticate",
                    Description = "Authenticate with Steam. This is required to load your game library.",
                    Action = Authenticate
                });
            else
                list.Add(new LibraryPluginButton()
                {
                    Name = "Authenticated",
                    Description = $"Authenticated as {result.SteamId}"
                });
            
            return list;
        }

        private async UniTask Authenticate(CancellationToken cancellationToken)
        {
            var closureCancellationToken = new CancellationTokenSource();
            var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, closureCancellationToken.Token).Token;
            
            Debug.Log("Authenticating");

            var root = new GameObject("Root", typeof(RectTransform), typeof(VerticalLayoutGroup));
            root.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
            root.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            
            var textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(root.transform, false);
            var text = textObj.GetComponent<Text>();
            text.font = Resources.Load<Font>("NXD");
            text.fontSize = 26;
            text.supportRichText = true;
            text.text = $"Please scan the QR below to authenticate.";

            var qrCodeObj = new GameObject("QRCode", typeof(RectTransform), typeof(RawImage), typeof(LayoutElement), typeof(AspectRatioFitter));
            qrCodeObj.transform.SetParent(root.transform, false);
            var qrCode = qrCodeObj.GetComponent<RawImage>();
            qrCodeObj.GetComponent<AspectRatioFitter>().aspectRatio = 1;
            qrCodeObj.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            qrCodeObj.GetComponent<LayoutElement>().preferredHeight = 256;
            qrCodeObj.GetComponent<LayoutElement>().preferredWidth = 256;

            UpdateQrCodeImage(qrCode, RelayApiClient.GetRelayUrl());

            var id = modalService.CreateModal(new CreateModalArgs()
            {
                Name = "Authenticate",
                CanBeClosed = true,
                ChildrenRoot = root
            });
            
            modalService.SetCloseCallback(id, () =>
            {
                closureCancellationToken.Cancel();
            });

            var token = await steamAuthService.PollForTokenAsync(linkedCancellationToken);

            Debug.Log("Authenticated!");
            
            steamAuthService.SaveToken(token);

            modalService.CloseModal(id);
        }

        private void UpdateQrCodeImage(RawImage imageComponent, string url)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new UnityQRCode(qrCodeData);
            var qrCodeAsTexture2D = qrCode.GetGraphic(20);

            imageComponent.texture = qrCodeAsTexture2D;
        }
    }
}