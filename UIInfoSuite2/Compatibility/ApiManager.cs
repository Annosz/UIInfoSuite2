using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI;

namespace UIInfoSuite2.Compatibility;

public static class ModCompat
{
  public const string CustomBush = "furyx639.CustomBush";
  public const string Gmcm = "spacechase0.GenericModConfigMenu";
  public const string DeluxeJournal = "MolsonCAD.DeluxeJournal";
}

public static class ApiManager
{
  private static readonly Dictionary<string, object> RegisteredApis = new();

  public static T? TryRegisterApi<T>(
    IModHelper helper,
    string modId,
    string? minimumVersion = null,
    bool warnIfNotPresent = false
  ) where T : class
  {
    IModInfo? modInfo = helper.ModRegistry.Get(modId);
    if (modInfo == null)
    {
      return null;
    }

    if (minimumVersion != null && modInfo.Manifest.Version.IsOlderThan(minimumVersion))
    {
      ModEntry.MonitorObject.Log(
        $"Requested version {minimumVersion} for mod {modId}, but got {modInfo.Manifest.Version} instead, cannot use API.",
        LogLevel.Warn
      );
      return null;
    }

    var api = helper.ModRegistry.GetApi<T>(modId);
    if (api is null)
    {
      if (warnIfNotPresent)
      {
        ModEntry.MonitorObject.Log($"Could not find API for mod {modId}, but one was requested", LogLevel.Warn);
      }

      return null;
    }

    ModEntry.MonitorObject.Log($"Loaded API for mod {modId}", LogLevel.Info);
    RegisteredApis[modId] = api;
    return api;
  }

  public static bool GetApi<T>(string modId, [NotNullWhen(true)] out T? apiInstance) where T : class
  {
    apiInstance = null;
    if (!RegisteredApis.TryGetValue(modId, out object? api))
    {
      return false;
    }

    if (api is T apiVal)
    {
      apiInstance = apiVal;
      return true;
    }

    ModEntry.MonitorObject.Log(
      $"API was registered for mod {modId} but the requested type is not supported",
      LogLevel.Warn
    );
    return false;
  }
}
