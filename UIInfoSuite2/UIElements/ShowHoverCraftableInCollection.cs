using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIInfoSuite2.Infrastructure;

namespace UIInfoSuite2.UIElements
{
    internal class ShowHoverCraftableInCollection : IDisposable
    {

        private readonly IModHelper _helper;
        private readonly PerScreen<CraftingRecipe> _hoverRecipe = new();

        public ShowHoverCraftableInCollection(IModHelper helper)
        {
            _helper = helper;
        }

        public void ToggleOption(bool showItemHoverInformation)
        {
            //_helper.Events.Display.MenuChanged -= OnMenuChanged;
            _helper.Events.Display.Rendering -= OnRendering;
            _helper.Events.Display.Rendered -= OnRendered;


            if (showItemHoverInformation)
            {
                //_helper.Events.Display.MenuChanged += OnMenuChanged;
                _helper.Events.Display.Rendering += OnRendering;
                _helper.Events.Display.Rendered += OnRendered;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private void OnRendering(object sender, EventArgs e)
        {
            _hoverRecipe.Value = Tools.GetHoveredCraftingRecipe();
        }

        private void OnRendered(object sender, EventArgs e)
        {
            if(Game1.activeClickableMenu != null)
            {
                if(_hoverRecipe.Value != null)
                {
                    var recipe = _hoverRecipe.Value;
                    if (recipe.isCookingRecipe && Game1.player.recipesCooked.Keys.Contains(recipe.getIndexOfMenuView()))
                    {
                        ModEntry.MonitorObject.Log($"Player has cooked {recipe.name}", LogLevel.Debug);
                    }
                }
            }
        }
    }
}