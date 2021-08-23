using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Quests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UIInfoSuite.Infrastructure;
using UIInfoSuite.Infrastructure.Extensions;
using UIInfoSuite.Options;

namespace UIInfoSuite.UIElements
{
    class LocationOfTownsfolk : IDisposable
    {
        #region Properties
        private SocialPage _socialPage;
        private string[] _friendNames;
        private List<NPC> _townsfolk = new List<NPC>();
        private List<OptionsCheckbox> _checkboxes = new List<OptionsCheckbox>();

        private readonly ModOptions _options;
        private readonly IModHelper _helper;

        private const int SocialPanelWidth = 190;
        private const int SocialPanelXOffset = 160;

        private static readonly Dictionary<string, KeyValuePair<int, int>> _mapLocations = new Dictionary<string, KeyValuePair<int, int>>()
        {
            { "HarveyRoom", new KeyValuePair<int, int>(677, 304) },
            { "BathHouse_Pool", new KeyValuePair<int, int>(576, 60) },
            { "WizardHouseBasement", new KeyValuePair<int, int>(196, 352) },
            { "BugLand", new KeyValuePair<int, int>(0, 0) },
            { "Desert", new KeyValuePair<int, int>(75, 40) },
            { "Cellar", new KeyValuePair<int, int>(470, 260) },
            { "JojaMart", new KeyValuePair<int, int>(872, 280) },
            { "LeoTreeHouse", new KeyValuePair<int, int>(744, 128) },
            { "Tent", new KeyValuePair<int, int>(784, 128) },
            { "HaleyHouse", new KeyValuePair<int, int>(652, 408) },
            { "Hospital", new KeyValuePair<int, int>(677, 304) },
            { "FarmHouse", new KeyValuePair<int, int>(470, 260) },
            { "Farm", new KeyValuePair<int, int>(470, 260) },
            { "ScienceHouse", new KeyValuePair<int, int>(732, 148) },
            { "ManorHouse", new KeyValuePair<int, int>(768, 395) },
            { "AdventureGuild", new KeyValuePair<int, int>(0, 0) },
            { "SeedShop", new KeyValuePair<int, int>(696, 296) },
            { "Blacksmith", new KeyValuePair<int, int>(852, 388) },
            { "JoshHouse", new KeyValuePair<int, int>(740, 320) },
            { "SandyHouse", new KeyValuePair<int, int>(40, 115) },
            { "Tunnel", new KeyValuePair<int, int>(0, 0) },
            { "CommunityCenter", new KeyValuePair<int, int>(692, 204) },
            { "Backwoods", new KeyValuePair<int, int>(460, 156) },
            { "ElliottHouse", new KeyValuePair<int, int>(826, 550) },
            { "SebastianRoom", new KeyValuePair<int, int>(732, 148) },
            { "BathHouse_Entry", new KeyValuePair<int, int>(576, 60) },
            { "Greenhouse", new KeyValuePair<int, int>(370, 270) },
            { "Sewer", new KeyValuePair<int, int>(380, 596) },
            { "WizardHouse", new KeyValuePair<int, int>(196, 352) },
            { "Trailer", new KeyValuePair<int, int>(780, 360) },
            { "Trailer_Big", new KeyValuePair<int, int>(780, 360) },
            { "Forest", new KeyValuePair<int, int>(80, 272) },
            { "Woods", new KeyValuePair<int, int>(100, 272) },
            { "WitchSwamp", new KeyValuePair<int, int>(0, 0) },
            { "ArchaeologyHouse", new KeyValuePair<int, int>(892, 416) },
            { "FishShop", new KeyValuePair<int, int>(844, 608) },
            { "Saloon", new KeyValuePair<int, int>(714, 354) },
            { "LeahHouse", new KeyValuePair<int, int>(452, 436) },
            { "Town", new KeyValuePair<int, int>(680, 360) },
            { "Mountain", new KeyValuePair<int, int>(762, 154) },
            { "BusStop", new KeyValuePair<int, int>(516, 224) },
            { "Railroad", new KeyValuePair<int, int>(644, 64) },
            { "SkullCave", new KeyValuePair<int, int>(0, 0) },
            { "BathHouse_WomensLocker", new KeyValuePair<int, int>(576, 60) },
            { "Beach", new KeyValuePair<int, int>(790, 550) },
            { "BathHouse_MensLocker", new KeyValuePair<int, int>(576, 60) },
            { "Mine", new KeyValuePair<int, int>(880, 100) },
            { "WitchHut", new KeyValuePair<int, int>(0, 0) },
            { "AnimalShop", new KeyValuePair<int, int>(420, 392) },
            { "SamHouse", new KeyValuePair<int, int>(612, 396) },
            { "WitchWarpCave", new KeyValuePair<int, int>(0, 0) },
            { "Club", new KeyValuePair<int, int>(60, 92) }
        };
        #endregion

        #region Lifecycle
        public LocationOfTownsfolk(IModHelper helper, ModOptions options)
        {
            _helper = helper;
            _options = options;
        }

        public void ToggleShowNPCLocationsOnMap(bool showLocations)
        {
            InitializeProperties();
            _helper.Events.Display.MenuChanged -= OnMenuChanged;
            _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu_DrawSocialPageOptions;
            _helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu_DrawNPCLocationsOnMap;
            _helper.Events.Input.ButtonPressed -= OnButtonPressed_ForSocialPage;
            _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

            if (showLocations)
            {
                _helper.Events.Display.MenuChanged += OnMenuChanged;
                _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu_DrawSocialPageOptions;
                _helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu_DrawNPCLocationsOnMap;
                _helper.Events.Input.ButtonPressed += OnButtonPressed_ForSocialPage;
                _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }

        public void Dispose()
        {
            ToggleShowNPCLocationsOnMap(false);
        }
        #endregion

        #region Event subscriptions
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            InitializeProperties();
        }

        private void OnButtonPressed_ForSocialPage(object sender, ButtonPressedEventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu && (e.Button == SButton.MouseLeft || e.Button == SButton.ControllerA || e.Button == SButton.ControllerX))
            {
                CheckSelectedBox(e);
            }
        }

        private void OnRenderedActiveMenu_DrawSocialPageOptions(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 2)
            {
                DrawSocialPageOptions();
            }
        }
        private void OnRenderedActiveMenu_DrawNPCLocationsOnMap(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 3)
            {
                DrawNPCLocationsOnMap(gameMenu);
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!e.IsOneSecond || (Context.IsSplitScreen && Context.ScreenId != 0))
                return;

            _townsfolk.Clear();

            foreach (var loc in Game1.locations)
            {
                foreach (var character in loc.characters)
                {
                    if (character.isVillager())
                        _townsfolk.Add(character);
                }
            }
        }
        #endregion

        #region Logic
        private void InitializeProperties()
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu)
            {
                foreach (var menu in gameMenu.pages)
                {
                    if (menu is SocialPage socialPage)
                    {
                        _socialPage = socialPage;
                        _friendNames = _socialPage.names
                            .Select(name => name.ToString())
                            .ToArray();
                        break;
                    }
                }

                _checkboxes.Clear();
                for (int i = 0; i < _friendNames.Length; i++)
                {
                    var friendName = _friendNames[i];
                    OptionsCheckbox checkbox = new OptionsCheckbox("", i);
                    if (Game1.player.friendshipData.ContainsKey(friendName))
                    {
                        // npc
                        checkbox.greyedOut = false;
                        checkbox.isChecked = _options.ShowLocationOfFriends.SafeGet(friendName, true);
                    }
                    else
                    {
                        // player
                        checkbox.greyedOut = true;
                        checkbox.isChecked = true;
                    }
                    _checkboxes.Add(checkbox);
                }
            }
        }

        private void CheckSelectedBox(ButtonPressedEventArgs e)
        {
            int slotPosition = (int)typeof(SocialPage)
                .GetField("slotPosition", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_socialPage);

            for (int i = slotPosition; i < slotPosition + 5; ++i)
            {
                OptionsCheckbox checkbox = _checkboxes[i];
                var rect = new Rectangle(checkbox.bounds.X, checkbox.bounds.Y, checkbox.bounds.Width, checkbox.bounds.Height);
                if(e.Button == SButton.ControllerX)
                {
                    rect.Width += SocialPanelWidth + Game1.activeClickableMenu.width;
                }
                if (rect.Contains((int)Utility.ModifyCoordinateForUIScale(Game1.getMouseX()), (int)Utility.ModifyCoordinateForUIScale(Game1.getMouseY())) &&
                    !checkbox.greyedOut)
                {
                    checkbox.isChecked = !checkbox.isChecked;
                    _options.ShowLocationOfFriends[_friendNames[checkbox.whichOption]] = checkbox.isChecked;
                    Game1.playSound("drumkit6");
                }
            }
        }

        private void DrawSocialPageOptions()
        {
            Game1.drawDialogueBox(Game1.activeClickableMenu.xPositionOnScreen - SocialPanelXOffset, Game1.activeClickableMenu.yPositionOnScreen,
                SocialPanelWidth, Game1.activeClickableMenu.height, false, true);

            int slotPosition = (int)typeof(SocialPage)
                .GetField("slotPosition", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_socialPage);
            int yOffset = 0;

            for (int i = slotPosition; i < slotPosition + 5 && i < _friendNames.Length; ++i)
            {
                OptionsCheckbox checkbox = _checkboxes[i];
                checkbox.bounds.X = Game1.activeClickableMenu.xPositionOnScreen - 60;

                checkbox.bounds.Y = Game1.activeClickableMenu.yPositionOnScreen + 130 + yOffset;

                checkbox.draw(Game1.spriteBatch, 0, 0);
                yOffset += 112;
                Color color = checkbox.isChecked ? Color.White : Color.Gray;

                Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(checkbox.bounds.X - 50, checkbox.bounds.Y), new Rectangle(80, 0, 16, 16),
                    color, 0.0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);

                if (yOffset != 560)
                {
                    // draw seperator line
                    Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(checkbox.bounds.X - 50, checkbox.bounds.Y + 72, SocialPanelWidth / 2 - 6, 4), Color.SaddleBrown);
                    Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(checkbox.bounds.X - 50, checkbox.bounds.Y + 76, SocialPanelWidth / 2 - 6, 4), Color.BurlyWood);
                }
                if (!Game1.options.hardwareCursor)
                {
                    Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.getMouseX(), Game1.getMouseY()),
                        Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, Game1.mouseCursor, 16, 16),
                        Color.White, 0.0f, Vector2.Zero, Game1.pixelZoom + (Game1.dialogueButtonScale / 150.0f), SpriteEffects.None, 1f);
                }

                if (checkbox.bounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
                    IClickableMenu.drawHoverText(Game1.spriteBatch, "Track on map", Game1.dialogueFont);
            }
        }

        private void DrawNPCLocationsOnMap(GameMenu gameMenu)
        {
            List<string> namesToShow = new List<string>();
            foreach (var character in _townsfolk)
            {
                try
                {
                    bool shouldDrawCharacter = Game1.player.friendshipData.ContainsKey(character.Name) && _options.ShowLocationOfFriends.SafeGet(character.Name, true) && _friendNames.Contains(character.Name);
                    if (shouldDrawCharacter)
                    {
                        DrawNPC(character, namesToShow);
                    }
                }
                catch (Exception ex)
                {
                    ModEntry.MonitorObject.Log(ex.Message + Environment.NewLine + ex.StackTrace, LogLevel.Error);
                }
            }
            DrawNPCNames(namesToShow);

            //The cursor needs to show up in front of the character faces
            Tools.DrawMouseCursor();

            string hoverText = (string)typeof(MapPage)
                .GetField("hoverText", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(gameMenu.pages[gameMenu.currentTab]);

            IClickableMenu.drawHoverText(Game1.spriteBatch, hoverText, Game1.smallFont);
        }

        private static void DrawNPC(NPC character, List<string> namesToShow)
        {
            KeyValuePair<int, int> location = GetMapCoordinatesForNPC(character);

            Rectangle headShot = character.GetHeadShot();
            int xBase = Game1.activeClickableMenu.xPositionOnScreen - 158;
            int yBase = Game1.activeClickableMenu.yPositionOnScreen - 40;

            int x = xBase + location.Key;
            int y = yBase + location.Value;

            Color color = character.CurrentDialogue.Count <= 0 ? Color.Gray : Color.White;
            ClickableTextureComponent textureComponent = new ClickableTextureComponent(character.Name, new Rectangle(x, y, 0, 0),
                null, character.Name, character.Sprite.Texture, headShot, 2.3f);

            float headShotScale = 2f;
            Game1.spriteBatch.Draw(character.Sprite.Texture, new Vector2(x, y), new Rectangle?(headShot),
                color, 0.0f, Vector2.Zero, headShotScale, SpriteEffects.None, 1f);

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            if (mouseX >= x && mouseX <= x + headShot.Width * headShotScale && mouseY >= y && mouseY <= y + headShot.Height * headShotScale)
            {
                namesToShow.Add(character.displayName);
            }

            DrawQuestsForNPC(character, x, y);
        }

        private static KeyValuePair<int, int> GetMapCoordinatesForNPC(NPC character)
        {
            string locationName = character.currentLocation?.Name ?? character.DefaultMap;

            // Ginger Island
            if (character.currentLocation is IslandLocation)
            {
                return new KeyValuePair<int, int>(1104, 658);
            }

            // Scale Town and Forest
            if (locationName == "Town" || locationName == "Forest")
            {
                int xStart = locationName == "Town" ? 595 : 183;
                int yStart = locationName == "Town" ? 163 : 378;
                int areaWidth = locationName == "Town" ? 345 : 319;
                int areaHeight = locationName == "Town" ? 330 : 261;

                xTile.Map map = character.currentLocation.Map;

                float xScale = (float)areaWidth / (float)map.DisplayWidth;
                float yScale = (float)areaHeight / (float)map.DisplayHeight;

                float scaledX = character.position.X * xScale;
                float scaledY = character.position.Y * yScale;
                int xPos = (int)scaledX + xStart;
                int yPos = (int)scaledY + yStart;
                return new KeyValuePair<int, int>(xPos, yPos);
            }

            // Other known locations
            return _mapLocations.SafeGet(locationName, new KeyValuePair<int, int>(0, 0));
        }

        private static void DrawQuestsForNPC(NPC character, int x, int y)
        {
            foreach (var quest in Game1.player.questLog.Where(q => q.accepted.Value && q.dailyQuest.Value && ! q.completed.Value))
            {
                bool isQuestTarget = false;
                switch (quest.questType.Value)
                {
                    case 3: isQuestTarget = (quest as ItemDeliveryQuest).target.Value == character.Name; break;
                    case 4: isQuestTarget = (quest as SlayMonsterQuest).target.Value == character.Name; break;
                    case 7: isQuestTarget = (quest as FishingQuest).target.Value == character.Name; break;
                    case 10: isQuestTarget = (quest as ResourceCollectionQuest).target.Value == character.Name; break;
                }

                if (isQuestTarget)
                    Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(x + 10, y - 12), new Rectangle(394, 495, 4, 10),
                        Color.White, 0.0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);
            }
        }

        private static void DrawNPCNames(List<string> namesToShow)
        {
            if (namesToShow.Count == 0)
                return;

            StringBuilder text = new StringBuilder();
            int longestLength = 0;
            foreach (string name in namesToShow)
            {
                text.AppendLine(name);
                longestLength = Math.Max(longestLength, (int)Math.Ceiling(Game1.smallFont.MeasureString(name).Length()));
            }

            int windowHeight = Game1.smallFont.LineSpacing * namesToShow.Count + 25;
            Vector2 windowPos = new Vector2(Game1.getMouseX() + 40, Game1.getMouseY() - windowHeight);
            IClickableMenu.drawTextureBox(Game1.spriteBatch, (int)windowPos.X, (int)windowPos.Y,
                longestLength + 30, Game1.smallFont.LineSpacing * namesToShow.Count + 25, Color.White);

            Game1.spriteBatch.DrawString(Game1.smallFont, text, new Vector2(windowPos.X + 17, windowPos.Y + 17), Game1.textShadowColor);

            Game1.spriteBatch.DrawString(Game1.smallFont, text, new Vector2(windowPos.X + 15, windowPos.Y + 15), Game1.textColor);
        }
        #endregion
    }
}