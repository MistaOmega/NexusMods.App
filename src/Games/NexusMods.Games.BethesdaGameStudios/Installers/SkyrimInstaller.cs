using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Games.BethesdaGameStudios.Installers;

/// <summary>
/// Installs Skyrim files; both plugins and loose files.
/// </summary>
/// <remarks>
///    This installer allows mods which use the following formats (where {Root} is archive root):
///    - {Root}/{AnyFolderName}
///    - {Root}/Data
///    - {Root}
///
///    Inside these folders, you can either have .esp, .esm, .esl or 'meshes' and 'textures' folders.
///    {AnyFolderName} allows only 1 level of nesting.
/// </remarks>
public class SkyrimInstaller : IModInstaller
{
    private const string MeshesFolderName = "meshes";
    private const string TexturesFolderName = "textures";
    private const string MeshExtension = ".nif";
    private const string TextureExtension = ".dds";
    private const string EspExtension = ".esp";
    private const string EslExtension = ".esl";
    private const string EsmExtension = ".esm";
    private const string BsaExtension = ".bsa";

    private static readonly RelativePath Data = "Data".ToRelativePath();

    private readonly IDataStore _dataStore;

    public SkyrimInstaller(IDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Priority GetPriority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> archiveFiles)
    {
        if (installation.Game is not SkyrimSpecialEdition)
            return Common.Priority.None;

        // Determine one of the following:
        // - There is a 'meshes' folder with NIF file.
        // - There is a 'textures' folder with DDS file.
        // - There is a folder (max 1 level deep) with either of the following cases.
        foreach (var file in archiveFiles)
        {
            var path = file.Key;
            var rawPath = file.Key.Path;

            // Check in subfolder first
            var separatorIndex = path.GetFirstDirectorySeparatorIndex(out _);
            if (separatorIndex != -1 && (separatorIndex + 1) < rawPath.Length)
            {
                var subDirectory = rawPath.AsSpan(separatorIndex + 1);
                if (AssertPathForPriority(subDirectory))
                    return Common.Priority.Normal;
            }

            if (AssertPathForPriority(rawPath))
                return Common.Priority.Normal;
        }

        return Common.Priority.None;
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
        var prefix = FindFolderPrefixForExtract(archiveFiles).ToString();
        var modFiles = archiveFiles
            .Where(kv => prefix.Length <= 0 || kv.Key.StartsWith(prefix))
            .Select(kv =>
            {
                var (path, file) = kv;
                var trimmedPath = path.Path.AsSpan(prefix.Length);

                return new FromArchive
                {
                    Id = ModFileId.New(),
                    From = new HashRelativePath(srcArchiveHash, path),
                    To = new GamePath(GameFolderType.Game, Data.Join(trimmedPath.ToString())),
                    Hash = file.Hash,
                    Size = file.Size
                };
            });

        yield return baseMod with
        {
            Files = modFiles.ToEntityDictionary(_dataStore)
        };
    }

    private static ReadOnlySpan<char> FindFolderPrefixForExtract(EntityDictionary<RelativePath, AnalyzedFile> files)
    {
        // Note: We already determined in priority testing that this is something
        // we want to extract, so just find the folder with meshes/textures subfolders
        // and roll with that.

        // Normally we could cache this stuff between priority and extract but there's
        // no guarantee functions will be ran in that order and another file wouldn't be
        // checked somewhere in the middle for example.
        foreach (var file in files)
        {
            var path = file.Key;
            var rawPath = file.Key.Path;

            // Check in subfolder first
            var separatorIndex = path.GetFirstDirectorySeparatorIndex(out _);
            if (separatorIndex != -1 && (separatorIndex + 1) < rawPath.Length)
            {
                var subDirectory = rawPath.AsSpan(separatorIndex + 1);
                if (AssertFolderForExtract(subDirectory))
                    return rawPath.AsSpan(0, separatorIndex);
            }

            if (AssertFolderForExtract(rawPath))
                return "";
        }

        throw new Exception("Possible bug in code, this should never throw.");
    }

    private static bool AssertPathForPriority(ReadOnlySpan<char> relativePath)
    {
        if (relativePath.StartsWith(MeshesFolderName, StringComparison.OrdinalIgnoreCase) &&
            relativePath.EndsWith(MeshExtension, StringComparison.OrdinalIgnoreCase))
            return true;

        if (relativePath.StartsWith(TexturesFolderName, StringComparison.OrdinalIgnoreCase) &&
            relativePath.EndsWith(TextureExtension, StringComparison.OrdinalIgnoreCase))
            return true;

        // Has plugins in current directory.
        if (PathHelpers.PathHasSubdirectory(relativePath)) 
            return false;
        
        if (relativePath.EndsWith(EsmExtension, StringComparison.OrdinalIgnoreCase))
            return true;

        if (relativePath.EndsWith(EspExtension, StringComparison.OrdinalIgnoreCase))
            return true;

        if (relativePath.EndsWith(EslExtension, StringComparison.OrdinalIgnoreCase))
            return true;

        if (relativePath.EndsWith(BsaExtension, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool AssertFolderForExtract(ReadOnlySpan<char> relativePath)
    {
        if (relativePath.StartsWith(MeshesFolderName, StringComparison.OrdinalIgnoreCase))
            return true;

        if (relativePath.StartsWith(TexturesFolderName, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check for plugins, subdirectory and extension check here should be sufficient.
        if (PathHelpers.PathHasSubdirectory(relativePath)) 
            return false;
        
        if (relativePath.EndsWith(EsmExtension, StringComparison.OrdinalIgnoreCase))
            return true;

        if (relativePath.EndsWith(EspExtension, StringComparison.OrdinalIgnoreCase))
            return true;

        if (relativePath.EndsWith(EslExtension, StringComparison.OrdinalIgnoreCase))
            return true;

        if (relativePath.EndsWith(BsaExtension, StringComparison.OrdinalIgnoreCase))
            return true;
        
        return false;
    }
}
