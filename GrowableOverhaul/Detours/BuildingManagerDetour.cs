using ColossalFramework;
using ColossalFramework.Threading;
using GrowableOverhaul.Redirection;
using UnityEngine;
using System.Reflection;
using ColossalFramework.Math;
using PloppableRICO;

namespace GrowableOverhaul
{
    [TargetType(typeof(BuildingManager))]

    public static class BuildingManagerDetour
    {

        private static FastList<ushort>[] areaBuildings = new FastList<ushort>[100000];

        private static FieldInfo m_areaBuildings_field;
        private static FieldInfo m_buildingsRefreshed_field;

        private static void FindFieldInfos()
        {
            if (m_areaBuildings_field == null || m_buildingsRefreshed_field == null)
            {
                m_areaBuildings_field = typeof(BuildingManager).GetField("m_areaBuildings", BindingFlags.NonPublic | BindingFlags.Instance);
                m_buildingsRefreshed_field = typeof(BuildingManager).GetField("m_buildingsRefreshed", BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }

        private static FastList<ushort>[] GetAreaBuildings( ref BuildingManager _this)
        {
            FindFieldInfos();
            return (FastList<ushort>[]) m_areaBuildings_field.GetValue(_this);
        }

        private static void SetAreaBuildings(ref BuildingManager _this, FastList<ushort>[] data) {

            FindFieldInfos();
            m_areaBuildings_field.SetValue(_this, data);

        }

        private static void SetBuildingsRefreshed(ref BuildingManager _this) {

            FindFieldInfos();
            m_buildingsRefreshed_field.SetValue(_this, true);
        }


        public static int SetPrefabDensity(BuildingInfo prefab)
        {

            if (prefab.m_collisionHeight < 16) return 1; //low
            else if (prefab.m_collisionHeight >= 16 & prefab.m_collisionHeight < 35) return 2; //medium
            else if (prefab.m_collisionHeight > 35) return 3; //high
            else return 1;
        }

        //Grab prefab density from RICO mod. 
        private static int GetDensity(BuildingInfo prefab)
        {
            //This is slow, and I know there are better ways to look this up. 
            foreach (var buildingData in RICOPrefabManager.prefabHash.Values)
            {
                if (buildingData.prefab == prefab) {

                    return buildingData.density;
                }
            }
                return 0;
        }

        //This methods reads all prefabs, and groups them based on service, sub service, size, ect... Detoured to account for larger lots and densities. 
        //called on scene load, and again from the RICO mod. 
        public static void ApplyExtendedRefreshBuildings()
        {

            int areaBuildingsLength = areaBuildings.Length;

            for (int i = 0; i < areaBuildingsLength; i++)
            {
                areaBuildings[i] = null;
            }
            int prefabCount = PrefabCollection<BuildingInfo>.PrefabCount();
            for (int j = 0; j < prefabCount; j++)
            {
                BuildingInfo prefab = PrefabCollection<BuildingInfo>.GetPrefab((uint)j);

                int style = 0;
                if (prefab != null && prefab.m_class.m_service != ItemClass.Service.None && prefab.m_placementStyle == ItemClass.Placement.Automatic && (!prefab.m_dontSpawnNormally || style > 0))
                {
                    int privateServiceIndex = ItemClass.GetPrivateServiceIndex(prefab.m_class.m_service);
                    if (privateServiceIndex != -1)
                    {  //modified to account for 8 deep lots. 
          
                        if (prefab.GetWidth() < 1 || prefab.GetWidth() > 16) //was 4
                        {
                            continue;

                        }//modified to account for 8 deep lots. 
                        else if (prefab.GetLength() < 1 || prefab.GetLength() > 16) //was 4
                        {                           
                            continue;
                        }
                        else
                        {
                            //For testing, lets make all assets level 1. 
                            int areaIndex = GetExtendedAreaIndex(prefab.m_class.m_service, prefab.m_class.m_subService, 
                            ItemClass.Level.Level1, SetPrefabDensity(prefab), prefab.GetWidth(), prefab.GetLength(), prefab.m_zoningMode);

                            Debug.Log(prefab.name + " AreaIndex is: " + areaIndex);

                            if (areaBuildings[areaIndex] == null)
                            {
                                areaBuildings[areaIndex] = new FastList<ushort>();
                            }
                            areaBuildings[areaIndex].Add((ushort)j);
                        }
                    }
                }
            }
            /*
            int num = 19;
            for (int j = 0; j < num; j++)
            {
                //5 levels
                for (int k = 0; k < 5; k++)
                {
                   //modified, original was 4
                    for (int l = 0; l < 16; l++)
                    {
                        //modified, original was 4
                        for (int m = 1; m < 16; m++)
                        {
                            //modified, original was 4
                            int num2 = j;
                            num2 = num2 * 5 + k;
                            num2 = num2 * 16 + l; // was 4
                            num2 = num2 * 16 + m; // was 4
                            num2 *= 2;
                            FastList<ushort> fastList = areaBuildings[num2];
                            FastList<ushort> fastList2 = areaBuildings[num2 - 2];
                            if (fastList2 != null)
                            {
                                if (fastList == null)
                                {
                                    areaBuildings[num2] = fastList2;
                                }
                                else
                                {
                                    for (int n = 0; n < fastList2.m_size; n++)
                                    {
                                        fastList.Add(fastList2.m_buffer[n]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            for (int index = 0; index < prefabCount; index++)
            {

                BuildingInfo info = PrefabCollection<BuildingInfo>.GetPrefab((uint)index);


                if (info != null && info.m_class.m_service != ItemClass.Service.None && info.m_placementStyle == ItemClass.Placement.Automatic && !info.m_dontSpawnNormally)
                {
                    int privateServiceIndex2 = ItemClass.GetPrivateServiceIndex(info.m_class.m_service);
                    if (privateServiceIndex2 != -1)
                    {//modified
                        if (info.GetWidth() >= 1 && info.GetWidth() <= 16) //was 4
                        {
                            if (info.GetLength() >= 1 && info.GetLength() <= 16) //was 4
                            {
                                ItemClass.Level level = ItemClass.Level.Level1;
                                ItemClass.Level level2 = ItemClass.Level.Level1;
                                if (info.m_class.m_service == ItemClass.Service.Residential)
                                {
                                    level2 = ItemClass.Level.Level5;
                                }
                                else if (info.m_class.m_service == ItemClass.Service.Commercial)
                                {
                                    if (info.m_class.m_subService == ItemClass.SubService.CommercialLow || info.m_class.m_subService == ItemClass.SubService.CommercialHigh)
                                    {
                                        level2 = ItemClass.Level.Level3;
                                    }
                                    else
                                    {
                                        level = ItemClass.Level.Level3;
                                    }
                                }
                                else if (info.m_class.m_service == ItemClass.Service.Industrial)
                                {
                                    if (info.m_class.m_subService == ItemClass.SubService.IndustrialGeneric)
                                    {
                                        level2 = ItemClass.Level.Level3;
                                    }
                                    else
                                    {
                                        level = ItemClass.Level.Level3;
                                    }
                                }
                                else if (info.m_class.m_service == ItemClass.Service.Office)
                                {
                                    level2 = ItemClass.Level.Level3;
                                }
                                if (info.m_class.m_level < level2)
                                {
                                    int areaIndex2 = GetExtendedAreaIndex(info.m_class.m_service, info.m_class.m_subService, info.m_class.m_level + 1, GetDensity(info),
                                        info.GetWidth(), info.GetLength(), info.m_zoningMode);

                                    if (areaBuildings[areaIndex2] == null)
                                    {
                                        continue;
                                    }
                                }
                                if (info.m_class.m_level > level)
                                {
                                    int areaIndex3 = GetExtendedAreaIndex( info.m_class.m_service, info.m_class.m_subService, info.m_class.m_level - 1, GetDensity(info),
                                        info.m_cellWidth, info.m_cellLength, info.m_zoningMode);

                                    if (areaBuildings[areaIndex3] == null)
                                    {

                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            */

            //Save private values via reflection. 
            //SetBuildingsRefreshed(ref _this);
            //SetAreaBuildings(ref _this, areaBuildings);
        }

        public static BuildingInfo GetExtendedRandomBuildingInfo(ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, int density, int width, int length, BuildingInfo.ZoningMode zoningMode, int style)
        {

            int index = GetExtendedAreaIndex(service, subService, level, density, width, length, zoningMode);

            Debug.Log("Requested index is: " + index);
            FastList<ushort> fastList;

            //disable styles for now
            
            if (style > 0)
            {/*
                style--;
                DistrictStyle districtStyle = Singleton<DistrictManager>.instance.m_Styles[style];
                if (style <= this.m_styleBuildings.Length && this.m_styleBuildings[style] != null && this.m_styleBuildings[style].Count > 0 && districtStyle.AffectsService(service, subService, level))
                {
                    if (this.m_styleBuildings[style].ContainsKey(index))
                    {
                        fastList = this.m_styleBuildings[style][index];
                    }
                    else
                    {
                        fastList = null;
                    }
                }
                else
                {
                    fastList = areaBuildings[index]; //pull areabuildings from this class
                }
                */
            }
            else
            {
                fastList = areaBuildings[index];
            }

            fastList = areaBuildings[index];

            if (fastList == null)
            {
                return null;
            }
            if (fastList.m_size == 0)
            {
                return null;
            }
            index = r.Int32((uint)fastList.m_size);
            return PrefabCollection<BuildingInfo>.GetPrefab((uint)fastList.m_buffer[index]);
        }


        //This returns the index for the array that contains the prefab groups. Needs to account for larger lots and new densities. 
        private static int GetExtendedAreaIndex(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, int density, int width, int length, BuildingInfo.ZoningMode zoningMode)
        {

            int widthcount = 16;
            int lengthcount = 16;

            int modes = 3;
            int leveloffset = widthcount * lengthcount * modes; //each level is 256 lots x 3 modes
            int modeoffset = widthcount * lengthcount; //each mode bracket is 256

            //lets manualy set service index offsets so we can better rearrange things later if need be

            //int resoffset = 0;
            //int comOffset = 16000;
            //int indOffset = 26000;
            //int offOffset = 36000;
            //int indspecOffset = 46000;
            //int comLesOffset = 58000;
            //int comTourOffset = 62000;


            int serviceoffset = 0;

            if (service == ItemClass.Service.Residential) {
                serviceoffset = 0;
            }
            else if (service == ItemClass.Service.Commercial) {

                if (subService == ItemClass.SubService.CommercialLeisure) serviceoffset = 58000;
                else if (subService == ItemClass.SubService.CommercialTourist) serviceoffset = 62000;
                else  serviceoffset = 16000;
            }
            else if (service == ItemClass.Service.Industrial) {

                if (subService == ItemClass.SubService.IndustrialGeneric) serviceoffset = 26000;
                //if speical subservice, level 1 = extractor, level 2 = processing. 
                else serviceoffset = 46000;

            }
            else if (service == ItemClass.Service.Office) {
                serviceoffset = 36000;
            }


            int zonemode = (int)zoningMode;
            int wealthlevel = (int)level + 1;

            int index = serviceoffset; //start at service offset

            index = index + density * (leveloffset * wealthlevel) ; // add density offset
            index = index + (leveloffset * wealthlevel); // add level offset
            index = index + (modeoffset * zonemode); // add mode offset
            index = index + width * widthcount; //width offset
            index = index + length; //lenth offset

            return index; 
        }

    }

}
