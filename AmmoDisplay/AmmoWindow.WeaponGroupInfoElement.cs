using System;
using System.Collections.Generic;
using System.Linq;
using AriUtils;
using RichHudFramework.UI;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using IMyCubeBlock = VRage.Game.ModAPI.IMyCubeBlock;
using IMyInventory = VRage.Game.ModAPI.IMyInventory;

namespace SetbTankManager.Client.AmmoDisplay
{
    internal partial class AmmoWindow
    {
        private class WeaponGroupInfoElement : HudElementBase
        {
            protected readonly LabelBox Label;
            protected readonly RichText LabelText;
            protected readonly HudChain Chain;

            private IMyCubeBlock _lastFirstWeapon = null;
            private string[] _ammoIds;

            public WeaponGroupInfoElement(HudParentBase parent) : base(parent)
            {
                ParentAlignment = ParentAlignments.InnerLeft | ParentAlignments.Bottom;
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
                Chain = new HudChain(true, Label)
                {
                    ParentAlignment = ParentAlignments.InnerLeft | ParentAlignments.Bottom,
                    SizingMode = HudChainSizingModes.AlignMembersStart | HudChainSizingModes.FitChainBoth,
                    DimAlignment = DimAlignments.Width,
                };
                LabelText = new RichText(_underscoreAlignFormat);
            }

            public void UpdateHud(List<IMyCubeBlock> weaponSet, Dictionary<string, int> storedItemCount, ref List<MyInventoryItem> itemBuffer, ref Dictionary<string, int> itemCountBuffer)
            {
                if (weaponSet.Count == 0) // failsafe
                    return;

                bool needsWeaponLabelUpdate = false;
                if (weaponSet[0] != _lastFirstWeapon) // caching my BELOVED
                {
                    _lastFirstWeapon = weaponSet[0];
                    if (!AmmoDisplay.I.WeaponMagMap.TryGetValue(_lastFirstWeapon.BlockDefinition.SubtypeName, out _ammoIds)) 
                        _ammoIds = Array.Empty<string>();
                    needsWeaponLabelUpdate = true;
                }

                if (needsWeaponLabelUpdate)
                {
                    LabelText.Clear();
                    LabelText.Add(weaponSet.Count == 1
                        ? _lastFirstWeapon.DisplayNameText
                        : _lastFirstWeapon.DefinitionDisplayNameText);
                    Label.Text = LabelText; // this is fucking hilarious, triggers text board update
                }

                if (Chain.Count > weaponSet.Count)
                {
                    Chain.RemoveRange(Chain.Count - (Chain.Count - weaponSet.Count), Chain.Count - weaponSet.Count);
                }
                while (Chain.Count < weaponSet.Count)
                {
                    Chain.Add(new WeaponInfoElement(null));
                }

                for (var i = 0; i < weaponSet.Count; i++)
                {
                    WeaponInfoElement wInfo = (WeaponInfoElement) Chain[i].Element;
                    wInfo.UpdateHud(weaponSet[i], storedItemCount, _ammoIds, ref itemBuffer, ref itemCountBuffer);
                }
            }

            protected override void Measure()
            {
                Height = Label.Height + (Chain[0].Element.Position.Y + AmmoIconSize - Chain[Chain.Count-1].Element.Position.Y) + Padding.Y;
                Width = Math.Max(Chain.Max(c => c.Element.Width), Label.Width);
            }
        }
    }
}