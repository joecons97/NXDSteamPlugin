#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LibraryPlugin;
using NXDSteamPlugin.Helpers;
using NXDSteamPlugin.Services.GameDetection;

namespace NXDSteamPlugin.Services.Processes
{
    public class InstallEntryService
    {
        public GameActionResult InstallEntry(SteamPlugin plugin, LibraryEntry entry, CancellationToken cancellationToken)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Steam.ClientExecPath,
                    Arguments = $"-silent \"steam://install/{entry.EntryId}\"",
                    UseShellExecute = true
                });

                _ = UniTask.RunOnThreadPool(async () => { await MonitorGameInstallation(plugin, entry, cancellationToken); }, cancellationToken: cancellationToken);

                return GameActionResult.Success;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                return GameActionResult.Fail;
            }
        }

        private async UniTask MonitorGameInstallation(SteamPlugin plugin, LibraryEntry entry, CancellationToken cancellationToken)
        {
            LibraryEntry? game;
            while (TryGetGame(entry.EntryId, out game) == false)
            {
                await UniTask.Delay(1000);

                if (cancellationToken.IsCancellationRequested)
                {
                    if(plugin.OnEntryInstallationCancelled != null)
                        await plugin.OnEntryInstallationCancelled(entry.EntryId, plugin);
                    
                    return;
                }
            }
            
            if(plugin.OnEntryInstallationComplete != null)
                await plugin.OnEntryInstallationComplete(entry.EntryId, game.Path, plugin);
        }

        private bool TryGetGame(string entryId, [NotNullWhen(true)] out LibraryEntry? game)
        {
            game = SteamLocalService.GetInstalledGamesAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult()
                .FirstOrDefault(x => x.EntryId == entryId);

            return game != null;
        }
    }
}