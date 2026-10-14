using System;
using AriUtils;
using CoreSystems.Api;
using RichHudFramework;
using RichHudFramework.UI.Client;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;

namespace SetbTankManager.Client.AmmoDisplay
{
    internal class AmmoDisplay : SingletonBase<AmmoDisplay>
    {
        private AmmoWindow _window = null;
        /// <summary>
        /// subtype, definition
        /// </summary>
        public readonly Dictionary<string, MyAmmoMagazineDefinition> AmmoDefs = new Dictionary<string, MyAmmoMagazineDefinition>();
        /// <summary>
        /// Weapon subtype, ammo subtypes
        /// </summary>
        public readonly Dictionary<string, string[]> WeaponMagMap = new Dictionary<string, string[]>();

        public override void Init()
        {
            ApiManager.RichHudOnLoadRegisterOrInvoke(OnRichHudReady);
            TurretManager.I.OnPlayerChangeGrid += (grid) => _window?.SetGrid(grid);
            WcApiManager.OnWcApiReady += () =>
            {
                IDictionary<MyDefinitionId, List<MyTuple<int, MyTuple<MyDefinitionId, string, string, bool>>>> weaponMagMap = new Dictionary<MyDefinitionId, List<MyTuple<int, MyTuple<MyDefinitionId, string, string, bool>>>>();
                WcApiManager.WcApi.GetAllWeaponMagazines(weaponMagMap);
                Log.Info("AmmoDisplay", $"Start poll WC magazines - count {weaponMagMap.Count}...");
                foreach (var weaponDef in WcApiManager.WcApi.WeaponDefinitions)
                {
                    HashSet<string> magIds = new HashSet<string>();
                    foreach (var mag in weaponDef.Ammos)
                    {
                        if (mag.AmmoMagazine == "Energy")
                            continue;
                        magIds.Add(mag.AmmoMagazine);
                    }

                    if (magIds.Count == 0)
                        continue;
                
                    string[] magIdArr = magIds.ToArray();
                    foreach (var mountpoint in weaponDef.Assignments.MountPoints)
                    {
                        WeaponMagMap[mountpoint.SubtypeId] = magIdArr;
                        //Log.Info("AmmoDisplay", $"Found weapon ammos \"{mountpoint.SubtypeId}\" -> \"{string.Join(", ", magIds)}\"");
                    }
                }
                Log.Info("AmmoDisplay", $"Finished polling WC magazines.");

                WcApiManager.WcApi.WeaponDefinitions = null;
            };

            Log.Info("AmmoDisplay", $"Start poll ammo magazine names...");
            foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions())
            {
                MyAmmoMagazineDefinition ammoDef = definition as MyAmmoMagazineDefinition;
                if (ammoDef == null)
                    continue;

                AmmoDefs[ammoDef.Id.SubtypeName] = ammoDef;
                //Log.Info("AmmoDisplay", $"Found ammo \"{ammoDef.Id.SubtypeName}\" -> \"{ammoDef.DisplayNameText}\"");
            }
            Log.Info("AmmoDisplay", $"Finished polling ammo magazine names.");
        }

        public override void Update()
        {
            if (_window == null)
                return;

            if (MyAPIGateway.Session.GameplayFrameCounter % 31 == 0)
            {
                _window.Update();
            }
        }

        private void OnRichHudReady()
        {
            _window = new AmmoWindow(HudMain.HighDpiRoot);
        }
    }
}
