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

        //clear array, and assign 10x larger array to hold larger values. Original was 3040. 
           FastList<ushort>[] areaBuildings = new FastList<ushort>[30400];


            for (int i = 0; i < infos.Length; i++)
            {
                BuildingInfo info = infos[i];
                if (info != null && info.m_class.m_service != ItemClass.Service.None && info.m_placementStyle == ItemClass.Placement.Automatic && (!info.m_dontSpawnNormally || style > 0))
                {
                    int privateServiceIndex = ItemClass.GetPrivateServiceIndex(info.m_class.m_service);
                    if (privateServiceIndex != -1)
                    {  //modified to account for 8 deep lots. 
                        if (info.GetWidth() < 1 || info.GetWidth() > 8) //was 4
                        {
                            //removed so it wont stop execution. 
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

                        }//modified to account for 8 deep lots. 
                        else if (info.GetLength() < 1 || info.GetLength() > 8) //was 4
                        {
                            //removed so it wont stop execution. 
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
                            int areaIndex = GetAreaIndex(info.m_class.m_service, info.m_class.m_subService, info.m_class.m_level, info.GetWidth(), info.GetLength(), info.m_zoningMode);
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
                //5 levels
                for (int k = 0; k < 5; k++)
                {
                   //modified, original was 4
                    for (int l = 0; l < 8; l++)
                    {
                        //modified, original was 4
                        for (int m = 1; m < 8; m++)
                        {
                            //modified, original was 4
                            int num2 = j;
                            num2 = num2 * 5 + k;
                            num2 = num2 * 8 + l; // was 4
                            num2 = num2 * 8 + m; // was 4
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
            for (int index = 0; index < infos.Length; index++)
            {
                BuildingInfo info = infos[index];

                if (info != null && info.m_class.m_service != ItemClass.Service.None && info.m_placementStyle == ItemClass.Placement.Automatic && !info.m_dontSpawnNormally)
                {
                    int privateServiceIndex2 = ItemClass.GetPrivateServiceIndex(info.m_class.m_service);
                    if (privateServiceIndex2 != -1)
                    {//modified
                        if (info.GetWidth() >= 1 && info.GetWidth() <= 8) //was 4
                        {
                            if (info.GetLength() >= 1 && info.GetLength() <= 8) //was 4
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
                                    int areaIndex2 = GetAreaIndex(info.m_class.m_service, info.m_class.m_subService, info.m_class.m_level + 1, info.GetWidth(), info.GetLength(), info.m_zoningMode);
                                    if (areaBuildings[areaIndex2] == null)
                                    {
                                        //removed so it wont stop execution. 
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
                                    int areaIndex3 = GetAreaIndex( info.m_class.m_service, info.m_class.m_subService, info.m_class.m_level - 1, info.m_cellWidth, info.m_cellLength, info.m_zoningMode);
                                    if (areaBuildings[areaIndex3] == null)
                                    {
                                        // removed so it wont stop execution.
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

            //Save private values via reflection. 
            SetBuildingsRefreshed(ref _this);
            SetAreaBuildings(ref _this, areaBuildings);
        }

        //Reason for detour: This class returns the index for the array that contains the prefab groups. Needs to account for larger lots. 
        [RedirectMethod(true)]
        private static int GetAreaIndex(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, int width, int length, BuildingInfo.ZoningMode zoningMode)
        {
            Debug.Log("GetAreaDetour called");

            int privateSubServiceIndex = ItemClass.GetPrivateSubServiceIndex(subService);
            int num;
            if (privateSubServiceIndex != -1)
            {
                //modified from 8 to 16
                num = 16 + privateSubServiceIndex;
            }
            else
            {
                num = ItemClass.GetPrivateServiceIndex(service);
            }
            num = (int)(num * 5 + level);
            if (zoningMode == BuildingInfo.ZoningMode.CornerRight)
            {
                //modified to 8. 
                num = num * 8 + length - 1; //was 4
                num = num * 8 + width - 1; //was 4
                num = num * 2 + 1;
            }
            else
            {
                //modified to 8. 
                num = num * 8 + width - 1; //was 4
                num = num * 8 + length - 1; //was 4
                num = (int)(num * 2 + zoningMode);
            }
            return num;
        }

    }

}
