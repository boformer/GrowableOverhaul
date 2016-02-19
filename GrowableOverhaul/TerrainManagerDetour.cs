using GrowableOverhaul.Redirection;

namespace GrowableOverhaul
{
    [TargetType(typeof(TerrainManager))]
    public static class TerrainManagerDetour
    {
        public static Redirector UpdateDataRedirector = null;

        [RedirectMethod(true)] // Detour reason: Load extra zone data before method is executed
        public static void UpdateData(TerrainManager _this, SimulationManager.UpdateMode mode)
        {
            DataExtension.instance.OnUpdateData();

            // Call original method
            UpdateDataRedirector.Revert();
            _this.UpdateData(mode);
            UpdateDataRedirector.Apply();
        }
    }
}
