using GrowableOverhaul.Redirection;
using ColossalFramework;
using System;
using System.Threading;
using UnityEngine;

namespace GrowableOverhaul
{
    //Reason for detour: Add some new global colors for new zones. 
    [TargetType(typeof(ZoneProperties))]
    class ZonePropertiesDetour
    {

        [RedirectMethod(true)]
        public static void InitializeShaderProperties(ZoneProperties _this)
        {
            for (int i = 0; i < NewZoneColorManager.NewColors.Length; i++)
            {
                Shader.SetGlobalColor("_ZoneColor" + i, NewZoneColorManager.NewColors[i].linear);
                //Debug.Log("Set new Shader " + i + NewZoneColorManager.NewColors[i]);
            }

            Shader.SetGlobalColor("_ZoneFillColor", _this.m_fillColor.linear);
            Shader.SetGlobalColor("_ZoneEdgeColor", _this.m_edgeColor.linear);
            Shader.SetGlobalColor("_ZoneEdgeColorOccupied", _this.m_edgeColorOccupied.linear);
            Shader.SetGlobalColor("_ZoneFillColorInfo", _this.m_fillColorInfo.linear);
            Shader.SetGlobalColor("_ZoneEdgeColorInfo", _this.m_edgeColorInfo.linear);
            Shader.SetGlobalColor("_ZoneEdgeColorOccupiedInfo", _this.m_edgeColorOccupiedInfo.linear);
        }


    }
}
