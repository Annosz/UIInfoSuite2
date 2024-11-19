using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure;

namespace UIInfoSuite2.UIElements;

internal class LuckOfDay : IDisposable
{
#region Properties
  private readonly PerScreen<string> _hoverText = new(() => string.Empty);
  private readonly PerScreen<Color> _color = new(() => new Color(Color.White.ToVector4()));

  private readonly PerScreen<ClickableTextureComponent> _icon = new(
    () => new ClickableTextureComponent(
      "",
      new Rectangle(Tools.GetWidthInPlayArea() - 134, 290, 10 * Game1.pixelZoom, 10 * Game1.pixelZoom),
      "",
      "",
      Game1.mouseCursors,
      new Rectangle(50, 428, 10, 14),
      Game1.pixelZoom
    )
  );

  private readonly IModHelper _helper;

  private bool Enabled { get; set; }
  private bool ShowExactValue { get; set; }

  private static readonly Color Luck1Color = new(87, 255, 106, 255);
  private static readonly Color Luck2Color = new(148, 255, 210, 255);
  private static readonly Color Luck3Color = new(246, 255, 145, 255);
  private static readonly Color Luck4Color = new(255, 255, 255, 255);
  private static readonly Color Luck5Color = new(255, 155, 155, 255);
  private static readonly Color Luck6Color = new(165, 165, 165, 204);
#endregion

#region Lifecycle
  public LuckOfDay(IModHelper helper)
  {
    _helper = helper;
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showLuckOfDay)
  {
    Enabled = showLuckOfDay;

    _helper.Events.Player.Warped -= OnWarped;
    _helper.Events.Display.RenderingHud -= OnRenderingHud;
    _helper.Events.Display.RenderedHud -= OnRenderedHud;
    _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

    if (showLuckOfDay)
    {
      AdjustIconXToBlackBorder();
      _helper.Events.Player.Warped += OnWarped;
      _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
      _helper.Events.Display.RenderingHud += OnRenderingHud;
      _helper.Events.Display.RenderedHud += OnRenderedHud;
    }
  }

  public void ToggleShowExactValueOption(bool showExactValue)
  {
    ShowExactValue = showExactValue;
    ToggleOption(Enabled);
  }
#endregion

#region Event subscriptions
  private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
  {
    CalculateLuck(e);
  }

  private void OnRenderedHud(object sender, RenderedHudEventArgs e)
  {
    // draw hover text
    if (_icon.Value.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
    {
      IClickableMenu.drawHoverText(Game1.spriteBatch, _hoverText.Value, Game1.dialogueFont);
    }
  }

  private void OnRenderingHud(object sender, RenderingHudEventArgs e)
  {
    // draw dice icon
    if (UIElementUtils.IsRenderingNormally())
    {
      Point iconPosition = IconHandler.Handler.GetNewIconPosition();
      ClickableTextureComponent icon = _icon.Value;
      icon.bounds.X = iconPosition.X;
      icon.bounds.Y = iconPosition.Y;
      _icon.Value = icon;
      _icon.Value.draw(Game1.spriteBatch, _color.Value, 1f);
    }
  }
#endregion

#region Logic
  private void CalculateLuck(UpdateTickedEventArgs e)
  {
    if (e.IsMultipleOf(30)) // half second
    {
      switch (Game1.player.DailyLuck)
      {
        // Spirits are very happy (FeelingLucky)
        case var l when l > 0.07:
          _hoverText.Value = I18n.LuckStatus1();
          _color.Value = Luck1Color;
          break;
        // Spirits are in good humor (LuckyButNotTooLucky)
        case var l when l > 0.02 && l <= 0.07:
          _hoverText.Value = I18n.LuckStatus2();
          _color.Value = Luck2Color;

          break;
        // The spirits feel neutral
        case var l when l >= -0.02 && l <= 0.02 && l != 0:
          _hoverText.Value = I18n.LuckStatus3();
          _color.Value = Luck3Color;

          break;
        // The spirits feel absolutely neutral
        case var l when l == 0:
          _hoverText.Value = I18n.LuckStatus4();
          _color.Value = Luck4Color;
          break;
        // The spirits are somewhat annoyed (NotFeelingLuckyAtAll)
        case var l when l >= -0.07 && l < -0.02:
          _hoverText.Value = I18n.LuckStatus5();
          _color.Value = Luck5Color;

          break;
        // The spirits are very displeased (MaybeStayHome)
        case var l when l < -0.07:
          _hoverText.Value = I18n.LuckStatus6();
          _color.Value = Luck6Color;
          break;
      }

      // Rewrite the text, but keep the color
      if (ShowExactValue)
      {
        _hoverText.Value = string.Format(I18n.DailyLuckValue(), Game1.player.DailyLuck.ToString("N3"));
      }
    }
  }

  private void OnWarped(object sender, WarpedEventArgs e)
  {
    // adjust icon X to black border
    if (e.IsLocalPlayer)
    {
      AdjustIconXToBlackBorder();
    }
  }

  private void AdjustIconXToBlackBorder()
  {
    _icon.Value = new ClickableTextureComponent(
      "",
      new Rectangle(Tools.GetWidthInPlayArea() - 134, 290, 10 * Game1.pixelZoom, 10 * Game1.pixelZoom),
      "",
      "",
      Game1.mouseCursors,
      new Rectangle(50, 428, 10, 14),
      Game1.pixelZoom
    );
  }
#endregion
}
