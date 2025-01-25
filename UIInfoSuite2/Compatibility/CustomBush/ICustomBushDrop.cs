using StardewValley.GameData;

namespace UIInfoSuite2.Compatibility.CustomBush;

/// <summary>Model used for drops from custom bushes.</summary>
public interface ICustomBushDrop : ISpawnItemData
{
  /// <summary>A game state query which indicates whether the item should be added. Defaults to always added.</summary>
  public string? Condition { get; }

  /// <summary>Gets a unique ID for this entry within the current list.</summary>
  public string? Id { get; }

  /// <summary>Gets a value indicating whether the drop can replace an existing item.</summary>
  public bool ReplaceItem { get; }

  /// <summary>Gets an offset to the bush sprite when this item is produced.</summary>
  public int SpriteOffset { get; }

  /// <summary>Try to get the chance for the drop based on its condition.</summary>
  /// <returns>Returns the chance of the drop being produced.</returns>
  public float GetChance();

  /// <summary>Try to get the earliest day for the drop based on its condition.</summary>
  /// <returns>Returns the first day of the drop being produced.</returns>
  public int GetDay();
}
