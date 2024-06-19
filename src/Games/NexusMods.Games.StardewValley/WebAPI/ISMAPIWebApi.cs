using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Paths;
using StardewModdingAPI;

namespace NexusMods.Games.StardewValley.WebAPI;

/// <summary>
/// Interface for working with the SMAPI Web API.
/// </summary>
[PublicAPI]
public interface ISMAPIWebApi : IDisposable
{
    /// <summary>
    /// Fetches details for mods using their IDs.
    /// </summary>
    public Task<IReadOnlyDictionary<string, SMAPIWebApiMod>> GetModDetails(
        IOSInformation os,
        ISemanticVersion gameVersion,
        ISemanticVersion smapiVersion,
        string[] smapiIDs
    );
}

public record SMAPIWebApiMod
{
    public required string UniqueId { get; init; }

    public required string? Name { get; init; }

    public required Optional<NamedLink> NexusModsLink { get; init; }
}
