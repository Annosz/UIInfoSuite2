using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using UIInfoSuite2.Infrastructure.Reflection;
using SObject = StardewValley.Object;

namespace UIInfoSuite2.Compatibility
{
    public class DynamicGameAssetsHelper
    {
        public IDynamicGameAssetsApi Api { get; init; }
        private IReflectionHelper Reflection { get; init; }
        private IModEvents ModEvents { get; init; }
        private IMonitor Monitor { get; init; }
        private Reflector Reflector { get; init; }

        private Assembly? _dgaAssembly;

        private DgaFakeIdRetriever? _dgaFakeId;
        private DgaFakeIdRetriever DgaFakeId {
            get => _dgaFakeId ??= new DgaFakeIdRetriever(this);
        }

        private IReflectedMethod? _modFindMethod;

        public DynamicGameAssetsHelper(IDynamicGameAssetsApi api, IModHelper helper, IMonitor monitor)
        {
            this.Api = api;
            this.Reflection = helper.Reflection;
            this.ModEvents = helper.Events;
            this.Monitor = monitor;
            this.Reflector = new Reflector();

            this.ModEvents.GameLoop.DayEnding += OnDayEnding;
        }

        /// <summary>Inject an object of any DGA type to get a reference to the DGA Assembly</summary>
        /// <returns>The initialized <see cref="DynamicGameAssetsHelper"/>.</returns>
        public DynamicGameAssetsHelper InjectDga(object dga)
        {
            if (_dgaAssembly == null)
            {
                _dgaAssembly = dga.GetType().Assembly;
                Monitor.Log($"{this.GetType().Name}: Retrieved reference to DGA assemby using DGA class instance of {dga.GetType().FullName}.", LogLevel.Trace);
            }
            return this;
        }

        public void Dispose()
        {
            this.ModEvents.GameLoop.DayEnding -= this.OnDayEnding;
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            this.Reflector.NewCacheInterval();
        }

        /// Retrieve fake object ids for DGA object using code copy-pasted from DGA.
        /// But it first checks using a roundabout way, that the copy-pasted code is still valid.
        private class DgaFakeIdRetriever
        {
            private DynamicGameAssetsHelper DgaHelper { get; init; }
            private bool? deterministicHashCodeIsCorrect = null;

            public DgaFakeIdRetriever(DynamicGameAssetsHelper dgaHelper)
            {
                this.DgaHelper = dgaHelper;
            }

            public string GetId(SObject dgaItem)
            {   
                if (deterministicHashCodeIsCorrect == null)
                {
                    string hashedId = this.GetIdByDeterministicHashCode(dgaItem);
                    string shippedId = this.GetIdByShippingIt(dgaItem);
                    deterministicHashCodeIsCorrect = (hashedId == shippedId);
                    
                    if ((bool) deterministicHashCodeIsCorrect)
                        DgaHelper.Monitor.Log($"{this.GetType().Name}: The GetDeterministicHashCode implementation seems to be correct", LogLevel.Trace);
                    else
                        DgaHelper.Monitor.Log($"{this.GetType().Name}: The GetDeterministicHashCode implementation seems to be incorrect. Processing DGA items will be slower.", LogLevel.Info);

                    return shippedId;
                }
                else if (deterministicHashCodeIsCorrect == true)
                {
                    return this.GetIdByDeterministicHashCode(dgaItem);
                }
                else
                {
                    return this.GetIdByShippingIt(dgaItem);
                }
            }

            private string GetIdByDeterministicHashCode(SObject dgaItem)
            {
                return this.GetDeterministicHashCode(DgaHelper.GetFullId(dgaItem)!).ToString();
            }

            private string GetIdByShippingIt(SObject dgaItem)
            {
                DgaHelper.Monitor.Log($"{this.GetType().Name}: Retrieving the fake DGA item ID for {dgaItem.Name} by shipping it.", LogLevel.Trace);

                var shippingMenu = new StardewValley.Menus.ShippingMenu(new List<Item>());

                // Record previous state
                uint oldCropsShipped = Game1.stats.CropsShipped;
                var oldBasicShipped = new Dictionary<string, int>(Game1.player.basicShipped.FieldDict.Select(x => KeyValuePair.Create(x.Key, x.Value.Value)));
                
                // Ship the item to observe side-effects
                shippingMenu.parseItems(new List<Item>{ dgaItem });

                // Restore previous state
                Game1.stats.CropsShipped = oldCropsShipped;
                var basicShipped = Game1.player.basicShipped;

                // Find the new item
                List<string> newItems = new();
                foreach (var shipped in basicShipped.Keys)
                {
                    if (oldBasicShipped.TryGetValue(shipped, out int oldValue))
                    {
                        if (oldValue != basicShipped[shipped])
                        {
                            basicShipped[shipped] = oldValue;
                            newItems.Add(shipped);
                        }
                    }
                    else
                    {
                        basicShipped.Remove(shipped);
                        newItems.Add(shipped);
                    }
                }
                if (newItems.Count != 1)
                    throw new Exception($"{newItems.Count} items were shipped when we expected only one");
                
                return newItems[0];
            }

            // Copied from SpaceShared.CommonExtensions
            private int GetDeterministicHashCode(string str)
            {
                unchecked
                {
                    int hash1 = (5381 << 16) + 5381;
                    int hash2 = hash1;

                    for (int i = 0; i < str.Length; i += 2)
                    {
                        hash1 = ((hash1 << 5) + hash1) ^ str[i];
                        if (i == str.Length - 1)
                            break;
                        hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                    }

                    return hash1 + (hash2 * 1566083941);
                }
            }
        }

        /// <summary>Mod.Find()</summary>
        public object? FindPackData(string fullId)
        {
            var modFind = GetModFindMethod();
            if (modFind == null)
                throw new Exception("Could not load DynamicGameAssets.Mod.Find");

            return modFind.Invoke<object?>(fullId);
        }

        public string GetDgaObjectFakeId(SObject dgaItem)
        {
            return DgaFakeId.GetId(dgaItem);
        }

        #region DGA instance fields, methods and properties
        /// <summary>CustomCrop.Data</summary>
        private object? GetCropData(object customCrop)
        {
            return Reflector.GetPropertyGetter<object?>(customCrop, "Data").GetValue();
        }

        /// <summary>IDGAItem.FullId</summary>
        public string? GetFullId(object dgaItem)
        {
            return Reflection.GetProperty<string?>(dgaItem, "FullId").GetValue();
        }
        #endregion

        #region Code reflecting into DGA
        public SObject? GetCropHarvest(object customCrop)
        {
            var cropData = this.GetCropData(customCrop);
            if (cropData == null)
                return null;
            
            return this.GetCropPackHarvest(cropData);
        }

        public SObject? GetSeedsHarvest(Item item)
        {
            if (!(item is StardewValley.Object seedsObject && seedsObject.Category == StardewValley.Object.SeedsCategory))
                return null;

            var itemData = Reflector.GetPropertyGetter<object?>(item, "Data").GetValue();
            if (itemData == null)
                return null;
            
            string? itemPlants = Reflection.GetProperty<string?>(itemData, "Plants").GetValue();
            if (itemPlants == null)
                return null;

            var cropData = this.FindPackData(itemPlants);
            if (cropData == null)
                return null;

            return this.GetCropPackHarvest(cropData);
        }

        private SObject? GetCropPackHarvest(object cropData)
        {
            var cropPhases = Reflector.GetPropertyGetter<IList>(cropData, "Phases").GetValue();

            // Find the last phase that has a harvest drop
            IList? harvestDrops = null;
            foreach (var phase in cropPhases)
            {
                var phaseDrops = Reflector.GetPropertyGetter<IList>(phase!, "HarvestedDrops").GetValue()!;
                if (phaseDrops.Count > 0)
                    harvestDrops = phaseDrops;
            }
            if (harvestDrops == null)
                return null;
            if (harvestDrops.Count > 1)
                throw new Exception("DGA crops with multiple drops on the last harvest are not supported");

            var possibleDrops = Reflector.GetPropertyGetter<IList>(harvestDrops[0]!, "Item").GetValue();
            if (possibleDrops.Count != 1)
                throw new Exception("DGA crops with random drops are not supported");

            var dropItem = Reflector.GetPropertyGetter<object?>(possibleDrops[0]!, "Value").GetValue()!;
            string dropItemType = Reflector.GetPropertyGetter<Enum>(dropItem, "Type").GetValue()!.ToString()!;
            string dropItemValue = Reflector.GetPropertyGetter<string?>(dropItem, "Value").GetValue()!;
            
            if (dropItemType == "DGAItem")
                return (StardewValley.Object) this.Api.SpawnDGAItem(dropItemValue);
            else if (dropItemType == "VanillaItem")
                return new StardewValley.Object(dropItemValue, 1);
            else
                throw new Exception("Harvest types other than DGAItem and VanillaItem are not supported");
        }
        #endregion

        #region Code loading from DGA assembly
        private IReflectedMethod? GetModFindMethod() {
            if (_modFindMethod != null)
                return _modFindMethod;
            
            if (_dgaAssembly == null)
                return null;
            
            var modClass = _dgaAssembly.GetType("DynamicGameAssets.Mod")!;
            _modFindMethod = Reflection.GetMethod(modClass, "Find");
            return _modFindMethod;
        }
        #endregion
    }
}
