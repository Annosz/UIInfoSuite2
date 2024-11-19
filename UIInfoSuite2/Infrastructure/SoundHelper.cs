using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewValley;

namespace UIInfoSuite2.Infrastructure;

public enum Sounds
{
  LevelUp
}

public class SoundHelper
{
  private static readonly Lazy<SoundHelper> LazyInstance = new(() => new SoundHelper());
  private bool _initialized;

  private string _modId = "InfoSuite";

  protected SoundHelper() { }

  public static SoundHelper Instance => LazyInstance.Value;

  public void Initialize(IModHelper helper)
  {
    if (_initialized)
    {
      throw new InvalidOperationException("Cannot re-initialize sound helper");
    }

    _modId = helper.ModContent.ModID;

    RegisterSound(helper, Sounds.LevelUp, "LevelUp.wav");

    _initialized = true;
  }

  private string GetQualifiedSoundName(Sounds sound)
  {
    return $"{_modId}.sounds.{sound.ToString()}";
  }

  private static void RegisterSound(
    IModHelper helper,
    Sounds sound,
    string fileName,
    string category = "Sound",
    int instanceLimit = -1,
    CueDefinition.LimitBehavior? limitBehavior = null
  )
  {
    CueDefinition newCueDefinition = new() { name = Instance.GetQualifiedSoundName(sound) };

    if (instanceLimit > 0)
    {
      newCueDefinition.instanceLimit = instanceLimit;
      newCueDefinition.limitBehavior = limitBehavior ?? CueDefinition.LimitBehavior.ReplaceOldest;
    }
    else if (limitBehavior.HasValue)
    {
      newCueDefinition.limitBehavior = limitBehavior.Value;
    }

    SoundEffect audio;
    string filePath = Path.Combine(helper.DirectoryPath, "assets", fileName);
    using (var stream = new FileStream(filePath, FileMode.Open))
    {
      audio = SoundEffect.FromStream(stream);
    }

    newCueDefinition.SetSound(audio, Game1.audioEngine.GetCategoryIndex(category));
    Game1.soundBank.AddCue(newCueDefinition);
    ModEntry.MonitorObject.Log($"Registered Sound: {newCueDefinition.name}");
  }

  public static void Play(Sounds sound)
  {
    Game1.playSound(Instance.GetQualifiedSoundName(sound));
  }
}
