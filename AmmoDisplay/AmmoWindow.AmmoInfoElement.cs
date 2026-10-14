using System;
using AriUtils;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using Sandbox.Definitions;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace SetbTankManager.Client.AmmoDisplay
{
    internal partial class AmmoWindow
    {
        private class AmmoInfoElement : TexturedBox
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
                    this.Color = (magAmmoCount == 0 ? GlobalData.NoAmmoColor : Color.Transparent);
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