using System.Net;
using FluentAssertions;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Networking.NexusWebApi.Tests;

[Trait("RequiresNetworking", "True")]
public class NexusApiClientTests
{
    private readonly NexusApiClient _nexusApiClient;

    public NexusApiClientTests(NexusApiClient nexusApiClient)
    {
        _nexusApiClient = nexusApiClient;
    }

    [Fact]
    public async Task CanGetCollectionDownloadLinks()
    {
        var links = await _nexusApiClient.CollectionDownloadLinksAsync(CollectionSlug.From("iszwwe"), RevisionNumber.From(469));
        links.Data.DownloadLinks.Should().HaveCountGreaterThan(0);
    }
}
