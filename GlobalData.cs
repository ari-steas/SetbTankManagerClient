
using System;
using System.Collections.Generic;
using SetbTankManager.Client;
using VRage.Game.ModAPI;
using VRageMath;

namespace AriUtils
{
    partial class GlobalData
    {
        public const string FriendlyModName = "SETBTankManager";
        public static readonly string LastBuildTime = "$MDK_DATETIME$";
        public const ushort ServerNetworkId = 0;
        public const ushort DataNetworkId = 0;
        public const ushort ClientNetworkId = 0;

        public static readonly Color HudBackgroundColor = new Color(255, 255, 255, 40);
        public static readonly Color NoAmmoColor = new Color(255, 0, 0, 120);

        public ObjectPool<List<IMyCubeGrid>> GridListPool = new ObjectPool<List<IMyCubeGrid>>(factory: () => new List<IMyCubeGrid>(), cleanObj: list => list.Clear());

        private static Func<string, bool> KillswitchCheck => modIdFormatted => modIdFormatted.Contains("setbtankmanager");

        #region Client Config

        //private IniConfig.IniSetting<float> _aimSensitivity = new IniConfig.IniSetting<float>(
        //    _clientConfig,
        //    "AimSensitivity",
        //    "Aim sensitivity multiplier.",
        //    0.25f,
        //    TurretManager.I.UpdateSensitivity
        //);

        private IniConfig.IniSetting<Color> _crosshairColor = new IniConfig.IniSetting<Color>(
            _clientConfig,
            "CrosshairColor",
            "Ingame crosshair color.",
            Color.White * 0.75f,
            TurretManager.I.UpdateColor
            );

        #endregion
    }
}
