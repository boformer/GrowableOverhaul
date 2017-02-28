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

            CommercialLow,
            CommercialHigh,

            IndustrialHigh,
            Industrial,

            Office,
            OfficeHigh,

            Test,

            None = 15
        }
    }

    public class NewZoneColorManager {

        public static Color[] NewColors = new Color[] {

        new Color { r = 0, g = 0, b = 0, a = 0 },
        new Color { r = 0, g = 0, b = 0, a = 0 },

        new Color { r = 0, g = 1, b = 0, a = 1 }, //res low
        new Color { r = 0, g = 1, b = 0, a = 1 }, //res med
        new Color { r = 0, g = 1, b = 0, a = 1 }, //res high

        new Color { r = 0, g = 0, b = 1, a = 1 }, //com low
        new Color { r = 0, g = 0, b = 1, a = 1 }, //com med

        new Color { r = 1, g = 1, b = 0, a = 1 }, //ind low
        new Color { r = 1, g = 1, b = 0, a = 1 }, //ind high

        new Color { r = 0, g = 1, b = 1, a = 1 }, //off low
        new Color { r = 0, g = 1, b = 1, a = 1 } //off high

        };

        public static void SetNewColors() {

            //Singleton<ZoneManager>.instance.m_properties.m_zoneColors = new Color[16];

            var i = 0;
            foreach (var color in NewColors) {

                //Singleton<ZoneManager>.instance.m_properties.m_zoneColors[i] = color;
                Shader.SetGlobalColor("_ZoneColor" + i, color.linear);
                i++;
            }
        
        }

        
    }
}
