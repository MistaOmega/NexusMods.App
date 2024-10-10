using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using TransparentValueObjects;
namespace NexusMods.Abstractions.NexusWebApi.Types;

/// <summary>
/// Machine friendly name for the game, should be devoid of special characters
/// that may conflict with URLs or file paths.
///
/// Sometimes used as a Game ID.
/// </summary>
/// <remarks>
///    Usually we match these with NexusMods' URLs.
/// </remarks>
[ValueObject<string>]
public readonly partial struct GameDomain : IAugmentWith<DefaultValueAugment, JsonAugment, DefaultEqualityComparerAugment>
{
    /// <inheritdoc/>
    public static GameDomain DefaultValue { get; } = From("Unknown");

    /// <inheritdoc/>
    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer { get; } = StringComparer.OrdinalIgnoreCase;
}

/// <summary>
/// MnemonicDB attribute for the GameDomain type.
/// </summary>
public class GameDomainAttribute(string ns, string name) : ScalarAttribute<GameDomain, string>(ValueTags.Ascii, ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(GameDomain value)
    {
        return value.Value;
    }

    /// <inheritdoc />
    protected override GameDomain FromLowLevel(string value, ValueTags tag, AttributeResolver resolver)
    {
        return GameDomain.From(value);
    }
}
