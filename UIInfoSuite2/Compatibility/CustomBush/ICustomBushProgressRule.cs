namespace UIInfoSuite2.Compatibility.CustomBush;

/// <summary>Represents rules for a custom bush to progress into a different stage.</summary>
public interface ICustomBushProgressRule
{
  /// <summary>A game state query which determines whether the bush should grow to the indicated stage.</summary>
  public string? Condition { get; }

  /// <summary>Gets a unique ID for this entry within the current list.</summary>
  public string? Id { get; }

  /// <summary>Gets the items dropped when the conditions for this rule are met.</summary>
  public ICustomBushDrops ItemsDropped { get; }

  /// <summary>Gets the stage that the bush should grow to.</summary>
  public string StageId { get; }

  /// <summary>Get the stage counter condition for this rule.</summary>
  /// <returns>Returns the stage counter.</returns>
  public int GetStageCounter();
}
