using GrowableOverhaul.Redirection.Attributes;
using GrowableOverhaul.Redirection.Extensions;
using GrowableOverhaul.Redirection;

namespace GrowableOverhaul.Detours
{
    [TargetType(typeof(TerrainManager))]
    public class TerrainManagerDetour
    {
        //public static Redirector UpdateDataRedirector = null;

        [RedirectMethod] // Detour reason: Load extra zone data before method is executed
        public static void UpdateData(TerrainManager _this, SimulationManager.UpdateMode mode)
        {
            DataExtension.instance.OnUpdateData();

            // Call original method
            Redirector<TerrainManagerDetour>.Revert();
            _this.UpdateData(mode);
            Redirector<TerrainManagerDetour>.Deploy();
        }
    }
}
