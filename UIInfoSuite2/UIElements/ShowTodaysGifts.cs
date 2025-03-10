using System;
using Leclair.Stardew.BetterGameMenu;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure;

namespace UIInfoSuite2.UIElements;

internal class ShowTodaysGifts : IDisposable
{
#region Properties
  private readonly IModHelper _helper;
#endregion

#region Lifecycle
  public ShowTodaysGifts(IModHelper helper)
  {
    _helper = helper;
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showTodaysGift)
  {
    _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;

    if (showTodaysGift)
    {
      _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
    }
  }
#endregion

#region Event subscriptions
  private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
  {
    if (Tools.GetCurrentMenuPage() is not SocialPage socialPage)
    {
      return;
    }

    DrawTodaysGifts(socialPage);

    if (Game1.activeClickableMenu is GameMenu gameMenu)
    {
      string hoverText = gameMenu.hoverText;
      IClickableMenu.drawHoverText(Game1.spriteBatch, hoverText, Game1.smallFont);
    }
  }
  #endregion

  #region Logic
  private static void DrawTodaysGifts(SocialPage? socialPage)
  {
    if (socialPage == null)
    {
      return;
    }

    int yOffset = 25;

    for (int i = socialPage.slotPosition; i < socialPage.slotPosition + 5 && i < socialPage.SocialEntries.Count; ++i)
    {
      int yPosition = Game1.activeClickableMenu.yPositionOnScreen + 130 + yOffset;
      yOffset += 112;
      string internalName = socialPage.SocialEntries[i].InternalName;
      if (Game1.player.friendshipData.TryGetValue(internalName, out Friendship? data) &&
          data.GiftsToday != 0 &&
          data.GiftsThisWeek < 2)
      {
        Game1.spriteBatch.Draw(
          Game1.mouseCursors,
          new Vector2(socialPage.xPositionOnScreen + 384 + 296 + 4, yPosition + 6),
          new Rectangle(106, 442, 9, 9),
          Color.LightGray,
          0.0f,
          Vector2.Zero,
          3f,
          SpriteEffects.None,
          0.22f
        );
      }
    }
  }
#endregion
}
