using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ColossalFramework;

namespace GrowableOverhaul
{
   public class ExtendedItemClass
    {
        //Adds new zones. Used in place of old ItemClass
        public enum Zone
        {
            Unzoned,
            Distant,

            ResidentialLow,           
            ResidentialMedium,
            ResidentialHigh,

            Office,
            OfficeMedium,
            OfficeHigh,

            CommercialLow,
            CommercialMedium,
            CommercialHigh,

            Industrial,
            IndustrialMedium,
            IndustrialHigh,

            Farming,

            None = 15
        }
    }

    //Lets add some new zone colors. 
    public class NewZoneColorManager {

        public static Color[] NewColors = new Color[] {

        new Color { r = 0, g = 0, b = 0, a = 0 },
        new Color { r = 0, g = 0, b = 0, a = 0 },

        new Color { r = 0.2f, g = 0.75f, b = 0.2f, a = .9f }, //res low
        new Color { r = 0.04f, g = 0.6f, b = 0.2f, a = .9f }, //res med
        new Color { r = 0.01f, g = 0.34f, b = .01f, a = .9f }, //res high

        new Color { r = 0.1f, g = 0.9f, b = .9f, a = 1 }, //off low 0.012, 0.984, 0.984
        new Color { r = .06f, g = .6f, b = .6f, a = 1 }, //off med
        new Color { r = .01f, g = .34f, b = .34f, a = 1 }, //off high

        new Color { r = 1, g = 1, b = 0, a = 1 }, //ind low
        new Color { r = 0, g = 0, b = 1, a = 1 }, //ind med
        new Color { r = 1, g = 1, b = 0, a = 1 }, //ind high

        new Color { r = 0, g = 1, b = 1, a = 1 }, //com low
        new Color { r = 0, g = 1, b = 1, a = 1 }, //com mid
        new Color { r = 0, g = 1, b = 1, a = 1 }, //com high

        new Color { r = 1, g = 1, b = 1, a = 1 }, //Farming
 
        };

        public static void SetNewColors() {

            //Not really needed, but lets expand existing array. 
            Singleton<ZoneManager>.instance.m_properties.m_zoneColors = new Color[16];

            var i = 0;
            foreach (var color in NewColors) {

                Singleton<ZoneManager>.instance.m_properties.m_zoneColors[i] = color;
                Shader.SetGlobalColor("_ZoneColor" + i, color.linear);
                i++;
            }
        
        }

        
    }
}
