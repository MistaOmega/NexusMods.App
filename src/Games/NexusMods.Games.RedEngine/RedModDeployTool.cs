using System.Reflection;
using CliWrap;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Games.Generic;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.Paths;
using static NexusMods.Games.RedEngine.Constants;

namespace NexusMods.Games.RedEngine;

public class RedModDeployTool : ITool
{
    private readonly GameToolRunner _toolRunner;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly ILogger _logger;

    public RedModDeployTool(GameToolRunner toolRunner, TemporaryFileManager temporaryFileManager, ILogger<RedModDeployTool> logger)
    {
        _logger = logger;
        _toolRunner = toolRunner;
        _temporaryFileManager = temporaryFileManager;
    }
    
    public IEnumerable<GameId> GameIds => [Cyberpunk2077.Cyberpunk2077Game.GameIdStatic];

    public async Task Execute(Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        var exe = RedModPath.CombineChecked(loadout.InstallationInstance);
        var deployFolder = RedModDeployFolder.CombineChecked(loadout.InstallationInstance);

        await using var loadorderFile = _temporaryFileManager.CreateFile();
        await WriteLoadorderFile(loadorderFile.Path, loadout);
        
        // RedMod deploys to this folder and freaks out if it doesn't exist, but our synchronizer will
        // delete it if it's empty, so we need to recreate it here.
        if (!deployFolder.DirectoryExists())
            deployFolder.CreateDirectory();

        var fs = FileSystem.Shared;
        if (fs.OS.IsWindows)
        {
            var command = Cli.Wrap(exe.ToString())
                .WithArguments(["deploy", "--modlist=" + loadorderFile.Path], true)
                .WithWorkingDirectory(exe.Parent.ToString());
            await _toolRunner.ExecuteAsync(loadout, command, true, cancellationToken);
        }
        else
        {
            if (loadout.InstallationInstance.LocatorResultMetadata is SteamLocatorResultMetadata)
            {
                await using var batchPath = await ExtractTemporaryDeployScript();
                await _toolRunner.ExecuteAsync(loadout, Cli.Wrap(batchPath.ToString()), true, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Skip running redmod, it's only supported for Steam on Linux at the moment");
            }
        }
    }

    internal async Task WriteLoadorderFile(AbsolutePath loadorderFilePath, Loadout.ReadOnly loadout)
    {
        // The process is as follows:
        // 1. Get all redModSortable items for the loadouts
        // 2. Remove all remods that are not enabled (or parent is disabled)
        // 3. Sort by the sort index
        // 4. Write the redmod folder names to the loadorder file using
        
        var conn = loadout.Db.Connection;

        var loadOrder = RedModLoadOrder.All(conn.Db)
            .FirstOrOptional(g => g.AsLoadOrder().LoadoutId == loadout.LoadoutId);

        if (!loadOrder.HasValue)
        {
            // Write an empty file
            await loadorderFilePath.WriteAllLinesAsync([""]);
            return;
        }
        
        var redMods = RedModLoadoutGroup.All(conn.Db)
            .Where(g => g.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadout.LoadoutId)
            .Where(RedModIsEnabled)
            .Select(g => RedModFolder(g).Name.ToString());

        var order = RedModSortableItemModel.All(conn.Db)
            // Get all sortable items
            .Where(g => g.AsSortableItemModel().ParentLoadOrder.LoadOrderId == loadOrder.Value.AsLoadOrder().LoadOrderId)
            // Only want the enabled redmods
            .Where(g => redMods.FirstOrOptional(m => m == g.RedModFolderName).HasValue)
            // Order by sort index
            .OrderBy(g => g.AsSortableItemModel().SortIndex)
            // Get the folder filename, so `My Mod` instead of `mod\My Mod\info.json`
            .Select(g => g.RedModFolderName);
        
        await loadorderFilePath.WriteAllLinesAsync(order);
    }

    internal static RelativePath RedModFolder(RedModLoadoutGroup.ReadOnly group)
    {
        var redModInfoFile = group.RedModInfoFile.AsLoadoutFile().AsLoadoutItemWithTargetPath().TargetPath.Item3;
        return redModInfoFile.Parent.FileName;
    }

    /// <summary>
    /// Returns true if the file and all its parents are not disabled.
    /// </summary>
    private static bool RedModIsEnabled(RedModLoadoutGroup.ReadOnly grp)
    {
        return !grp.AsLoadoutItemGroup().AsLoadoutItem().GetThisAndParents().Any(f => f.Contains(LoadoutItem.Disabled));
    }

    public string Name => "RedMod Deploy";

    private async Task<TemporaryPath> ExtractTemporaryDeployScript()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "deploy_redmod.bat";

        await using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException($"Resource {resourceName} not found in assembly.");

        using var reader = new StreamReader(stream);
        var file = _temporaryFileManager.CreateFile((Extension?)".bat");
        await file.Path.WriteAllTextAsync(await reader.ReadToEndAsync());
        return file;
    }
}
