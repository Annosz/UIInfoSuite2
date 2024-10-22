using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace UIInfoSuite2.UIElements.ExperienceElements;

public class DisplayedExperienceBar
{
  private const int MaxBarWidth = 235;

  public void Draw(
    Color experienceFillColor,
    Color textColor,
    Rectangle experienceIconPosition,
    int experienceEarnedThisLevel,
    int experienceDifferenceBetweenLevels,
    int currentLevel
  )
  {
    int barWidth = GetBarWidth(experienceEarnedThisLevel, experienceDifferenceBetweenLevels);
    float leftSide = GetExperienceBarLeftSide();

    Game1.drawDialogueBox(
      (int)leftSide,
      Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 160,
      300,
      160,
      false,
      true
    );

    Game1.spriteBatch.Draw(
      Game1.staminaRect,
      new Rectangle((int)leftSide + 32, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63, barWidth, 31),
      experienceFillColor
    );

    Game1.spriteBatch.Draw(
      Game1.staminaRect,
      new Rectangle(
        (int)leftSide + 32,
        Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63,
        Math.Min(4, barWidth),
        31
      ),
      experienceFillColor
    );

    Game1.spriteBatch.Draw(
      Game1.staminaRect,
      new Rectangle((int)leftSide + 32, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63, barWidth, 4),
      experienceFillColor
    );

    Game1.spriteBatch.Draw(
      Game1.staminaRect,
      new Rectangle((int)leftSide + 32, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 36, barWidth, 4),
      experienceFillColor
    );

      if (IsMouseOverExperienceBar(leftSide))
    {
      Game1.drawWithBorder(
        experienceEarnedThisLevel + "/" + experienceDifferenceBetweenLevels,
        textColor,
        textColor,
        new Vector2(leftSide + 33, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 70)
      );
    }
    else
    {
      String levelNumber = "";
      int masteryMargin = 0;
      if (Game1.player.Level < 25)
      {
        Game1.spriteBatch.Draw(
          Game1.mouseCursors,
          new Vector2(leftSide + 54, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 62),
          experienceIconPosition,
          Color.White,
          0,
          Vector2.Zero,
          2.9f,
          SpriteEffects.None,
          0.85f
        );
        levelNumber = currentLevel.ToString();
      }
      else
      {
        Game1.spriteBatch.Draw(
          Game1.mouseCursors_1_6,
          new Vector2(leftSide + 38, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 62),
          new Rectangle(458,299,10,10),
          Color.White,
          0,
          Vector2.Zero,
          2.9f,
          SpriteEffects.None,
          0.85f
        );

        levelNumber = MasteryTrackerMenu.getCurrentMasteryLevel().ToString();
        masteryMargin = 37;
      }

      Game1.drawWithBorder(
        levelNumber,
        textColor * 0.6f,
        textColor,
        new Vector2(leftSide + 33 + masteryMargin, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 70)
      );
    }
  }

#region Static helpers
  private static int GetBarWidth(int experienceEarnedThisLevel, int experienceDifferenceBetweenLevels)
  {
    return (int)((double)experienceEarnedThisLevel / experienceDifferenceBetweenLevels * MaxBarWidth);
  }

  private static float GetExperienceBarLeftSide()
  {
    float leftSide = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Left;

    if (Game1.isOutdoorMapSmallerThanViewport())
    {
      int num3 = Game1.currentLocation.map.Layers[0].LayerWidth * Game1.tileSize;
      leftSide += (Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right - num3) / 2;
    }

    return leftSide;
  }

  private static bool IsMouseOverExperienceBar(float leftSide)
  {
    return GetExperienceBarTextureComponent(leftSide).containsPoint(Game1.getMouseX(), Game1.getMouseY());
  }

  private static ClickableTextureComponent GetExperienceBarTextureComponent(float leftSide)
  {
    return new ClickableTextureComponent(
      "",
      new Rectangle((int)leftSide - 36, Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 80, 260, 100),
      "",
      "",
      Game1.mouseCursors,
      new Rectangle(0, 0, 0, 0),
      Game1.pixelZoom
    );
  }
#endregion
}
