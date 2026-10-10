
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace AriUtils
{
    partial class GlobalData
    {
        public const string FriendlyModName = "SETBTankManager";
        public static readonly string LastBuildTime = "$MDK_DATETIME$";
        public const ushort ServerNetworkId = 15189;
        public const ushort DataNetworkId = 15188;
        public const ushort ClientNetworkId = 15187;

        public ObjectPool<List<IMyCubeGrid>> GridListPool = new ObjectPool<List<IMyCubeGrid>>(factory: () => new List<IMyCubeGrid>(), cleanObj: list => list.Clear());

        private static Func<string, bool> KillswitchCheck => modIdFormatted => modIdFormatted.Contains("skytech") && modIdFormatted.Contains("engines");
    }
}
