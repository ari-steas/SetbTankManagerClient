using AriUtils;
using RichHudFramework.UI;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using RichHudFramework.UI.Rendering;
using VRage.Game.ModAPI;
using VRageMath;
using static CoreSystems.Api.WcApiManager;
using MyInventoryItem = VRage.Game.ModAPI.Ingame.MyInventoryItem;

namespace SetbTankManager.Client.AmmoDisplay
{
    internal class AmmoWindow : HudElementBase
    {
        private IMyCubeGrid _playerGrid;
        private bool _awaitSetGrid = false;
        private List<IMyCubeGrid> _grids = new List<IMyCubeGrid>();
        private List<MyInventoryItem> _items = new List<MyInventoryItem>();

        private List<IMyInventory> _inventories = new List<IMyInventory>();
        private List<IMyCubeBlock> _weapons = new List<IMyCubeBlock>();
        /// <summary>
        /// subtypeid, count
        /// </summary>
        private Dictionary<string, int> _itemCountBuffer = new Dictionary<string, int>();
        /// <summary>
        /// subtypeid, count
        /// </summary>
        private Dictionary<string, ItemInfo> _itemCount = new Dictionary<string, ItemInfo>();

        private HudChain _weaponsBox;
        private readonly GlyphFormat _leftAlignFormat = new GlyphFormat(color: Color.White, alignment: TextAlignment.Left);
        private readonly GlyphFormat _underscoreAlignFormat = new GlyphFormat(color: Color.White, alignment: TextAlignment.Left, style: FontStyles.Underline);

        public AmmoWindow(HudParentBase parent) : base(parent)
        {
            ParentAlignment = ParentAlignments.InnerTopLeft;

            _weaponsBox = new HudChain(true, this)
            {
                ParentAlignment = ParentAlignments.PaddedInnerTopLeft,
                Padding = Vector2.One * 4f,
                SizingMode = HudChainSizingModes.AlignMembersStart,
                Spacing = 2.5f,
            };
            TexturedBox box = new TexturedBox(_weaponsBox)
            {
                DimAlignment = DimAlignments.Both,
                Color = GlobalData.HudBackgroundColor,
            };
        }

        public void SetGrid(IMyCubeGrid playerGrid)
        {
            _playerGrid = playerGrid;
            _awaitSetGrid = true;
        }

        public void Update()
        {
            if (_awaitSetGrid)
            {
                if (WcApi != null && WcApi.IsReady)
                {
                    _inventories.Clear();
                    _weapons.Clear();
                    if (_playerGrid == null)
                    {
                        Visible = false;
                        return;
                    }

                    _playerGrid.GetGridGroup(GridLinkTypeEnum.Logical).GetGrids(_grids);
                    foreach (var iGrid in _grids)
                    {
                        MyCubeGrid grid = (MyCubeGrid) iGrid;
                        foreach (var inventoryBlock in grid.Inventories)
                        {
                            if (((IMyTerminalBlock) inventoryBlock).ShowInInventory && WcApi.HasCoreWeapon(inventoryBlock))
                            {
                                _weapons.Add(inventoryBlock);
                            }
                            else
                            {
                                _inventories.Add(inventoryBlock.GetInventory()); // notably doesn't support multiple inventories
                            }
                        }
                    }
                    _grids.Clear();
                    Visible = true;
                    _awaitSetGrid = false;
                }
                else
                {
                    return;
                }
            }

            if (_playerGrid == null)
            {
                return;
            }

            GetItemCount(ref _itemCount);

            if (_weaponsBox.Count > _weapons.Count)
            {
                _weaponsBox.RemoveRange(_weaponsBox.Count - (_weaponsBox.Count - _weapons.Count), _weaponsBox.Count - 1);
            }

            while (_weaponsBox.Count < _weapons.Count)
            {
                _weaponsBox.Add(new HudChain(true)
                {
                    SizingMode = HudChainSizingModes.AlignMembersStart,
                    DimAlignment = DimAlignments.Width,
                    Spacing = 4,
                });
            }

            int i = 0;
            foreach (var weapon in _weapons)
            {
                HudChain chain = (HudChain) _weaponsBox[i++].Element;
                SetupWeaponHudCollection(weapon, chain);
            }

            _itemCount.Clear();
        }

        private void SetupWeaponHudCollection(IMyCubeBlock weapon, HudChain chain)
        {
            string[] ammoIds;
            if (!AmmoDisplay.I.WeaponMagMap.TryGetValue(weapon.BlockDefinition.SubtypeName, out ammoIds)) 
                ammoIds = Array.Empty<string>();

            weapon.GetInventory().GetItems(_items);
            foreach (var item in _items)
            {
                if (!_itemCountBuffer.ContainsKey(item.Type.SubtypeId))
                {
                    _itemCountBuffer.Add(item.Type.SubtypeId, (int) item.Amount);
                }
                else
                {
                    _itemCountBuffer[item.Type.SubtypeId] += (int) item.Amount;
                }
            }
            _items.Clear();

            if (chain.Count > ammoIds.Length + 1)
            {
                chain.RemoveRange(chain.Count - (chain.Count - ammoIds.Length), chain.Count - 1);
            }

            while (chain.Count < ammoIds.Length + 1)
            {
                chain.Add(new LabelBox
                {
                    ParentAlignment = ParentAlignments.Left,
                    //DimAlignment = DimAlignments.Width,
                    Color = Color.Transparent,
                    Text = "",
                    TextPadding = new Vector2(2.5f, 0),
                    Padding = Vector2.Zero,
                    FitToTextElement = true,
                    Format = _leftAlignFormat,
                });
            }

            int i = 0;
            {
                LabelBox label = (LabelBox) chain[i++].Element;
                label.Text = $"{weapon.DisplayNameText}: {(weapon.SlimBlock.Integrity / weapon.SlimBlock.MaxIntegrity) * 100:F0}% HP";
                label.Format = _underscoreAlignFormat;
            }
            foreach (var item in ammoIds)
            {
                LabelBox label = (LabelBox) chain[i++].Element;

                ItemInfo currentCount;
                if (!_itemCount.TryGetValue(item, out currentCount))
                    currentCount = new ItemInfo(AmmoDisplay.I.AmmoDefDisplayNames.GetValueOrDefault(item, item), 0);
                int weaponMagCt = _itemCountBuffer.GetValueOrDefault(item, 0);

                label.Text = $" |  {currentCount.DisplayName}: {weaponMagCt} MAG | {currentCount.Count} STOR";
                label.Color = (weaponMagCt == 0 ? GlobalData.NoAmmoColor : Color.Transparent);
            }

            _itemCountBuffer.Clear();
        }

        private void GetItemCount(ref Dictionary<string, ItemInfo> itemCount)
        {
            foreach (var inventory in _inventories)
            {
                inventory.GetItems(_items);
                foreach (var item in _items)
                {
                    string dispName;
                    if (!AmmoDisplay.I.AmmoDefDisplayNames.TryGetValue(item.Type.SubtypeId, out dispName))
                        continue;

                    ItemInfo info;
                    if (!itemCount.TryGetValue(item.Type.SubtypeId, out info))
                    {
                        itemCount.Add(item.Type.SubtypeId, new ItemInfo(dispName, (int) item.Amount));
                    }
                    else
                    {
                        info.Count += (int) item.Amount;
                    }
                }
                _items.Clear();
            }
        }

        private class ItemInfo
        {
            public string DisplayName;
            public int Count;

            public ItemInfo(string displayName, int count)
            {
                DisplayName = displayName;
                Count = count;
            }
        }
    }
}
