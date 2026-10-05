using System.Reflection;
using VRage.Plugins;

// Define assembly version when compiled by Pulsar
#if !LOCAL_BUILD
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
#endif

namespace SETBTankManager.Client
{
    // ReSharper disable once UnusedType.Global
    public class Plugin : IPlugin
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void Init(object gameInstance) { }
        public void Update() { }
        public void Dispose() { }

    }
}