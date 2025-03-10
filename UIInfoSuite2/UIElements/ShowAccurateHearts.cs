using System;

using Leclair.Stardew.BetterGameMenu;

using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure;

namespace UIInfoSuite2.UIElements;

internal class ShowAccurateHearts : IDisposable
{
#region Properties
  private readonly IModEvents _events;

    // @formatter:off
    private static readonly int[][] _numArray =
    {
      new[] { 1, 1, 0, 1, 1 },
      new[] { 1, 1, 1, 1, 1 },
      new[] { 0, 1, 1, 1, 0 },
      new[] { 0, 0, 1, 0, 0 }
    };
  // @formatter:on
#endregion

#region Lifecycle
  public ShowAccurateHearts(IModEvents events)
  {
    _events = events;
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showAccurateHearts)
  {
    _events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;

    if (showAccurateHearts)
    {
      _events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
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

    DrawHeartFills(socialPage);

    if (Game1.activeClickableMenu is GameMenu gameMenu)
    {
      string hoverText = gameMenu.hoverText;
      IClickableMenu.drawHoverText(Game1.spriteBatch, hoverText, Game1.smallFont);
    }
  }
#endregion

#region Logic
  private static void DrawHeartFills(SocialPage? socialPage)
  {
    if (socialPage == null)
    {
      return;
    }

    int yOffset = 0;

    for (int i = socialPage.slotPosition; i < socialPage.slotPosition + 5 && i < socialPage.SocialEntries.Count; ++i)
    {
      string internalName = socialPage.SocialEntries[i].InternalName;
      if (Game1.player.friendshipData.TryGetValue(internalName, out Friendship friendshipValues) &&
          friendshipValues.Points > 0 &&
          friendshipValues.Points <
          Utility.GetMaximumHeartsForCharacter(Game1.getCharacterFromName(internalName)) * 250)
      {
        int pointsToNextHeart = friendshipValues.Points % 250;
        int numHearts = friendshipValues.Points / 250;
        int yPosition = Game1.activeClickableMenu.yPositionOnScreen + 130 + yOffset;
        DrawEachIndividualSquare(numHearts, pointsToNextHeart, yPosition);
      }

      yOffset += 112;
    }
  }

  private static void DrawEachIndividualSquare(int friendshipLevel, int friendshipPoints, int yPosition)
  {
    int numberOfPointsToDraw = (int)(friendshipPoints / 12.5);
    int num2;

    if (friendshipLevel >= 10)
    {
      num2 = 32 * (friendshipLevel - 10);
      yPosition += 28;
    }
    else
    {
      num2 = 32 * friendshipLevel;
    }

    for (int i = 3; i >= 0 && numberOfPointsToDraw > 0; --i)
    {
      for (int j = 0; j < 5 && numberOfPointsToDraw > 0; ++j, --numberOfPointsToDraw)
      {
        if (_numArray[i][j] == 1)
        {
          Game1.spriteBatch.Draw(
            Game1.staminaRect,
            new Rectangle(
              Game1.activeClickableMenu.xPositionOnScreen + 320 + num2 + j * 4,
              yPosition + 14 + i * 4,
              4,
              4
            ),
            Color.Crimson
          );
        }
      }
    }
  }
#endregion
}
