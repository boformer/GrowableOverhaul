using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GrowableOverhaul
/// <summary>
/// These are simple classes for testing purposes. 
/// </summary>
{
    public class LargerResidential : ResidentialBuildingAI
    {

        public override void GetWidthRange(out int minWidth, out int maxWidth)
        {
            minWidth = 1;
            maxWidth = 16;
        }

        public override void GetLengthRange(out int minLength, out int maxLength)
        {
            minLength = 1;
            maxLength = 16;
        }
    }
class PrefabManager
    {

        string[] LowResTestlots = {

            //1 Depth
            //"1x1ResTest_Data",

            //2 Depth
            //"1x2ResTest_Data",
            //"2x2ResTest_Data",
           // "3x2ResTest_Data",
           // "4x2ResTest_Data",

            //3 Depth
           // "1x3ResTest_Data",
           // "2x3ResTest_Data",
           // "3x3ResTest_Data",
           // "4x3ResTest_Data",
           // "5x3ResTest_Data",
           // "6x3ResTest_Data",

            //4 Depth

           // "1x4ResTest_Data",
           // "2x4ResTest_Data",
           // "3x4ResTest_Data",
           // "4x4ResTest_Data",

           // "5x4ResTest_Data",
           // "6x4ResTest_Data",
           // "7x4ResTest_Data",
           // "8x4ResTest_Data",

            //5 Depth
            "1x5ResTest_Data",
            "2x5ResTest_Data",
            "3x5ResTest_Data",
            "4x5ResTest_Data",

            "5x5ResTest_Data",
            "6x5ResTest_Data",
            "7x5ResTest_Data",
            "8x5ResTest_Data",

            //6 Depth
            "1x6ResTest_Data",
            "2x6ResTest_Data",
            "3x6ResTest_Data",
            "4x6ResTest_Data",

            "5x6ResTest_Data",
            "6x6ResTest_Data",
            "7x6ResTest_Data",
            "8x6ResTest_Data",

            //7 Depth
            "1x7ResTest_Data",
            "2x7ResTest_Data",
            "3x7ResTest_Data",
            "4x7ResTest_Data",

            "5x7ResTest_Data",
            "6x7ResTest_Data",
            "7x7ResTest_Data",
            "8x7ResTest_Data",

            //8 Depth
            "1x8ResTest_Data",
            "2x8ResTest_Data",
            "3x8ResTest_Data",
            "4x8ResTest_Data",

            "5x8ResTest_Data",
            "6x8ResTest_Data",
            "7x8ResTest_Data",
            "8x8ResTest_Data"
        };

        public void ConvertPrefab()
        {

            foreach (var buildingName in LowResTestlots)
            {

                var prefab = PrefabCollection<BuildingInfo>.FindLoaded(buildingName);

                if (prefab != null)
                {
                    var ai = prefab.gameObject.AddComponent<LargerResidential>();
                    prefab.m_buildingAI = ai;
                    prefab.m_buildingAI.m_info = prefab;
                    prefab.m_zoningMode = BuildingInfo.ZoningMode.Straight;
                    prefab.InitializePrefab();
                    prefab.m_autoRemove = true;
                   
                    prefab.m_class = ItemClassCollection.FindClass("Low Residential - Level1");
                }

            }
        }
    }
}
