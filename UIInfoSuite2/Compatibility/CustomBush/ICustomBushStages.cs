using System.Collections.Generic;

namespace UIInfoSuite2.Compatibility.CustomBush;

/// <inheritdoc />
public interface ICustomBushStages : IDictionary<string, ICustomBushStage>
{
  /// <summary>Try to get sequential stages starting from an initial stage.</summary>
  /// <returns>Stages that go from initial to last without any repeated elements.</returns>
  public IEnumerable<(ICustomBushStage Stage, string Id, int Counter)> GetSequentialStages();
}
