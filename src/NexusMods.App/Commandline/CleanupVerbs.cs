using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.DataModel;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.App.Commandline;

/// <summary>
///     These verbs are used in cleaning up unneeded data related to the application.
///     Namely, Garbage Collection tasks.
/// </summary>
internal static class CleanupVerbs
{
    // ReSharper disable once UnusedMethodReturnValue.Global
    internal static IServiceCollection AddCleanupVerbs(this IServiceCollection services) =>
        services.AddVerb(() => UninstallApp);
    
    [Verb("uninstall-app", "Uninstall the application and revert games to their original state")]
    private static async Task<int> UninstallApp(
        [Injected] IRenderer renderer,
        [Injected] ILoadoutRegistry loadoutRegistry,
        [Injected] ISettingsManager settingsManager,
        [Injected] IFileSystem fileSystem)
    {
        var dataModelSettings = settingsManager.Get<DataModelSettings>();
        var fileExtractorSettings = settingsManager.Get<FileExtractorSettings>();
        var loggingSettings = settingsManager.Get<LoggingSettings>();

        // Step 1: Revert the managed games to their original state
        var managedInstallations = loadoutRegistry.AllLoadouts()
            .Select(loadout => loadout.Installation)
            .Distinct();

        foreach (var installation in managedInstallations)
        {
            try
            {
                var synchronizer = installation.GetGame().Synchronizer;
                await synchronizer.UnManage(installation);
                await renderer.Text($"Reverted {installation.Game.Name} to its original state");
            }
            catch (Exception ex)
            {
                await renderer.Error(ex, "Error reverting {0}: {1}", installation.Game.Name, ex.Message);
            }
        }

        // Step 2: Delete application-specific directories
        try
        {
            /*
                Note (Sewer): There's a possibility some data may be left behind if
                the user manually modified the logging configuration to point
                their logs outside of the regular 'Logs' folder.
                
                Specifically, the historical logs may use Layout Renderers
                https://nlog-project.org/config/?tab=layout-renderers that
                are other than {##}. Resolving these on our end would be hard.
            */
            var appFiles = (AbsolutePath[]) [ 
                dataModelSettings.DataStoreFilePath.ToPath(fileSystem),
                loggingSettings.MainProcessLogFilePath.ToPath(fileSystem),
                loggingSettings.SlimProcessLogFilePath.ToPath(fileSystem),
            ];

            foreach (var appFile in appFiles)
            {
                if (!appFile.FileExists)
                    continue;

                try
                {
                    appFile.Delete();
                    await renderer.Text("Deleted file: {0}", appFile);
                }
                catch (Exception e)
                {
                    await renderer.Error(e, "Failed to delete file: {0}", appFile); 
                }
            }

            var appDirectories = new[]
            {
                dataModelSettings.MnemonicDBPath.ToPath(fileSystem),
                fileExtractorSettings.TempFolderLocation.ToPath(fileSystem),
                LoggingSettings.GetLogBaseFolder(OSInformation.Shared, fileSystem)
            }.Concat(dataModelSettings.ArchiveLocations.Select(path => path.ToPath(fileSystem)));

            foreach (var directory in appDirectories)
            {
                if (!directory.DirectoryExists())
                    continue;

                directory.Delete();
                await renderer.Text("Deleted directory: {0}", directory);
            }

            await renderer.Text("Application uninstalled successfully");
            return 0;
        }
        catch (Exception ex)
        {
            await renderer.Error(ex, $"Error deleting application directories: {ex.Message}");
            return -1;
        }
    }
}
