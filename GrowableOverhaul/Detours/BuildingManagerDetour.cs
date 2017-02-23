using ColossalFramework;
using ColossalFramework.Threading;
using GrowableOverhaul.Redirection;
using UnityEngine;
using System.Reflection;
using ColossalFramework.Math;

namespace GrowableOverhaul
{
    [TargetType(typeof(BuildingManager))]

    public static class BuildingManagerDetour
    {
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

        //This methods reads all prefabs, and groups them based on service, sub service, size, ect... Detoured to account for larger lots
        [RedirectMethod(true)]
        public static void ApplyRefreshBuildings(BuildingManager _this, BuildingInfo[] infos, ushort[] indices, int style)
        {
            Debug.Log("ApplyRefreshBuildings called");

            //FastList<ushort>[] areaBuildings = GetAreaBuildings(ref _this);

            //if (areaBuildings.Length == 3040)
            //{
                FastList<ushort>[] areaBuildings = new FastList<ushort>[30400];
            //}

            for (int i = 0; i < infos.Length; i++)
            {
                BuildingInfo info = infos[i];
                if (info != null && info.m_class.m_service != ItemClass.Service.None && info.m_placementStyle == ItemClass.Placement.Automatic && (!info.m_dontSpawnNormally || style > 0))
                {
                    int privateServiceIndex = ItemClass.GetPrivateServiceIndex(info.m_class.m_service);
                    if (privateServiceIndex != -1)
                    {   //increase from 4 to 16
                        if (info.GetWidth() < 1 || info.GetWidth() > 8)
                        {
                            /*
                            ThreadHelper.dispatcher.Dispatch(delegate
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat(new object[]
                                {
                            "Invalid width (",
                            info.gameObject.name,
                            "): ",
                            info.m_cellWidth
                                }), info.gameObject);
                            });
                            */
                            continue;
                        }//increase from 4 to 16
                        else if (info.GetLength() < 1 || info.GetLength() > 8)
                        {
                            /*
                            ThreadHelper.dispatcher.Dispatch(delegate
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, string.Concat(new object[]
                                {
                            "Invalid length (",
                            info.gameObject.name,
                            "): ",
                            info.m_cellLength
                                }), info.gameObject);
                            });
                            */
                            continue;
                        }
                        else
                        {
                            int areaIndex = GetAreaIndex2( info.m_class.m_service, info.m_class.m_subService, info.m_class.m_level, info.GetWidth(), info.GetLength(), info.m_zoningMode);
                            Debug.Log(info.name + " AreaIndex is: " + areaIndex);

                            if (areaBuildings[areaIndex] == null)
                            {
                                areaBuildings[areaIndex] = new FastList<ushort>();
                            }
                            areaBuildings[areaIndex].Add(indices[i]);
                        }
                    }
                }
            }

            int num = 19;
            for (int j = 0; j < num; j++)
            {
                // 5 levels
                for (int k = 0; k < 5; k++)
                {
                    // 8 widths old was 4
                    for (int l = 0; l < 8; l++)
                    {
                        // old was 4
                        for (int m = 1; m < 8; m++)
                        {
                            //old was 4
                            int num2 = j;
                            num2 = num2 * 5 + k;
                            num2 = num2 * 8 + l;
                            num2 = num2 * 8 + m;
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
            for (int num3 = 0; num3 < infos.Length; num3++)
            {
                BuildingInfo info = infos[num3];

                //Debug.Log(info.name);

                if (info != null && info.m_class.m_service != ItemClass.Service.None && info.m_placementStyle == ItemClass.Placement.Automatic && !info.m_dontSpawnNormally)
                {
                    int privateServiceIndex2 = ItemClass.GetPrivateServiceIndex(info.m_class.m_service);
                    if (privateServiceIndex2 != -1)
                    {//increase from 4 to 16
                        if (info.GetWidth() >= 1 && info.GetWidth() <= 8)
                        {//increase from 4 to 16
                            if (info.GetLength() >= 1 && info.GetLength() <= 8)
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
                                    int areaIndex2 = GetAreaIndex2(info.m_class.m_service, info.m_class.m_subService, info.m_class.m_level + 1, info.GetWidth(), info.GetLength(), info.m_zoningMode);
                                    if (areaBuildings[areaIndex2] == null)
                                    {
                                        /*
                                        ThreadHelper.dispatcher.Dispatch(delegate
                                        {
                                            CODebugBase<LogChannel>.Warn(LogChannel.Core, "Building cannot upgrade to next level: " + info.gameObject.name, info.gameObject);
                                        });
                                        */
                                        continue;
                                    }
                                }
                                if (info.m_class.m_level > level)
                                {
                                    int areaIndex3 = GetAreaIndex2( info.m_class.m_service, info.m_class.m_subService, info.m_class.m_level - 1, info.m_cellWidth, info.m_cellLength, info.m_zoningMode);
                                    if (areaBuildings[areaIndex3] == null)
                                    {
                                        /*
                                        ThreadHelper.dispatcher.Dispatch(delegate
                                        {
                                            CODebugBase<LogChannel>.Warn(LogChannel.Core, "There is no building that would upgrade to: " + info.gameObject.name, info.gameObject);
                                        });
                                        */
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //Save private values
            SetBuildingsRefreshed(ref _this);
            SetAreaBuildings(ref _this, areaBuildings);

            // Print test 
            /*
            var counter = 0;
            foreach (var array in areaBuildings) {

                if (array != null)
                {
                    Debug.Log("Index is: " + counter);

                    foreach (var buildings in array)
                    {  
                        //Debug.Log("ID is: " + buildings);
                        Debug.Log(PrefabCollection<BuildingInfo>.GetPrefab((uint)buildings).name);
                    }
                }
                counter++;
            }
           
            var index = GetAreaIndex2(ItemClass.Service.Residential, ItemClass.SubService.ResidentialLow, ItemClass.Level.Level2,

                2, 2, BuildingInfo.ZoningMode.Straight);

            Debug.Log("2x2 Test Area Index = " + index);

            //L2 2x2 Detached05 AreaIndex is: 10386
             */

                            Debug.Log("End of AreaBuildings");
        }

        [RedirectMethod(true)]
        private static int GetAreaIndex(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, int width, int length, BuildingInfo.ZoningMode zoningMode)
        {
            Debug.Log("GetAreaDetour called");

            int privateSubServiceIndex = ItemClass.GetPrivateSubServiceIndex(subService);
            int num;
            if (privateSubServiceIndex != -1)
            {
                // was 8
                num = 16 + privateSubServiceIndex;
            }
            else
            {
                num = ItemClass.GetPrivateServiceIndex(service);
            }
            num = (int)(num * 5 + level);
            if (zoningMode == BuildingInfo.ZoningMode.CornerRight)
            {
                num = num * 8 + length - 1;
                num = num * 8 + width - 1;
                num = num * 2 + 1;
            }
            else
            {
                // old num = num * 4 + width - 1;
                num = num * 8 + width - 1;
                num = num * 8 + length - 1;
                num = (int)(num * 2 + zoningMode);
            }
            return num;
        }


        private static int GetAreaIndex2(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, int width, int length, BuildingInfo.ZoningMode zoningMode)
        {
            int privateSubServiceIndex = ItemClass.GetPrivateSubServiceIndex(subService);
            int num;
            if (privateSubServiceIndex != -1)
            {
                num = 16 + privateSubServiceIndex;
            }
            else
            {
                num = ItemClass.GetPrivateServiceIndex(service);
            }
            num = (int)(num * 5 + level);
            if (zoningMode == BuildingInfo.ZoningMode.CornerRight)
            {
                num = num * 8 + length - 1;
                num = num * 8 + width - 1;
                num = num * 2 + 1;
            }
            else
            {
                // old num = num * 4 + width - 1;
                num = num * 8 + width - 1;
                num = num * 8 + length - 1;
                num = (int)(num * 2 + zoningMode);
            }
            return num;
        }

        //[RedirectMethod(true)]
        public static BuildingInfo GetRandomBuildingInfo(BuildingManager _this, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, int width, int length, BuildingInfo.ZoningMode zoningMode, int style)
        {
            Debug.Log("GetRandom Detour Called");

            int num = GetAreaIndex(service, subService, level, width, length, zoningMode);

            Debug.Log("AreaIndex = " + num);

            //FastList<ushort> fastList;
            //FastList<ushort>[] areaBuildings = GetAreaBuildings(ref _this);

            var values = GetAreaBuildings(ref _this);

            var fastList = values[num];

           // var counter = 0;
            //foreach (var ID in fastList) {

                //Debug.Log("ID is :" + fastList[counter]);
               // counter++;
            //}
           
            if (fastList == null)
            {
                Debug.Log("List is null");
                return null;
            }
            if (fastList.m_size == 0)
            {
                Debug.Log("Size is 0");
                return null;
            }
            //num = r.Int32((uint)fastList.m_size);

            return PrefabCollection<BuildingInfo>.GetPrefab(fastList[0]);
        }

    }


}
