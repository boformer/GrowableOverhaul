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
            ResidentialMedium, //new
            ResidentialHigh,

            CommercialLow,
            CommercialHigh,

            IndustrialHigh, //new
            Industrial,

            Office,
            OfficeHigh, //new

            NewZone, //Room for 4 more! Perhaps farming and medium for office, industrial, and commercial?
            NewZone2,
            NewZone3,
            NewZone4,

            None = 15
        }
    }

    //Lets add some new zone colors. 
    public class NewZoneColorManager {

        public static Color[] NewColors = new Color[] {

        new Color { r = 0, g = 0, b = 0, a = 0 },
        new Color { r = 0, g = 0, b = 0, a = 0 },

        new Color { r = 0.2f, g = 0.8f, b = 0.2f, a = 1 }, //res low
        new Color { r = 0.004f, g = 0.6f, b = 0.2f, a = 1 }, //res med
        new Color { r = 0, g = 0.3f, b = 0, a = 1 }, //res high

        new Color { r = 0, g = 0, b = 1, a = 1 }, //com low
        new Color { r = 0, g = 0, b = 1, a = 1 }, //com med

        new Color { r = 1, g = 1, b = 0, a = 1 }, //ind low
        new Color { r = 1, g = 1, b = 0, a = 1 }, //ind high

        new Color { r = 0, g = 1, b = 1, a = 1 }, //off low
        new Color { r = 0, g = 1, b = 1, a = 1 }, //off high

        new Color { r = 1, g = 1, b = 1, a = 1 }, //white test
        new Color { r = 1, g = 1, b = 1, a = 1 },
        new Color { r = 1, g = 1, b = 1, a = 1 }, //white test
        new Color { r = 1, g = 1, b = 1, a = 1 }

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
