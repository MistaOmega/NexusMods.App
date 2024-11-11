using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// An abstraction for a sortable item that can be moved around in a list relative to its siblings.
/// All items in the list will have a non-gaming sort index. If a item is moved the other items will
/// adjust to compensate for the positional change.
/// </summary>
public interface ISortableItem : IComparable<ISortableItem>
{
    /// <summary>
    /// Reference to the provider that manages this item
    /// </summary>
    public ILoadoutSortableItemProvider SortableItemProvider { get; }
    
    /// <summary>
    /// The index of the item in a sorted list of item as given by the provider
    /// </summary>
    public int SortIndex { get; set; }
    
    /// <summary>
    /// Name of the item for display purposes
    /// </summary>
    public string DisplayName { get; }
    
    /// <summary>
    /// The name of the winning mod containing the item
    /// </summary>
    public string ModName { get; set; }
    
    /// <summary>
    /// Represents whether the item is active in the load order or not
    /// An item is considered active if it is part of the load order and will be loaded by the game
    /// An item is considered inactive if it is for some reason not going to be loaded by the game,
    /// e.g. it is disabled in the load order, or parent mod is disabled.
    /// </summary>
    public bool IsActive { get; set; }
    
}
