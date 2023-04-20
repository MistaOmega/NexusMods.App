using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.ModInstallers;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Games.Generic.Entities;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.Reshade;

public class ReshadePresetInstaller : IModInstaller
{
    private static readonly HashSet<RelativePath> IgnoreFiles = new[]
        {
            "readme.txt",
            "installation.txt",
            "license.txt"
        }
        .Select(t => t.ToRelativePath())
        .ToHashSet();

    private readonly IDataStore _dataStore;

    public ReshadePresetInstaller(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Priority GetPriority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        var filtered = archiveFiles
            .Where(f => !IgnoreFiles.Contains(f.Key.FileName))
            .ToList();

        // We have to be able to find the game's executable
        if (installation.Game is not AGame)
            return Common.Priority.None;

        // We only support ini files for now
        if (!filtered.All(f => f.Value.FileTypes.Contains(FileType.INI)))
            return Common.Priority.None;

        // Get all the ini data
        var iniData = filtered
            .Select(f => f.Value.AnalysisData
                .OfType<IniAnalysisData>()
                .FirstOrDefault())
            .Where(d => d is not null)
            .Select(d => d!)
            .ToList();

        // All the files must have ini data
        if (iniData.Count != filtered.Count)
            return Common.Priority.None;

        // All the files must have a section that ends with .fx marking them as likely a reshade preset
        if (!iniData.All(f => f.Sections.All(x => x.EndsWith(".fx", StringComparison.CurrentCultureIgnoreCase))))
            return Common.Priority.None;

        return Common.Priority.Low;
    }

    public ValueTask<IEnumerable<Mod>> GetModsAsync(
        GameInstallation gameInstallation,
        Mod baseMod,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(GetMods(baseMod, srcArchiveHash, archiveFiles));
    }

    private IEnumerable<Mod> GetMods(
        Mod baseMod,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {

        var modFiles = archiveFiles
            .Where(kv => !IgnoreFiles.Contains(kv.Key.FileName))
            .Select(kv =>
            {
                var (path, file) = kv;

                return new FromArchive
                {
                    Id = ModFileId.New(),
                    From = new HashRelativePath(srcArchiveHash, path),
                    To = new GamePath(GameFolderType.Game, path.FileName),
                    Hash = file.Hash,
                    Size = file.Size
                };
            });

        yield return baseMod with
        {
            Files = modFiles.ToEntityDictionary(_dataStore)
        };
    }
}
