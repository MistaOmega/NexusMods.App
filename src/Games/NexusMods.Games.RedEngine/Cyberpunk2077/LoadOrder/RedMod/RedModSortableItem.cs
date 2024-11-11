using NexusMods.Abstractions.Games;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.LoadOrder;

public class RedModSortableItem : ISortableItem, IComparable<RedModSortableItem>
{
    public RedModSortableItem(RedModSortableItemProvider provider, int sortIndex, string displayName, string modName, bool isActive)
    {
        SortableItemProvider = provider;
        SortIndex = sortIndex;
        DisplayName = displayName;
        ModName = modName;
        IsActive = isActive;
    }

    public ILoadoutSortableItemProvider SortableItemProvider { get; }
    public int SortIndex { get; set; }
    public string DisplayName { get; }
    public string ModName { get; set; }
    public bool IsActive { get; set; }

    public int CompareTo(RedModSortableItem? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return SortIndex.CompareTo(other.SortIndex);
    }

    public int CompareTo(ISortableItem? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return SortIndex.CompareTo(other.SortIndex);
    }
}
