using System.Collections.Generic;

namespace UIInfoSuite2.Compatibility.CustomBush;

/// <inheritdoc />
public interface ICustomBushProgressRules : IList<ICustomBushProgressRule>
{
  /// <summary>Try to get the minimum stage counter for any condition with the given stage id.</summary>
  /// <param name="id">The stage id, or any.</param>
  /// <returns>Returns the stage counter or -1 if the stage id cannot be reached.</returns>
  public int GetCounterTo(string? id);
}
