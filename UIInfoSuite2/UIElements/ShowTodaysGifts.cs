using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Linq;
using System.Reflection;

namespace UIInfoSuite2.UIElements
{
    internal class ShowTodaysGifts : IDisposable
    {
        #region Properties
        private SocialPage _socialPage;
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
            _helper.Events.Display.MenuChanged -= OnMenuChanged;
            _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;

            if (showTodaysGift)
            {
                _helper.Events.Display.MenuChanged += OnMenuChanged;
                _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            }
        }
        #endregion

        #region Event subscriptions
        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (_socialPage == null)
            {
                ExtendMenuIfNeeded();
                return;
            }

            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 2)
            {
                DrawTodaysGifts();

                string hoverText = gameMenu.hoverText;
                IClickableMenu.drawHoverText(
                    Game1.spriteBatch,
                    hoverText,
                    Game1.smallFont);
            }
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            ExtendMenuIfNeeded();
        }
        #endregion

        #region Logic
        private void ExtendMenuIfNeeded()
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu)
            {
                foreach (var menu in gameMenu.pages)
                {
                    if (menu is SocialPage page)
                    {
                        _socialPage = page;
                        break;
                    }
                }
            }
        }

        private void DrawTodaysGifts()
        {
            int slotPosition = (int)typeof(SocialPage)
                                .GetField(
                                    "slotPosition",
                                    BindingFlags.Instance | BindingFlags.NonPublic)
                                    .GetValue(_socialPage);
            int yOffset = 25;

            for (int i = slotPosition; i < slotPosition + 5 && i < _socialPage.SocialEntries.Count; ++i)
            {
                int yPosition = Game1.activeClickableMenu.yPositionOnScreen + 130 + yOffset;
                yOffset += 112;

                if (_socialPage.GetSocialEntry(i).Friendship != null && _socialPage.GetSocialEntry(i).Friendship.GiftsToday != 0  && _socialPage.GetSocialEntry(i).Friendship.GiftsThisWeek < 2)
                {
                    Game1.spriteBatch.Draw(
                        Game1.mouseCursors,
                        new Vector2(_socialPage.xPositionOnScreen + 384 + 296 + 4, yPosition + 6),
                        new Rectangle?(new Rectangle(106, 442, 9, 9)),
                        Color.LightGray, 0.0f, Vector2.Zero, 3f, SpriteEffects.None, 0.22f
                    );
                }
            }
        }
        #endregion
    }
}
