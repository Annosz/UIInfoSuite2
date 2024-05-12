using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIInfoSuite2.Infrastructure;
using static System.Net.Mime.MediaTypeNames;

namespace UIInfoSuite2.UIElements
{
  internal class ShowUncraftedCraftablesLabel : IDisposable
  {

    private readonly IModHelper _helper;
    private readonly PerScreen<CraftingRecipe?> _hoveredRecipe = new();

    public ShowUncraftedCraftablesLabel(IModHelper helper)
    {
      _helper = helper;
    }

    public void ToggleOption(bool showItemHoverInformation)
    {
      _helper.Events.Display.RenderingActiveMenu -= OnRenderingActiveMenu;
      _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;

      if (showItemHoverInformation)
      {
        _helper.Events.Display.RenderingActiveMenu += OnRenderingActiveMenu;
        _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
      }
    }

    public void Dispose()
    {
      ToggleOption(false);
    }

    private void OnRenderingActiveMenu(object? sender, RenderingActiveMenuEventArgs e)
    {
      CraftingRecipe? recipe = Tools.GetHoveredCraftingRecipe();
      if (recipe != null)
      {
        _hoveredRecipe.Value = recipe;
      }
      else
      {
        _hoveredRecipe.Value = null;
      }
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
      DrawToolTip(e.SpriteBatch);
    }

    private void DrawToolTip(SpriteBatch spriteBatch)
    {
      if (Game1.activeClickableMenu != null)
      {
        if (_hoveredRecipe.Value is CraftingRecipe recipe)
        {
          if ((recipe.isCookingRecipe && !Game1.player.recipesCooked.ContainsKey(recipe.getIndexOfMenuView()))
            || (!recipe.isCookingRecipe && !Game1.player.craftingRecipes.ContainsKey(recipe.getIndexOfMenuView()) && recipe.timesCrafted == 0))
          {
            int windowWidth, windowHeight;
            windowHeight = 68;
            windowWidth = 89;

            int windowY = Game1.getMouseY() + 20;
            int windowX = Game1.getMouseX() - 25 - windowWidth;

            Rectangle safeArea = Utility.getSafeArea();

            if (windowY + windowHeight > safeArea.Bottom)
            {
              windowY = safeArea.Bottom - windowHeight;
            }

            if (Game1.getMouseX() + 300 > safeArea.Right)
            {
              windowX = safeArea.Right - 350 - windowWidth;
            }
            else if (windowX < safeArea.Left)
            {
              windowX = Game1.getMouseX() + 350;
            }

            Vector2 windowPos = new Vector2(windowX, windowY);
            Vector2 drawPosition = windowPos + new Vector2(16, 20);

            IClickableMenu.drawTextureBox(
              Game1.spriteBatch,
              Game1.menuTexture,
              new Rectangle(0, 256, 60, 60),
              (int)windowPos.X,
              (int)windowPos.Y,
              windowWidth,
              windowHeight,
              Color.White);

            spriteBatch.DrawString(Game1.smallFont, "New!", drawPosition + new Vector2(2, 2), Game1.textShadowColor);
            spriteBatch.DrawString(Game1.smallFont, "New!", drawPosition, Game1.textColor);
          }
        }
      }
    }
  }
}
