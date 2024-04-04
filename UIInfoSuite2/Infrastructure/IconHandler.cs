using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace UIInfoSuite2.Infrastructure;

public sealed class IconHandler
{
  private readonly PerScreen<int> _amountOfVisibleIcons = new();

  private IconHandler() { }

  public static IconHandler Handler { get; } = new();

  public Point GetNewIconPosition()
  {
    int yPos = Game1.options.zoomButtons ? 290 : 260;
    int xPosition = Tools.GetWidthInPlayArea() - 70 - 48 * _amountOfVisibleIcons.Value;
    if (Game1.player.questLog.Any() || Game1.player.team.specialOrders.Any())
    {
      xPosition -= 65;
    }

    ++_amountOfVisibleIcons.Value;
    return new Point(xPosition, yPos);
  }

  public void Reset(object? sender, EventArgs e)
  {
    _amountOfVisibleIcons.Value = 0;
  }
}
