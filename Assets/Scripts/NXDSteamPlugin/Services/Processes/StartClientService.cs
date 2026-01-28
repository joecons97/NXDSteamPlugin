using System.Diagnostics;
using LibraryPlugin;
using NXDSteamPlugin.Helpers;

namespace NXDSteamPlugin.Services.Processes
{
    public class StartClientService
    {
        public void StartClient(LibraryLocation location)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Steam.ClientExecPath,
                Arguments = $"-silent \"steam://open/{location.ToString().ToLower()}\"",
                UseShellExecute = true
            });
        }
    }
}