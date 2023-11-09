using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Linq;
using System.Reflection;

namespace UIInfoSuite2.UIElements
{
    internal class ShowAccurateHearts : IDisposable
    {
        #region Properties
        private SocialPage _socialPage;
        private IModEvents _events;

        private readonly int[][] _numArray = new int[][]
        {
            new int[] { 1, 1, 0, 1, 1 },
            new int[] { 1, 1, 1, 1, 1 },
            new int[] { 0, 1, 1, 1, 0 },
            new int[] { 0, 0, 1, 0, 0 }
        };
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
            _events.Display.MenuChanged -= OnMenuChanged;
            _events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;

            if (showAccurateHearts)
            {
                _events.Display.MenuChanged += OnMenuChanged;
                _events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
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
                DrawHeartFills();

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

        private void DrawHeartFills()
        {
            int slotPosition = (int)typeof(SocialPage)
                .GetField(
                "slotPosition",
                BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_socialPage);
            int yOffset = 0;

            for (int i = slotPosition; i < slotPosition + 5 && i < _socialPage.SocialEntries.Count; ++i)
            {
                if (Game1.player.friendshipData.TryGetValue(_socialPage.GetSocialEntry(i).InternalName, out Friendship friendshipValues)
                    && friendshipValues.Points > 0
                    && friendshipValues.Points < Utility.GetMaximumHeartsForCharacter(Game1.getCharacterFromName(_socialPage.GetSocialEntry(i).InternalName)) * 250)
                {
                    int pointsToNextHeart = friendshipValues.Points % 250;
                    int numHearts = friendshipValues.Points / 250;
                    int yPosition = Game1.activeClickableMenu.yPositionOnScreen + 130 + yOffset;
                    DrawEachIndividualSquare(numHearts, pointsToNextHeart, yPosition);
                }

                yOffset += 112;
            }
        }

        private void DrawEachIndividualSquare(int friendshipLevel, int friendshipPoints, int yPosition)
        {
            int numberOfPointsToDraw = (int)((friendshipPoints) / 12.5);
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
                                4),
                            Color.Crimson);
                    }
                }
            }
        }
        #endregion
    }
}
