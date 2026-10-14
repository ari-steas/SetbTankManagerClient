using AriUtils;
using RichHudFramework.UI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using IMyCubeBlock = VRage.Game.ModAPI.IMyCubeBlock;
using IMyInventory = VRage.Game.ModAPI.IMyInventory;

namespace SetbTankManager.Client.AmmoDisplay
{
    internal partial class AmmoWindow
    {
        private class WeaponInfoElement : HudElementBase
        {
            protected readonly HudChain Chain;

            private IMyCubeBlock _lastWeapon = null;
            private IMyInventory _lastWeaponInv = null;
            private float _lastWeaponIntegrity = -1;

            public WeaponInfoElement(HudParentBase parent) : base(parent)
            {
                ParentAlignment = ParentAlignments.InnerTopLeft;
                Padding = new Vector2(4, 0);
                Chain = new HudChain(false, this)
                {
                    ParentAlignment = ParentAlignments.InnerTopLeft,
                    SizingMode = HudChainSizingModes.AlignMembersStart | HudChainSizingModes.FitChainBoth,
                    DimAlignment = DimAlignments.Width,
                    Spacing = 4,
                };
            }

            public void UpdateHud(IMyCubeBlock weapon, Dictionary<string, int> storedItemCount, string[] ammoIds, ref List<MyInventoryItem> itemBuffer, ref Dictionary<string, int> itemCountBuffer)
            {
                if (_lastWeapon != weapon) // caching my BELOVED
                {
                    _lastWeapon = weapon;
                    if (Chain.Count > ammoIds.Length)
                    {
                        Chain.RemoveRange(Chain.Count - (Chain.Count - ammoIds.Length), Chain.Count - 1);
                    }
                    while (Chain.Count < ammoIds.Length)
                    {
                        Chain.Add(new AmmoInfoElement(null));
                    }
                    _lastWeaponInv = weapon.GetInventory();
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

                for (int i = 0; i < ammoIds.Length; i++)
                {
                    string ammoSubtypeId = ammoIds[i];
                    int magAmmoCount = itemCountBuffer.GetValueOrDefault(ammoSubtypeId, 0);
                    int storAmmoCount = storedItemCount.GetValueOrDefault(ammoSubtypeId, 0);

                    AmmoInfoElement ammoInfo = (AmmoInfoElement) Chain[i].Element;
                    ammoInfo.SetLabelText(_lastWeaponInv, ammoSubtypeId, magAmmoCount, storAmmoCount);
                }

                itemCountBuffer.Clear();
            }

            protected override void Measure()
            {
                Height = AmmoIconSize + Padding.Y;
                Width = (Chain[Chain.Count-1].Element.Position.X + Chain[Chain.Count-1].Element.Size.X - Chain[0].Element.Position.X) + Padding.X;
            }
        }
    }
}