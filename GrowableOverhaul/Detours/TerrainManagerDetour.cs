using GrowableOverhaul.Redirection;
using ColossalFramework;
using UnityEngine;

namespace GrowableOverhaul
{
    [TargetType(typeof(TerrainManager))]

    public static class TerrainManagerDetour
    {
        public static Redirector UpdateDataRedirector = null;

        public static void BeginOverlayImpl(TerrainManager _this, RenderManager.CameraInfo cameraInfo) {

            if (true)
            {
                for (int k = 0; k < 9; k++)
                {
                    for (int l = 0; l < 9; l++)
                    {
                        int num2 = k * 9 + l;

                        // Debug.Log(Singleton<ZoneManager>.instance.m_zoneMaterial.GetColor("_ZoneColor4"));

                        var instance = Singleton<ZoneManager>.instance;

                        //Singleton<TerrainManager>.instance

                        instance.m_zoneMaterial.SetTexture(_this.ID_ZoneLayout, null);
                        //Singleton<ZoneManager>.instance.m_zoneMaterial.SetColorArray(1, NewZoneColorManager.NewColors);


                        _this.m_patches[num2].RenderOverlay(cameraInfo, Singleton<ZoneManager>.instance.m_zoneMaterial, true);
                    }
                }
            }

        }



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
