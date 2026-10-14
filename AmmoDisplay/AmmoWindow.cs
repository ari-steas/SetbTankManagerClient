using AriUtils;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using VRage.Game.ModAPI;
using VRageMath;
using static CoreSystems.Api.WcApiManager;
using MyInventoryItem = VRage.Game.ModAPI.Ingame.MyInventoryItem;

namespace SetbTankManager.Client.AmmoDisplay
{
    internal partial class AmmoWindow : HudElementBase
    {
        private IMyCubeGrid _playerGrid;
        private bool _awaitSetGrid = false;
        private List<IMyCubeGrid> _grids = new List<IMyCubeGrid>();
        private List<MyInventoryItem> _itemBuffer = new List<MyInventoryItem>();

        private List<IMyInventory> _inventories = new List<IMyInventory>();
        /// <summary>
        /// grid & subgrid weapons organized by displayname
        /// </summary>
        private Dictionary<string, List<IMyCubeBlock>> _weapons = new Dictionary<string, List<IMyCubeBlock>>();
        /// <summary>
        /// subtypeid, count
        /// </summary>
        private Dictionary<string, int> _itemCountBuffer = new Dictionary<string, int>();
        /// <summary>
        /// subtypeid, count
        /// </summary>
        private Dictionary<string, int> _itemCount = new Dictionary<string, int>();

        private HudChain _weaponsBox;
        private static readonly GlyphFormat _leftAlignFormat = new GlyphFormat(color: Color.White, alignment: TextAlignment.Left);
        private static readonly GlyphFormat _leftAlignFormatGray = new GlyphFormat(color: Color.Gray, alignment: TextAlignment.Left);
        private static readonly GlyphFormat _underscoreAlignFormat = new GlyphFormat(color: Color.White, alignment: TextAlignment.Left, style: FontStyles.Underline);
        private const float AmmoIconSize = 24;

        public AmmoWindow(HudParentBase parent) : base(parent)
        {
            ParentAlignment = ParentAlignments.InnerTopLeft;

            _weaponsBox = new HudChain(true, this)
            {
                ParentAlignment = ParentAlignments.PaddedInnerTopLeft,
                Padding = Vector2.One * 4f,
                SizingMode = HudChainSizingModes.AlignMembersStart | HudChainSizingModes.FitChainBoth,
                Spacing = 2.5f,
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
                                List<IMyCubeBlock> subtypeWeapons;
                                if (!_weapons.TryGetValue(inventoryBlock.BlockDefinition.DisplayNameText, out subtypeWeapons))
                                {
                                    subtypeWeapons = new List<IMyCubeBlock>();
                                    _weapons.Add(inventoryBlock.BlockDefinition.DisplayNameText, subtypeWeapons);
                                }
                                subtypeWeapons.Add(inventoryBlock);
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

            foreach (var type in _weapons)
            {
                foreach (var weapon in type.Value)
                {
                    if (weapon.Closed)
                    {
                        _awaitSetGrid = true;
                        break;
                    }
                }

                if (_awaitSetGrid)
                    break;
            }

            if (_awaitSetGrid)
                return;

            GetItemCount(ref _itemCount);

            if (_weaponsBox.Count > _weapons.Count)
            {
                _weaponsBox.RemoveRange(_weaponsBox.Count - (_weaponsBox.Count - _weapons.Count), _weaponsBox.Count - _weapons.Count);
            }

            while (_weaponsBox.Count < _weapons.Count)
            {
                _weaponsBox.Add(new WeaponGroupInfoElement(null));
            }

            int i = 0;
            foreach (var weaponSet in _weapons)
            {
                WeaponGroupInfoElement chain = (WeaponGroupInfoElement) _weaponsBox[i++].Element;
                chain.UpdateHud(weaponSet.Value, _itemCount, ref _itemBuffer, ref _itemCountBuffer);
            }

            _itemCount.Clear();
        }

        private void GetItemCount(ref Dictionary<string, int> itemCount)
        {
            foreach (var inventory in _inventories)
            {
                inventory.GetItems(_itemBuffer);
                foreach (var item in _itemBuffer)
                {
                    MyAmmoMagazineDefinition magDef;
                    if (!AmmoDisplay.I.AmmoDefs.TryGetValue(item.Type.SubtypeId, out magDef))
                        continue;

                    if (!itemCount.ContainsKey(item.Type.SubtypeId))
                    {
                        itemCount.Add(item.Type.SubtypeId, (int) item.Amount);
                    }
                    else
                    {
                        itemCount[item.Type.SubtypeId] += (int) item.Amount;
                    }
                }
                _itemBuffer.Clear();
            }
        }
    }
}
