using AriUtils;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using VRage.Game.ModAPI;
using VRage.Utils;
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
        private List<MyInventoryItem> _itemBuffer = new List<MyInventoryItem>();

        private List<IMyInventory> _inventories = new List<IMyInventory>();
        private List<IMyCubeBlock> _weapons = new List<IMyCubeBlock>();
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
                _weaponsBox.Add(new WeaponInfoElement(null));
            }

            int i = 0;
            foreach (var weapon in _weapons)
            {
                WeaponInfoElement chain = (WeaponInfoElement) _weaponsBox[i++].Element;
                chain.UpdateHud(weapon, _itemCount, ref _itemBuffer, ref _itemCountBuffer);
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

        private class WeaponInfoElement : HudElementBase
        {
            protected readonly LabelBox Label;
            protected readonly RichText LabelText;
            protected readonly HudChain Chain;

            private IMyCubeBlock _lastWeapon = null;
            private IMyInventory _lastWeaponInv = null;
            private float _lastWeaponIntegrity = -1;
            private string[] _ammoIds;

            public WeaponInfoElement(HudParentBase parent) : base(parent)
            {
                ParentAlignment = ParentAlignments.InnerTop;
                Padding = new Vector2(4, 4);
                Label = new LabelBox(this)
                {
                    ParentAlignment = ParentAlignments.InnerTopLeft,
                    //DimAlignment = DimAlignments.Width,
                    Color = Color.Transparent,
                    TextPadding = new Vector2(2.5f, 0),
                    Padding = Vector2.Zero,
                    FitToTextElement = true,
                };
                Chain = new HudChain(false, Label)
                {
                    ParentAlignment = ParentAlignments.InnerLeft | ParentAlignments.Bottom,
                    SizingMode = HudChainSizingModes.AlignMembersStart | HudChainSizingModes.FitChainBoth,
                    DimAlignment = DimAlignments.Width,
                    Spacing = 4,
                };
                LabelText = new RichText(_underscoreAlignFormat);
            }

            public void UpdateHud(IMyCubeBlock weapon, Dictionary<string, int> storedItemCount, ref List<MyInventoryItem> itemBuffer, ref Dictionary<string, int> itemCountBuffer)
            {
                bool needsWeaponLabelUpdate = weapon.SlimBlock.Integrity != _lastWeaponIntegrity;
                _lastWeaponIntegrity = weapon.SlimBlock.Integrity;
                if (_lastWeapon != weapon) // caching my BELOVED
                {
                    _lastWeapon = weapon;
                    if (!AmmoDisplay.I.WeaponMagMap.TryGetValue(weapon.BlockDefinition.SubtypeName, out _ammoIds)) 
                        _ammoIds = Array.Empty<string>();
                    if (Chain.Count > _ammoIds.Length)
                    {
                        Chain.RemoveRange(Chain.Count - (Chain.Count - _ammoIds.Length), Chain.Count - 1);
                    }
                    while (Chain.Count < _ammoIds.Length)
                    {
                        Chain.Add(new AmmoInfoElement(null));
                    }
                    _lastWeaponInv = weapon.GetInventory();
                    needsWeaponLabelUpdate = true;
                }

                if (needsWeaponLabelUpdate)
                {
                    LabelText.Clear();
                    LabelText.Add(weapon.DisplayNameText);
                    LabelText.Add(": ");
                    LabelText.Add(((weapon.SlimBlock.Integrity / weapon.SlimBlock.MaxIntegrity) * 100).ToString("F0"));
                    LabelText.Add("% HP");
                    Label.Text = LabelText; // this is fucking hilarious, triggers text board update
                }
                
                _lastWeaponInv.GetItems(itemBuffer);
                foreach (var item in itemBuffer)
                {
                    if (!itemCountBuffer.ContainsKey(item.Type.SubtypeId))
                    {
                        itemCountBuffer.Add(item.Type.SubtypeId, (int) item.Amount);
                    }
                    else
                    {
                        itemCountBuffer[item.Type.SubtypeId] += (int) item.Amount;
                    }
                }
                itemBuffer.Clear();

                for (int i = 0; i < _ammoIds.Length; i++)
                {
                    string ammoSubtypeId = _ammoIds[i];
                    int magAmmoCount = itemCountBuffer.GetValueOrDefault(ammoSubtypeId, 0);
                    int storAmmoCount = storedItemCount.GetValueOrDefault(ammoSubtypeId, 0);

                    AmmoInfoElement ammoInfo = (AmmoInfoElement) Chain[i].Element;
                    ammoInfo.SetLabelText(_lastWeaponInv, ammoSubtypeId, magAmmoCount, storAmmoCount);
                }

                itemCountBuffer.Clear();
            }

            protected override void Measure()
            {
                Height = Label.Height + Chain[0].Element.Height + Padding.Y;
            }
        }
        

        private class AmmoInfoElement : HudElementBase
        {
            protected readonly TexturedBox AmmoIcon;
            protected readonly LabelBox Label;
            protected readonly RichText Text;

            private IMyInventory _lastWeaponInv = null;
            private string _lastMagSubtypeId = "";
            private MyAmmoMagazineDefinition _magDef = null;
            private int _lastMagAmmoCt = -1;
            private int _lastStorAmmoCt = -1;

            public AmmoInfoElement(HudParentBase parent) : base(parent)
            {
                ParentAlignment = ParentAlignments.InnerLeft;
                Height = AmmoIconSize;
                AmmoIcon = new TexturedBox(this)
                {
                    ParentAlignment = ParentAlignments.InnerLeft,
                    Size = new Vector2(AmmoIconSize, AmmoIconSize)
                };
                Label = new LabelBox(AmmoIcon)
                {
                    ParentAlignment = ParentAlignments.Right,
                    //DimAlignment = DimAlignments.Width,
                    Color = Color.Transparent,
                    TextPadding = new Vector2(2.5f, 0),
                    Padding = Vector2.Zero,
                    FitToTextElement = true,
                };
                Text = new RichText(_leftAlignFormat);
            }

            public void SetLabelText(IMyInventory weaponInventory, string magSubtypeId, int magAmmoCount, int storedAmmoCount)
            {
                if (_lastMagSubtypeId != magSubtypeId) // only update if needed
                {
                    _lastMagSubtypeId = magSubtypeId;
                    _magDef = AmmoDisplay.I.AmmoDefs[magSubtypeId];
                    MyStringId icon = MyStringId.GetOrCompute(_magDef.Id.SubtypeName + "_AutoMaterial"); // horrid jank, need to define a transparent material in this mod
                    AmmoIcon.Material = new Material(icon, new Vector2(AmmoIconSize, AmmoIconSize));
                    Log.Info("AmmoWindow.AmmoInfoElement", $"Set ammo icon material to {icon}");
                    _lastMagAmmoCt = -1; // force update
                }

                if (_lastWeaponInv != weaponInventory) // only update if needed
                {
                    _lastWeaponInv = weaponInventory;
                    _lastMagAmmoCt = -1; // force update
                }

                if (_lastMagAmmoCt != magAmmoCount || _lastStorAmmoCt != storedAmmoCount) // only update if needed
                {
                    _lastMagAmmoCt = magAmmoCount;
                    _lastStorAmmoCt = storedAmmoCount;

                    Text.Clear();
                    for (int i = 0; i < magAmmoCount; i++)
                    {
                        Text.Add('I', _leftAlignFormat);
                    }

                    int maxMagCount = (int) Math.Floor((float) weaponInventory.MaxVolume / _magDef.Volume);
                    for (int i = magAmmoCount; i < maxMagCount; i++)
                    {
                        Text.Add('I', _leftAlignFormatGray);
                    }

                    Text.Add(" x", _leftAlignFormat);
                    Text.Add(storedAmmoCount.ToString(), _leftAlignFormat);

                    Label.Text = Text;
                    Label.Color = (magAmmoCount == 0 ? GlobalData.NoAmmoColor : Color.Transparent);
                }
            }

            protected override void Measure()
            {
                Width = Label.Width + AmmoIcon.Width + 4;
                Height = AmmoIcon.Height;
            }
        }
    }
}
