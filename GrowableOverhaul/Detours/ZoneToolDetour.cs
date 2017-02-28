using System.Reflection;
using ColossalFramework.Math;
using GrowableOverhaul.Redirection;
using UnityEngine;
using ColossalFramework;
using System.Threading;
namespace GrowableOverhaul
{
    [TargetType(typeof(ZoneTool))]
    public static class ZoneToolDetour
    {
        //This is set from ZoningPanel when player clicks on zoning button. Change all refs from m_zone to this. 
        public static ExtendedItemClass.Zone ExtendedZone;

        //only read and assigned by CalculateFillBuffer. 
        private static FastList<FillPos> fillPositions;

        private static FieldInfo m_dataLock_field;

        private static FieldInfo m_fillBuffer1_field;
        private static FieldInfo m_fillBuffer2_field;
        private static FieldInfo m_fillBuffer3_field;

        private static FieldInfo m_zoning_field;
        private static FieldInfo m_dezoning_field;

        private static FieldInfo m_mouseRay_field;
        private static FieldInfo m_mousePosition_field;
        private static FieldInfo m_mouseRayValid_field;
        private static FieldInfo m_mouseRayLength_field;
        private static FieldInfo m_mouseDirection_field;

        private static FieldInfo m_cameraDirection_field;

        private static FieldInfo m_closeSegmentCount_field;
        private static FieldInfo m_closeSegments_field;

        private static FieldInfo m_validPosition_field;
        private static FieldInfo m_startPosition_field;
        private static FieldInfo m_startDirection_field;

        private static FieldInfo m_toolController_field;

        private static FieldInfo ToolCursor_field;



        private static void FindFieldInfos()
        {
            if (m_fillBuffer1_field == null || m_zoning_field == null || m_dezoning_field == null)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

                m_dataLock_field = typeof(ZoneTool).GetField("m_dataLock", flags);

                m_fillBuffer1_field = typeof (ZoneTool).GetField("m_fillBuffer1", flags);
                m_fillBuffer2_field = typeof(ZoneTool).GetField("m_fillBuffer2", flags);
                m_fillBuffer3_field = typeof(ZoneTool).GetField("m_fillBuffer3", flags);

                m_zoning_field = typeof(ZoneTool).GetField("m_zoning", flags);
                m_dezoning_field = typeof(ZoneTool).GetField("m_dezoning", flags);

                m_mouseRay_field = typeof(ZoneTool).GetField("m_mouseRay", flags);
                m_mousePosition_field = typeof(ZoneTool).GetField("m_mousePosition", flags);
                m_mouseRayValid_field = typeof(ZoneTool).GetField("m_mouseRayValid", flags);
                m_mouseRayLength_field = typeof(ZoneTool).GetField("m_mouseRayLength", flags);
                m_mouseDirection_field = typeof(ZoneTool).GetField("m_mouseDirection", flags);

                m_cameraDirection_field = typeof(ZoneTool).GetField("m_cameraDirection", flags);

                m_closeSegmentCount_field = typeof(ZoneTool).GetField("m_closeSegmentCount", flags);
                m_closeSegments_field = typeof(ZoneTool).GetField("m_closeSegments", flags);

                m_validPosition_field = typeof(ZoneTool).GetField("m_validPosition", flags);
                m_startPosition_field = typeof(ZoneTool).GetField("m_startPosition", flags);
                m_startDirection_field = typeof(ZoneTool).GetField("m_startDirection", flags);

                m_toolController_field = typeof(ZoneTool).GetField("m_toolController", flags);

                ToolCursor_field = typeof(ZoneTool).GetField("ToolCursor", flags);
            }
        }


        private static ToolController GettoolController(ZoneTool _this)
        {
            FindFieldInfos();
            return (ToolController)m_toolController_field.GetValue(_this);
        }

        private static ulong[] GetFillBuffer1(ZoneTool _this)
        {
            FindFieldInfos();
            return (ulong[])m_fillBuffer1_field.GetValue(_this);
        }
        private static ulong[] GetFillBuffer2(ZoneTool _this)
        {
            FindFieldInfos();
            return (ulong[])m_fillBuffer2_field.GetValue(_this);
        }
        private static ulong[] GetFillBuffer3(ZoneTool _this)
        {
            FindFieldInfos();
            return (ulong[])m_fillBuffer3_field.GetValue(_this);
        }

        private static void AssignFillBuffer1(ZoneTool _this, ulong[] data)
        {
            FindFieldInfos();
            m_fillBuffer1_field.SetValue(_this, data);
        }
        private static void AssignFillBuffer2(ZoneTool _this, ulong[] data)
        {
            FindFieldInfos();
            m_fillBuffer2_field.SetValue(_this, data);
        }
        private static void AssignFillBuffer3(ZoneTool _this, ulong[] data)
        {
            FindFieldInfos();
            m_fillBuffer3_field.SetValue(_this, data);
        }

        private static bool GetZoning(ZoneTool _this)
        {
            FindFieldInfos();
            return (bool) m_zoning_field.GetValue(_this);
        }
        private static bool GetDezoning(ZoneTool _this)
        {
            FindFieldInfos();
            return (bool)m_dezoning_field.GetValue(_this);
        }

        private static object GetDataLock(ZoneTool _this) {

            FindFieldInfos();
            return (object)m_dataLock_field.GetValue(_this);
        }

        private static bool GetValidPosition(ZoneTool _this)
        {
            FindFieldInfos();
            return (bool)m_validPosition_field.GetValue(_this);
        }

        private static Ray GetMouseRay(ZoneTool _this) {

            FindFieldInfos();
            return (Ray)m_mouseRay_field.GetValue(_this);
        }
        private static float GetMouseRayLength(ZoneTool _this)
        {
            FindFieldInfos();
            return (float)m_mouseRayLength_field.GetValue(_this);
        }
        private static bool GetMouseRayValid(ZoneTool _this)
        {
            FindFieldInfos();
            return (bool)m_mouseRayValid_field.GetValue(_this);
        }
        private static Vector3 GetMousePosition(ZoneTool _this)
        {
            FindFieldInfos();
            return (Vector3)m_mousePosition_field.GetValue(_this);
        }
        private static Vector3 GetMouseDirection(ZoneTool _this)
        {
            FindFieldInfos();
            return (Vector3)m_mouseDirection_field.GetValue(_this);
        }

        private static void AssignMouseDirection(ref ZoneTool _this, Vector3 data)
        {

            FindFieldInfos();
            m_mouseDirection_field.SetValue(_this, data);
        }
        private static void AssignMousePosition(ref ZoneTool _this, Vector3 data)
        {

            FindFieldInfos();
            m_mousePosition_field.SetValue(_this, data);
        }


        private static Vector3 GetCameraDirection(ZoneTool _this)
        {
            FindFieldInfos();
            return (Vector3)m_cameraDirection_field.GetValue(_this);
        }

        private static int GetcloseSegmentCount(ZoneTool _this)
        {
            FindFieldInfos();
            return (int)m_closeSegmentCount_field.GetValue(_this);
        }
        private static ushort[] GetCloseSegments(ZoneTool _this)
        {
            FindFieldInfos();
            return (ushort[])m_closeSegments_field.GetValue(_this);
        }

        private static void AssignValidPosition(ref ZoneTool _this, bool data ) {

            FindFieldInfos();
            m_validPosition_field.SetValue(_this, data);
        }
        private static Vector3 GetStartPosition(ZoneTool _this)
        {
            FindFieldInfos();
            return (Vector3)m_startPosition_field.GetValue(_this);
        }
        private static Vector3 GetStartDirection(ZoneTool _this)
        {
            FindFieldInfos();
            return (Vector3)m_startDirection_field.GetValue(_this);
        }

        private static void AssignToolCursor(ref ZoneTool _this, CursorInfo data)
        {
            FindFieldInfos();
            ToolCursor_field.SetValue(_this, data);
        }



        //Begin Detours!

        [RedirectMethod(true)]
        private static void SimulationStep(ZoneTool _this)
        {
            int closeSegmentCount = GetcloseSegmentCount(_this);

            while (!Monitor.TryEnter( GetDataLock(_this), SimulationManager.SYNCHRONIZE_TIMEOUT))
            {
            }
            Ray mouseRay;
            Vector3 cameraDirection;
            bool mouseRayValid;
            try
            {
                mouseRay = GetMouseRay(_this);
                cameraDirection = GetCameraDirection(_this);
                mouseRayValid = GetMouseRayValid(_this);
            }
            finally
            {
                Monitor.Exit(GetDataLock(_this));
            }
            if (_this.m_mode == ZoneTool.Mode.Fill)
            {
                GuideController properties = Singleton<GuideManager>.instance.m_properties;
                if (properties != null)
                {
                    Singleton<ZoneManager>.instance.m_optionsNotUsed.Activate(properties.m_zoneOptionsNotUsed);
                }
            }
            else
            {
                GenericGuide optionsNotUsed = Singleton<ZoneManager>.instance.m_optionsNotUsed;
                if (optionsNotUsed != null && !optionsNotUsed.m_disabled)
                {
                    optionsNotUsed.Disable();
                }
            }
            ToolBase.RaycastInput input = new ToolBase.RaycastInput(mouseRay, GetMouseRayLength(_this));
            ToolBase.RaycastOutput raycastOutput;
            raycastOutput.m_hitPos = new Vector3();

            if (mouseRayValid && RayCast(input, out raycastOutput))
            {
                switch (_this.m_mode)
                {
                    case ZoneTool.Mode.Select:
                        if (!GetZoning(_this) && !GetDezoning(_this))
                        {
                            

                            Singleton<NetManager>.instance.GetClosestSegments(raycastOutput.m_hitPos, GetCloseSegments(_this), out closeSegmentCount);


                            float num = 256f;
                            ushort num2 = 0;
                            for (int i = 0; i < closeSegmentCount; i++)
                            {
                                Singleton<NetManager>.instance.m_segments.m_buffer[(int)GetCloseSegments(_this)[i]].GetClosestZoneBlock(raycastOutput.m_hitPos, ref num, ref num2);
                            }
                            if (num2 != 0)
                            {
                                ZoneBlock zoneBlock = Singleton<ZoneManager>.instance.m_blocks.m_buffer[(int)num2];
                                Vector3 forward = Vector3.forward;

                                //change
                                ExtendedItemClass.Zone zone = ExtendedItemClass.Zone.Unzoned;
                                bool flag = false;
                                bool flag2 = false;

                                //chage this
                                Snap(_this, ref raycastOutput.m_hitPos, ref forward, ref zone, ref flag, ref flag2, ref zoneBlock);

                                while (!Monitor.TryEnter(GetDataLock(_this), SimulationManager.SYNCHRONIZE_TIMEOUT))
                                {
                                }
                                try
                                {
                                    AssignMouseDirection(ref _this, forward);
                                    AssignMousePosition(ref _this, raycastOutput.m_hitPos);
                                    AssignValidPosition(ref _this, true);
                                }
                                finally
                                {
                                    Monitor.Exit(GetDataLock(_this));
                                }
                            }
                            else
                            {
                                while (!Monitor.TryEnter(GetDataLock(_this), SimulationManager.SYNCHRONIZE_TIMEOUT))
                                {
                                }
                                try
                                {
                                    AssignMouseDirection(ref _this, cameraDirection);
                                    AssignMousePosition(ref _this, raycastOutput.m_hitPos);
                                    AssignValidPosition(ref _this, true);
                                }
                                finally
                                {
                                    Monitor.Exit(GetDataLock(_this));
                                }
                            }
                        }
                        else
                        {
                            while (!Monitor.TryEnter(GetDataLock(_this), SimulationManager.SYNCHRONIZE_TIMEOUT))
                            {
                            }
                            try
                            {
                                AssignMousePosition(ref _this, raycastOutput.m_hitPos);
                                AssignValidPosition(ref _this, true);
                            }
                            finally
                            {
                                Monitor.Exit(GetDataLock(_this));
                            }
                        }
                        break;
                    case ZoneTool.Mode.Brush:
                        while (!Monitor.TryEnter(GetDataLock(_this), SimulationManager.SYNCHRONIZE_TIMEOUT))
                        {
                        }
                        try
                        {
                            AssignMousePosition(ref _this, raycastOutput.m_hitPos);
                            AssignValidPosition(ref _this, true);
                        }
                        finally
                        {
                            Monitor.Exit(GetDataLock(_this));
                        }
                        if (GetZoning(_this) != GetDezoning(_this))
                        {
                            ApplyBrush(_this);
                        }
                        break;
                    case ZoneTool.Mode.Fill:
                        {

                            Singleton<NetManager>.instance.GetClosestSegments(raycastOutput.m_hitPos, GetCloseSegments(_this), out closeSegmentCount);
                            float num3 = 256f;
                            ushort num4 = 0;
                            for (int j = 0; j < closeSegmentCount; j++)
                            {
                                Singleton<NetManager>.instance.m_segments.m_buffer[(int)GetCloseSegments(_this)[j]].GetClosestZoneBlock(raycastOutput.m_hitPos, ref num3, ref num4);
                            }
                            if (num4 != 0)
                            {
                                ZoneBlock zoneBlock2 = Singleton<ZoneManager>.instance.m_blocks.m_buffer[(int)num4];
                                Vector3 forward2 = Vector3.forward;
                                ExtendedItemClass.Zone requiredZone = ExtendedItemClass.Zone.Unzoned;
                                bool occupied = false;
                                bool occupied2 = false;

                                //Snap
                                Snap(_this, ref raycastOutput.m_hitPos, ref forward2, ref requiredZone, ref occupied, ref occupied2, ref zoneBlock2);

                                //Change
                                if (CalculateFillBuffer(_this, raycastOutput.m_hitPos, forward2, requiredZone, occupied, occupied2))
                                {
                                    while (!Monitor.TryEnter(GetDataLock(_this), SimulationManager.SYNCHRONIZE_TIMEOUT))
                                    {
                                    }
                                    try
                                    {
                                        for (int k = 0; k < 64; k++)
                                        {
                                            var fillbuffer2temp = GetFillBuffer2(_this);

                                            fillbuffer2temp[k] = GetFillBuffer1(_this)[k];

                                            AssignFillBuffer2(_this, fillbuffer2temp);
                                        }
                                        AssignMouseDirection(ref _this, forward2);
                                        AssignMousePosition(ref _this, raycastOutput.m_hitPos);
                                        AssignValidPosition(ref _this, true);
                                    }
                                    finally
                                    {
                                        Monitor.Exit(GetDataLock(_this));
                                    }
                                }
                                else
                                {
                                    AssignValidPosition(ref _this, false);
                                }
                            }
                            else
                            {
                                AssignValidPosition(ref _this, false);
                            }
                            break;
                        }
                }
            }
            else
            {
                AssignValidPosition(ref _this, false);
            }
        }

        private static bool RayCast(ToolBase.RaycastInput input, out ToolBase.RaycastOutput output)
        {
            Vector3 origin = input.m_ray.origin;
            Vector3 normalized = input.m_ray.direction.normalized;
            Vector3 vector = input.m_ray.origin + normalized * input.m_length;
            Segment3 ray = new Segment3(origin, vector);
            output.m_hitPos = vector;
            output.m_netNode = 0;
            output.m_netSegment = 0;
            output.m_building = 0;
            output.m_propInstance = 0;
            output.m_treeInstance = 0u;
            output.m_vehicle = 0;
            output.m_parkedVehicle = 0;
            output.m_citizenInstance = 0;
            output.m_transportLine = 0;
            output.m_transportStopIndex = 0;
            output.m_transportSegmentIndex = 0;
            output.m_district = 0;
            output.m_disaster = 0;
            output.m_currentEditObject = false;
            bool result = false;
            float num = input.m_length;
            Vector3 vector2;
            if (!input.m_ignoreTerrain && Singleton<TerrainManager>.instance.RayCast(ray, out vector2))
            {
                float num2 = Vector3.Distance(vector2, origin) + 100f;
                if (num2 < num)
                {
                    output.m_hitPos = vector2;
                    result = true;
                    num = num2;
                }
            }
            if ((input.m_ignoreNodeFlags != NetNode.Flags.All || input.m_ignoreSegmentFlags != NetSegment.Flags.All) && Singleton<NetManager>.instance.RayCast(input.m_buildObject as NetInfo, ray, input.m_netSnap, input.m_netService.m_service, input.m_netService2.m_service, input.m_netService.m_subService, input.m_netService2.m_subService, input.m_netService.m_itemLayers, input.m_netService2.m_itemLayers, input.m_ignoreNodeFlags, input.m_ignoreSegmentFlags, out vector2, out output.m_netNode, out output.m_netSegment))
            {
                float num3 = Vector3.Distance(vector2, origin);
                if (num3 < num)
                {
                    output.m_hitPos = vector2;
                    result = true;
                    num = num3;
                }
                else
                {
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                }
            }
            if (input.m_ignoreBuildingFlags != Building.Flags.All && Singleton<BuildingManager>.instance.RayCast(ray, input.m_buildingService.m_service, input.m_buildingService.m_subService, input.m_buildingService.m_itemLayers, input.m_ignoreBuildingFlags, out vector2, out output.m_building))
            {
                float num4 = Vector3.Distance(vector2, origin);
                if (num4 < num)
                {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    result = true;
                    num = num4;
                }
                else
                {
                    output.m_building = 0;
                }
            }
            if (input.m_ignoreDisasterFlags != DisasterData.Flags.All && Singleton<DisasterManager>.instance.RayCast(ray, input.m_ignoreDisasterFlags, out vector2, out output.m_disaster))
            {
                float num5 = Vector3.Distance(vector2, origin);
                if (num5 < num)
                {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    output.m_building = 0;
                    result = true;
                    num = num5;
                }
                else
                {
                    output.m_disaster = 0;
                }
            }
            if (input.m_currentEditObject && Singleton<ToolManager>.instance.m_properties.RaycastEditObject(ray, out vector2))
            {
                float num6 = Vector3.Distance(vector2, origin);
                if (num6 < num)
                {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    output.m_building = 0;
                    output.m_disaster = 0;
                    output.m_currentEditObject = true;
                    result = true;
                    num = num6;
                }
            }
            if (input.m_ignorePropFlags != PropInstance.Flags.All && Singleton<PropManager>.instance.RayCast(ray, input.m_propService.m_service, input.m_propService.m_subService, input.m_propService.m_itemLayers, input.m_ignorePropFlags, out vector2, out output.m_propInstance))
            {
                float num7 = Vector3.Distance(vector2, origin) - 0.5f;
                if (num7 < num)
                {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    output.m_building = 0;
                    output.m_disaster = 0;
                    output.m_currentEditObject = false;
                    result = true;
                    num = num7;
                }
                else
                {
                    output.m_propInstance = 0;
                }
            }
            if (input.m_ignoreTreeFlags != global::TreeInstance.Flags.All && Singleton<TreeManager>.instance.RayCast(ray, input.m_treeService.m_service, input.m_treeService.m_subService, input.m_treeService.m_itemLayers, input.m_ignoreTreeFlags, out vector2, out output.m_treeInstance))
            {
                float num8 = Vector3.Distance(vector2, origin) - 1f;
                if (num8 < num)
                {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    output.m_building = 0;
                    output.m_disaster = 0;
                    output.m_propInstance = 0;
                    output.m_currentEditObject = false;
                    result = true;
                    num = num8;
                }
                else
                {
                    output.m_treeInstance = 0u;
                }
            }
            if ((input.m_ignoreVehicleFlags != (Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding | Vehicle.Flags.LeftHandDrive) || input.m_ignoreParkedVehicleFlags != VehicleParked.Flags.All) && Singleton<VehicleManager>.instance.RayCast(ray, input.m_ignoreVehicleFlags, input.m_ignoreParkedVehicleFlags, out vector2, out output.m_vehicle, out output.m_parkedVehicle))
            {
                float num9 = Vector3.Distance(vector2, origin) - 0.5f;
                if (num9 < num)
                {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    output.m_building = 0;
                    output.m_disaster = 0;
                    output.m_propInstance = 0;
                    output.m_treeInstance = 0u;
                    output.m_currentEditObject = false;
                    result = true;
                    num = num9;
                }
                else
                {
                    output.m_vehicle = 0;
                    output.m_parkedVehicle = 0;
                }
            }
            if (input.m_ignoreCitizenFlags != CitizenInstance.Flags.All && Singleton<CitizenManager>.instance.RayCast(ray, input.m_ignoreCitizenFlags, out vector2, out output.m_citizenInstance))
            {
                float num10 = Vector3.Distance(vector2, origin) - 0.5f;
                if (num10 < num)
                {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    output.m_building = 0;
                    output.m_disaster = 0;
                    output.m_propInstance = 0;
                    output.m_treeInstance = 0u;
                    output.m_vehicle = 0;
                    output.m_parkedVehicle = 0;
                    output.m_currentEditObject = false;
                    result = true;
                    num = num10;
                }
                else
                {
                    output.m_citizenInstance = 0;
                }
            }
            if (input.m_ignoreTransportFlags != TransportLine.Flags.All && Singleton<TransportManager>.instance.RayCast(input.m_ray, input.m_length, input.m_transportTypes, out vector2, out output.m_transportLine, out output.m_transportStopIndex, out output.m_transportSegmentIndex))
            {
                float num11 = Vector3.Distance(vector2, origin) - 2f;
                if (num11 < num)
                {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    output.m_building = 0;
                    output.m_disaster = 0;
                    output.m_propInstance = 0;
                    output.m_treeInstance = 0u;
                    output.m_vehicle = 0;
                    output.m_parkedVehicle = 0;
                    output.m_citizenInstance = 0;
                    output.m_currentEditObject = false;
                    result = true;
                }
                else
                {
                    output.m_transportLine = 0;
                }
            }
            if (input.m_ignoreDistrictFlags != District.Flags.All)
            {
                if (input.m_districtNameOnly)
                {
                    if (Singleton<DistrictManager>.instance.RayCast(ray, input.m_rayRight, out vector2, out output.m_district))
                    {
                        output.m_hitPos = vector2;
                    }
                }
                else
                {
                    output.m_district = Singleton<DistrictManager>.instance.SampleDistrict(output.m_hitPos);
                    if ((Singleton<DistrictManager>.instance.m_districts.m_buffer[(int)output.m_district].m_flags & input.m_ignoreDistrictFlags) != District.Flags.None)
                    {
                        output.m_district = 0;
                    }
                }
                if (output.m_district != 0)
                {
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    output.m_building = 0;
                    output.m_disaster = 0;
                    output.m_propInstance = 0;
                    output.m_treeInstance = 0u;
                    output.m_vehicle = 0;
                    output.m_parkedVehicle = 0;
                    output.m_citizenInstance = 0;
                    output.m_transportLine = 0;
                    output.m_transportStopIndex = 0;
                    output.m_transportSegmentIndex = 0;
                    output.m_currentEditObject = false;
                    result = true;
                }
            }
            return result;
        }

        //Called only from simulationstep
        private static bool CalculateFillBuffer(ZoneTool _this, Vector3 position, Vector3 direction, ExtendedItemClass.Zone requiredZone, bool occupied1, bool occupied2)
        {
            fillPositions = new FastList<FillPos>();

            //Debug.Log("Start CalculateFillBuffer");
            for (int i = 0; i < 64; i++)
            {
                var fillbuffer1 = GetFillBuffer1(_this);

                fillbuffer1[i] = 0uL;

                AssignFillBuffer1(_this, fillbuffer1);


            }
            //Debug.Log("CalculateFillBuffer After First Read");

            if (!occupied2)
            {
                float angle = Mathf.Atan2(-direction.x, direction.z);
                float num = position.x - 256f;
                float num2 = position.z - 256f;
                float num3 = position.x + 256f;
                float num4 = position.z + 256f;
                int num5 = Mathf.Max((int)((num - 46f) / 64f + 75f), 0);
                int num6 = Mathf.Max((int)((num2 - 46f) / 64f + 75f), 0);
                int num7 = Mathf.Min((int)((num3 + 46f) / 64f + 75f), 149);
                int num8 = Mathf.Min((int)((num4 + 46f) / 64f + 75f), 149);
                ZoneManager instance = Singleton<ZoneManager>.instance;
                for (int j = num6; j <= num8; j++)
                {
                    for (int k = num5; k <= num7; k++)
                    {
                        ushort num9 = instance.m_zoneGrid[j * 150 + k];
                        int num10 = 0;
                        while (num9 != 0)
                        {
                            Vector3 position2 = instance.m_blocks.m_buffer[(int)num9].m_position;
                            float num11 = Mathf.Max(Mathf.Max(num - 46f - position2.x, num2 - 46f - position2.z), Mathf.Max(position2.x - num3 - 46f, position2.z - num4 - 46f));
                            if (num11 < 0f)
                            {
                                //Changed call
                                CalculateFillBuffer(_this, position, direction, angle, num9, ref instance.m_blocks.m_buffer[(int)num9], requiredZone, occupied1, occupied2);
                            }
                            num9 = instance.m_blocks.m_buffer[(int)num9].m_nextGridBlock;
                            if (++num10 >= 49152)
                            {
                                //Debug.Log("CalculateFillBuffer Debug1");
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n");
                                break;
                            }
                        }
                    }
                }
            }
            //Debug.Log("CalculateFillBuffer Middle");

            if ((GetFillBuffer1(_this)[32] & 4294967296uL) != 0uL)
            {
                
                fillPositions.Clear();
                int l = 0;
                int num12 = 32;
                int num13 = 32;
                int num14 = 32;
                int num15 = 32;
                FillPos fillPos;
                fillPos.m_x = 32;
                fillPos.m_z = 32;
               fillPositions.Add(fillPos);

                //changed
                var fillBuffer1 = GetFillBuffer1(_this);
                fillBuffer1[32] &= 18446744069414584319uL;
                AssignFillBuffer1(_this, fillBuffer1);


                while (l < fillPositions.m_size)
                {
                    fillPos = fillPositions.m_buffer[l++];
                    if (fillPos.m_z > 0)
                    {
                        FillPos item = fillPos;
                        item.m_z -= 1;
                        if ((GetFillBuffer1(_this)[(int)item.m_z] & 1uL << (int)item.m_x) != 0uL)
                        {
                            fillPositions.Add(item);

                            //changed
                            fillBuffer1 = GetFillBuffer1(_this);
                            fillBuffer1[(int)item.m_z] &= ~(1uL << (int)item.m_x);
                            AssignFillBuffer1(_this, fillBuffer1);

                            if ((int)item.m_z < num13)
                            {
                                num13 = (int)item.m_z;
                            }
                        }
                    }
                    if (fillPos.m_x > 0)
                    {
                        FillPos item2 = fillPos;
                        item2.m_x -= 1;
                        if ((GetFillBuffer1(_this)[(int)item2.m_z] & 1uL << (int)item2.m_x) != 0uL)
                        {
                            fillPositions.Add(item2);

                            //changed
                            fillBuffer1 = GetFillBuffer1(_this);
                            fillBuffer1[(int)item2.m_z] &= ~(1uL << (int)item2.m_x);
                            AssignFillBuffer1(_this, fillBuffer1);

                            if ((int)item2.m_x < num12)
                            {
                                num12 = (int)item2.m_x;
                            }
                        }
                    }
                    if (fillPos.m_x < 63)
                    {
                        FillPos item3 = fillPos;
                        item3.m_x += 1;
                        if ((GetFillBuffer1(_this)[(int)item3.m_z] & 1uL << (int)item3.m_x) != 0uL)
                        {
                            fillPositions.Add(item3);

                            //changed
                            fillBuffer1 = GetFillBuffer1(_this);
                            fillBuffer1[(int)item3.m_z] &= ~(1uL << (int)item3.m_x);
                            AssignFillBuffer1(_this, fillBuffer1);

                            if ((int)item3.m_x > num14)
                            {
                                num14 = (int)item3.m_x;
                            }
                        }
                    }
                    if (fillPos.m_z < 63)
                    {
                        FillPos item4 = fillPos;
                        item4.m_z += 1;
                        if ((GetFillBuffer1(_this)[(int)item4.m_z] & 1uL << (int)item4.m_x) != 0uL)
                        {
                            fillPositions.Add(item4);

                            //changed
                            fillBuffer1 = GetFillBuffer1(_this);
                            fillBuffer1[(int)item4.m_z] &= ~(1uL << (int)item4.m_x);
                            AssignFillBuffer1(_this, fillBuffer1);

                            if ((int)item4.m_z > num15)
                            {
                                num15 = (int)item4.m_z;
                            }
                        }
                    }
                }
                for (int m = 0; m < 64; m++)

                {
                    fillBuffer1 = GetFillBuffer1(_this);
                    fillBuffer1[m] = 0uL;
                    AssignFillBuffer1(_this, fillBuffer1);

                }
                for (int n = 0; n < fillPositions.m_size; n++)
                {
                    FillPos fillPos2 = fillPositions.m_buffer[n];

                    fillBuffer1 = GetFillBuffer1(_this);
                    fillBuffer1[(int)fillPos2.m_z] |= 1uL << (int)fillPos2.m_x;
                    AssignFillBuffer1(_this, fillBuffer1);

                }
                return true;
            }
            for (int num16 = 0; num16 < 64; num16++)
            {
                var fillBuffer1 = GetFillBuffer1(_this);
                fillBuffer1[num16] = 0uL;
                AssignFillBuffer1(_this, fillBuffer1);
            }
            //Debug.Log("CalculateFillBuffer End");
            return false;
        }

        //Copied from ZoneTool. Only used in CalculateFillBuffer, so it was easier to just copy it. 
        private struct FillPos
        {
            public byte m_x;
            public byte m_z;
        }

        private static void CalculateFillBuffer(ZoneTool _this, Vector3 position, Vector3 direction, float angle, ushort blockIndex, ref ZoneBlock block, ExtendedItemClass.Zone requiredZone, bool occupied1, bool occupied2)
        {
            float f1 = Mathf.Abs(block.m_angle - angle) * 0.6366197f;
            float num1 = f1 - Mathf.Floor(f1);
            if ((double)num1 >= 0.00999999977648258 && (double)num1 <= 0.990000009536743)
                return;
            int rowCount = block.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref block); // modified
            Vector3 vector3_1 = new Vector3(Mathf.Cos(block.m_angle), 0.0f, Mathf.Sin(block.m_angle)) * 8f;
            Vector3 vector3_2 = new Vector3(vector3_1.z, 0.0f, -vector3_1.x);

            var m_fillBuffer1 = GetFillBuffer1(_this); // modified
            var blockID = ZoneBlockDetour.FindBlockId(ref block); // modified

            for (int z = 0; z < rowCount; ++z)
            {
                Vector3 vector3_3 = ((float)z - 3.5f) * vector3_2;
                for (int x = 0; x < columnCount; ++x) // modifed
                {
                    if (((long)block.m_valid & 1L << (z << 3 | x)) != 0L && ZoneBlockDetour.GetZoneDeep(ref block, blockID, x, z) == requiredZone)
                    {
                        if (occupied1)
                        {
                            if (requiredZone == ExtendedItemClass.Zone.Unzoned && ((long)block.m_occupied1 & 1L << (z << 3 | x)) == 0L)
                                continue;
                        }
                        else if (occupied2)
                        {
                            if (requiredZone == ExtendedItemClass.Zone.Unzoned && ((long)block.m_occupied2 & 1L << (z << 3 | x)) == 0L)
                                continue;
                        }
                        else if ((((long)block.m_occupied1 | (long)block.m_occupied2) & 1L << (z << 3 | x)) != 0L)
                            continue;
                        Vector3 vector3_4 = ((float)x - 3.5f) * vector3_1;
                        Vector3 vector3_5 = block.m_position + vector3_4 + vector3_3 - position;
                        float f2 = (float)(((double)vector3_5.x * (double)direction.x + (double)vector3_5.z * (double)direction.z) * 0.125 + 32.0);
                        float f3 = (float)(((double)vector3_5.x * (double)direction.z - (double)vector3_5.z * (double)direction.x) * 0.125 + 32.0);
                        int num2 = Mathf.RoundToInt(f2);
                        int index = Mathf.RoundToInt(f3);
                        if (num2 >= 0 && num2 < 64 && (index >= 0 && index < 64) && ((double)Mathf.Abs(f2 - (float)num2) < 0.0125000001862645 && (double)Mathf.Abs(f3 - (float)index) < 0.0125000001862645))
                            m_fillBuffer1[index] |= (ulong)(1L << num2);
                    }
                }
            }
        }

        //Called only from simulationstep
        private static void ApplyBrush(ZoneTool _this)
        {
            float num = _this.m_brushSize * 0.5f;

            Vector3 mousePosition = GetMousePosition(_this);

            float num2 = mousePosition.x - num;
            float num3 = mousePosition.z - num;
            float num4 = mousePosition.x + num;
            float num5 = mousePosition.z + num;
            ZoneManager instance = Singleton<ZoneManager>.instance;
            int num6 = Mathf.Max((int)((num2 - 46f) / 64f + 75f), 0);
            int num7 = Mathf.Max((int)((num3 - 46f) / 64f + 75f), 0);
            int num8 = Mathf.Min((int)((num4 + 46f) / 64f + 75f), 149);
            int num9 = Mathf.Min((int)((num5 + 46f) / 64f + 75f), 149);
            for (int i = num7; i <= num9; i++)
            {
                for (int j = num6; j <= num8; j++)
                {
                    ushort num10 = instance.m_zoneGrid[i * 150 + j];
                    int num11 = 0;
                    while (num10 != 0)
                    {
                        Vector3 position = instance.m_blocks.m_buffer[(int)num10].m_position;
                        float num12 = Mathf.Max(Mathf.Max(num2 - 46f - position.x, num3 - 46f - position.z), Mathf.Max(position.x - num4 - 46f, position.z - num5 - 46f));
                        if (num12 < 0f)
                        {
                            ApplyBrush(_this, num10, ref instance.m_blocks.m_buffer[(int)num10], mousePosition, num);
                        }
                        num10 = instance.m_blocks.m_buffer[(int)num10].m_nextGridBlock;
                        if (++num11 >= 49152)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n");
                            break;
                        }
                    }
                }
            }
        }

        private static void ApplyBrush(ZoneTool _this, ushort blockIndex, ref ZoneBlock data, Vector3 position, float brushRadius)
        {
            Vector3 vector3_1 = data.m_position - position;
            if ((double)Mathf.Abs(vector3_1.x) > 46.0 + (double)brushRadius || (double)Mathf.Abs(vector3_1.z) > 46.0 + (double)brushRadius)
                return;
            int num = (int)((data.m_flags & 65280U) >> 8);
            int columnCount = ZoneBlockDetour.GetColumnCount(ref data); // modified
            Vector3 vector3_2 = new Vector3(Mathf.Cos(data.m_angle), 0.0f, Mathf.Sin(data.m_angle)) * 8f;
            Vector3 vector3_3 = new Vector3(vector3_2.z, 0.0f, -vector3_2.x);
            bool flag = false;

            var m_zoning = GetZoning(_this); // custom
            var m_dezoning = GetDezoning(_this); // custom
            var blockID = ZoneBlockDetour.FindBlockId(ref data); // modified

            for (int z = 0; z < num; ++z)
            {
                Vector3 vector3_4 = ((float)z - 3.5f) * vector3_3;
                for (int x = 0; x < columnCount; ++x) // modified
                {
                    Vector3 vector3_5 = ((float)x - 3.5f) * vector3_2;
                    Vector3 vector3_6 = vector3_1 + vector3_5 + vector3_4;
                    if ((double)vector3_6.x * (double)vector3_6.x + (double)vector3_6.z * (double)vector3_6.z <= (double)brushRadius * (double)brushRadius)
                    {
                        if (m_zoning)
                        {
                            if ((ExtendedZone == ExtendedItemClass.Zone.Unzoned || ZoneBlockDetour.GetZoneDeep(ref data, blockID, x, z) == ExtendedItemClass.Zone.Unzoned)
                                && ZoneBlockDetour.SetZoneDeep(ref data, blockID, x, z, ExtendedZone))
                                flag = true;
                        }
                        else if (m_dezoning && ZoneBlockDetour.SetZoneDeep(ref data, blockID, x, z, ExtendedItemClass.Zone.Unzoned))
                            flag = true;
                    }
                }
            }
            if (!flag)
                return;
            data.RefreshZoning(blockIndex);
            if (!m_zoning)
                return;
            UsedZone(ExtendedZone);
        }

        /// <summary>
        /// Called only from simulationstep
        /// </summary>
        private static void Snap(ZoneTool _this, ref Vector3 point, ref Vector3 direction, ref ExtendedItemClass.Zone zone, ref bool occupied1, ref bool occupied2, ref ZoneBlock block)
        {
            direction = new Vector3(Mathf.Cos(block.m_angle), 0.0f, Mathf.Sin(block.m_angle));
            Vector3 vector3_1 = direction * 8f;
            Vector3 vector3_2 = new Vector3(vector3_1.z, 0.0f, -vector3_1.x);
            Vector3 vector3_3 = block.m_position + vector3_1 * 0.5f + vector3_2 * 0.5f;
            Vector2 vector2 = new Vector2(point.x - vector3_3.x, point.z - vector3_3.z);
            int num1 = Mathf.RoundToInt((float)(((double)vector2.x * (double)vector3_1.x + (double)vector2.y * (double)vector3_1.z) * (1.0 / 64.0)));
            int num2 = Mathf.RoundToInt((float)(((double)vector2.x * (double)vector3_2.x + (double)vector2.y * (double)vector3_2.z) * (1.0 / 64.0)));
            point.x = (float)((double)vector3_3.x + (double)num1 * (double)vector3_1.x + (double)num2 * (double)vector3_2.x);
            point.z = (float)((double)vector3_3.z + (double)num1 * (double)vector3_1.z + (double)num2 * (double)vector3_2.z);
            // changed from:
            // if (num1 < -4 || num1 >= 0 || (num2 < -4 || num2 >= 4))
            if (num1 < -4 || num1 >= 4 || (num2 < -4 || num2 >= 4))
                return;

            //zone = block.GetZone(num1 + 4, num2 + 4); // keep old method (it's only a single call)

            zone = ZoneBlockDetour.GetZoneDeep(ref block, ZoneBlockDetour.FindBlockId(ref block), num1 + 4, num2 + 4);

            occupied1 = block.IsOccupied1(num1 + 4, num2 + 4);
            occupied2 = block.IsOccupied2(num1 + 4, num2 + 4);
        }

        [RedirectMethod(true)]
        private static void ApplyFill(ZoneTool _this)
        {
            if (! GetValidPosition(_this) )
            {
                return;
            }
            Vector3 mousePosition = GetMousePosition(_this);
            Vector3 mouseDirection = GetMouseDirection(_this);
            float angle = Mathf.Atan2(-mouseDirection.x, mouseDirection.z);
            float num = mousePosition.x - 256f;
            float num2 = mousePosition.z - 256f;
            float num3 = mousePosition.x + 256f;
            float num4 = mousePosition.z + 256f;
            int num5 = Mathf.Max((int)((num - 46f) / 64f + 75f), 0);
            int num6 = Mathf.Max((int)((num2 - 46f) / 64f + 75f), 0);
            int num7 = Mathf.Min((int)((num3 + 46f) / 64f + 75f), 149);
            int num8 = Mathf.Min((int)((num4 + 46f) / 64f + 75f), 149);
            ZoneManager instance = Singleton<ZoneManager>.instance;
            bool flag = false;
            for (int i = num6; i <= num8; i++)
            {
                for (int j = num5; j <= num7; j++)
                {
                    ushort num9 = instance.m_zoneGrid[i * 150 + j];
                    int num10 = 0;
                    while (num9 != 0)
                    {
                        Vector3 position = instance.m_blocks.m_buffer[(int)num9].m_position;
                        float num11 = Mathf.Max(Mathf.Max(num - 46f - position.x, num2 - 46f - position.z), Mathf.Max(position.x - num3 - 46f, position.z - num4 - 46f));

                        //Call ApplyFillBuffer
                        if (num11 < 0f && ApplyFillBuffer(_this, mousePosition, mouseDirection, angle, num9, ref instance.m_blocks.m_buffer[(int)num9]))
                        {
                            flag = true;
                        }
                        num9 = instance.m_blocks.m_buffer[(int)num9].m_nextGridBlock;
                        if (++num10 >= 49152)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + "Environment.StackTrace");
                            break;
                        }
                    }
                }
            }
            if (flag)
            {
                if (GetZoning(_this))
                {
                    UsedZone(ExtendedZone);
                }
                EffectInfo fillEffect = instance.m_properties.m_fillEffect;
                if (fillEffect != null)
                {
                    InstanceID instance2 = default(InstanceID);
                    EffectInfo.SpawnArea spawnArea = new EffectInfo.SpawnArea(mousePosition, Vector3.up, 1f);
                    Singleton<EffectManager>.instance.DispatchEffect(fillEffect, instance2, spawnArea, Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup);
                }
            }
        }

        private static bool ApplyFillBuffer(ZoneTool _this, Vector3 position, Vector3 direction, float angle, ushort blockIndex, ref ZoneBlock block)
        {
            var m_zoning = GetZoning(_this); // custom
            var m_dezoning = GetDezoning(_this); // custom
            var blockID = ZoneBlockDetour.FindBlockId(ref block); // modified

            int rowCount = block.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref block); // modified

            Vector3 vector3_1 = new Vector3(Mathf.Cos(block.m_angle), 0.0f, Mathf.Sin(block.m_angle)) * 8f;
            Vector3 vector3_2 = new Vector3(vector3_1.z, 0.0f, -vector3_1.x);
            bool flag1 = false;
            for (int z = 0; z < rowCount; ++z)
            {
                Vector3 vector3_3 = ((float)z - 3.5f) * vector3_2;
                for (int x = 0; x < columnCount; ++x) // custom
                {
                    Vector3 vector3_4 = ((float)x - 3.5f) * vector3_1;
                    Vector3 vector3_5 = block.m_position + vector3_4 + vector3_3 - position;
                    float f1 = (float)(((double)vector3_5.x * (double)direction.x + (double)vector3_5.z * (double)direction.z) * 0.125 + 32.0);
                    float f2 = (float)(((double)vector3_5.x * (double)direction.z - (double)vector3_5.z * (double)direction.x) * 0.125 + 32.0);
                    int num1 = Mathf.Clamp(Mathf.RoundToInt(f1), 0, 63);
                    int num2 = Mathf.Clamp(Mathf.RoundToInt(f2), 0, 63);
                    bool flag2 = false;

                    var m_fillBuffer1 = GetFillBuffer1(_this); // modified

                    for (int index1 = -1; index1 <= 1 && !flag2; ++index1)
                    {
                        for (int index2 = -1; index2 <= 1 && !flag2; ++index2)
                        {
                            int num3 = num1 + index2;
                            int index3 = num2 + index1;
                            if (num3 >= 0 && num3 < 64 && (index3 >= 0 && index3 < 64) && (((double)f1 - (double)num3) * ((double)f1 - (double)num3) 
                                + ((double)f2 - (double)index3) * ((double)f2 - (double)index3) < 9.0 / 16.0 && ((long)m_fillBuffer1[index3] & 1L << num3) != 0L))
                            {
                                if (m_zoning)
                                {
                                    if ((ExtendedZone == ExtendedItemClass.Zone.Unzoned || ZoneBlockDetour.GetZoneDeep(ref block, blockID, x, z) == ExtendedItemClass.Zone.Unzoned) 
                                        && ZoneBlockDetour.SetZoneDeep(ref block, blockID, x, z, ExtendedZone))
                                        flag1 = true;
                                }
                                else if (m_dezoning && ZoneBlockDetour.SetZoneDeep(ref block, blockID, x, z, ExtendedItemClass.Zone.Unzoned))
                                    flag1 = true;
                                flag2 = true;
                            }
                        }
                    }
                }
            }
            if (!flag1)
                return false;
            block.RefreshZoning(blockIndex);
            return true;
        }

        [RedirectMethod(true)]
        private static void ApplyZoning(ZoneTool _this)
        {
            Vector2 a = VectorUtils.XZ( GetStartPosition(_this));
            Vector2 b = VectorUtils.XZ(GetMousePosition(_this));
            Vector2 a2 = VectorUtils.XZ(GetStartDirection(_this));
            Vector2 a3 = new Vector2(a2.y, -a2.x);
            float num = Mathf.Round(((b.x - a.x) * a2.x + (b.y - a.y) * a2.y) * 0.125f) * 8f;
            float num2 = Mathf.Round(((b.x - a.x) * a3.x + (b.y - a.y) * a3.y) * 0.125f) * 8f;
            float num3 = (num < 0f) ? -4f : 4f;
            float num4 = (num2 < 0f) ? -4f : 4f;
            Quad2 quad = default(Quad2);
            quad.a = a - a2 * num3 - a3 * num4;
            quad.b = a - a2 * num3 + a3 * (num2 + num4);
            quad.c = a + a2 * (num + num3) + a3 * (num2 + num4);
            quad.d = a + a2 * (num + num3) - a3 * num4;
            if (num3 == num4)
            {
                Vector2 b2 = quad.b;
                quad.b = quad.d;
                quad.d = b2;
            }
            Vector2 vector = quad.Min();
            Vector2 vector2 = quad.Max();
            ZoneManager instance = Singleton<ZoneManager>.instance;
            int num5 = Mathf.Max((int)((vector.x - 46f) / 64f + 75f), 0);
            int num6 = Mathf.Max((int)((vector.y - 46f) / 64f + 75f), 0);
            int num7 = Mathf.Min((int)((vector2.x + 46f) / 64f + 75f), 149);
            int num8 = Mathf.Min((int)((vector2.y + 46f) / 64f + 75f), 149);
            bool flag = false;
            for (int i = num6; i <= num8; i++)
            {
                for (int j = num5; j <= num7; j++)
                {
                    ushort num9 = instance.m_zoneGrid[i * 150 + j];
                    int num10 = 0;
                    while (num9 != 0)
                    {
                        Vector3 position = instance.m_blocks.m_buffer[(int)num9].m_position;
                        float num11 = Mathf.Max(Mathf.Max(vector.x - 46f - position.x, vector.y - 46f - position.z), Mathf.Max(position.x - vector2.x - 46f, position.z - vector2.y - 46f));
                        if (num11 < 0f && ApplyZoning(_this, num9, ref instance.m_blocks.m_buffer[(int)num9], quad))
                        {
                            flag = true;
                        }
                        num9 = instance.m_blocks.m_buffer[(int)num9].m_nextGridBlock;
                        if (++num10 >= 49152)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" );
                            break;
                        }
                    }
                }
            }
            if (flag)
            {
                if (GetZoning(_this))
                {
                    //pass new itemclass field
                    UsedZone(ExtendedZone);
                }
                EffectInfo fillEffect = instance.m_properties.m_fillEffect;
                if (fillEffect != null)
                {
                    InstanceID instance2 = default(InstanceID);
                    EffectInfo.SpawnArea spawnArea = new EffectInfo.SpawnArea((a + b) * 0.5f, Vector3.up, 1f);
                    Singleton<EffectManager>.instance.DispatchEffect(fillEffect, instance2, spawnArea, Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup);
                }
            }
        }

        private static bool ApplyZoning(ZoneTool _this, ushort blockIndex, ref ZoneBlock data, Quad2 quad2)
        {
            int rowCount = data.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref data); // modified

            Vector2 vector2_1 = new Vector2(Mathf.Cos(data.m_angle), Mathf.Sin(data.m_angle)) * 8f;
            Vector2 vector2_2 = new Vector2(vector2_1.y, -vector2_1.x);
            Vector2 vector2_3 = VectorUtils.XZ(data.m_position);
            if (!new Quad2()
            {
                a = (vector2_3 - 4f * vector2_1 - 4f * vector2_2),
                b = (vector2_3 + 4f * vector2_1 - 4f * vector2_2),
                c = (vector2_3 + 4f * vector2_1 + (float)(rowCount - 4) * vector2_2),
                d = (vector2_3 - 4f * vector2_1 + (float)(rowCount - 4) * vector2_2)
            }.Intersect(quad2))
                return false;
            bool flag = false;

            var m_zoning = GetZoning(_this); // custom
            var m_dezoning = GetDezoning(_this); // custom
            var blockID = ZoneBlockDetour.FindBlockId(ref data); // modified

            for (int z = 0; z < rowCount; ++z)
            {
                Vector2 vector2_4 = ((float)z - 3.5f) * vector2_2;
                for (int x = 0; x < columnCount; ++x) // custom
                {
                    Vector2 vector2_5 = ((float)x - 3.5f) * vector2_1;
                    Vector2 p = vector2_3 + vector2_5 + vector2_4;
                    if (quad2.Intersect(p))
                    {
                        if (m_zoning)
                        {
                            if ((ExtendedZone == ExtendedItemClass.Zone.Unzoned || ZoneBlockDetour.GetZoneDeep(ref data, blockID, x, z) == ExtendedItemClass.Zone.Unzoned) 

                                //change m_zone
                                && ZoneBlockDetour.SetZoneDeep(ref data, blockID, x, z, ExtendedZone))
                                flag = true;
                        }
                        else if (m_dezoning && ZoneBlockDetour.SetZoneDeep(ref data, blockID, x, z, ExtendedItemClass.Zone.Unzoned))
                            flag = true;
                    }
                }
            }
            if (!flag)
                return false;
            data.RefreshZoning(blockIndex);
            return true;
        }

        [RedirectMethod(true)]
        private static void OnToolUpdate(ZoneTool _this)
        {
            //Change to new itemclass
            ExtendedItemClass.Zone zone;
            if (ExtendedZone <= ExtendedItemClass.Zone.Unzoned || GetDezoning(_this))
            {
                zone = ExtendedItemClass.Zone.Unzoned;
            }
            else
            {
                zone = ExtendedZone;
            }
            if (_this.m_zoneCursors != null && (ExtendedItemClass.Zone)_this.m_zoneCursors.Length > zone)
            {
                //AssignToolCursor(ref _this, _this.m_zoneCursors[(int)zone]) ;
            }
        }

        [RedirectMethod(true)]
        private static void RenderOverlay(ZoneTool _this, RenderManager.CameraInfo cameraInfo)
        {
            while (!Monitor.TryEnter(GetDataLock(_this), SimulationManager.SYNCHRONIZE_TIMEOUT))
            {
            }
            bool zoning;
            bool dezoning;
            bool validPosition;
            Vector3 startPosition;
            Vector3 mousePosition;
            Vector3 startDirection;
            Vector3 mouseDirection;
            try
            {
                zoning = GetZoning(_this);
                dezoning = GetDezoning(_this);
                validPosition = GetValidPosition(_this);
                startPosition = GetStartPosition(_this);
                mousePosition = GetMousePosition(_this);
                startDirection = GetStartDirection(_this);
                mouseDirection = GetMouseDirection(_this);

                for (int i = 0; i < 64; i++)
                {
                    var fillbuffer3 = GetFillBuffer3(_this);

                    fillbuffer3[i] = GetFillBuffer2(_this)[i];

                    AssignFillBuffer3(_this, fillbuffer3);
                }
            }
            finally
            {
                Monitor.Exit(GetDataLock(_this));
            }
            if ((!zoning && !dezoning && !validPosition) || !Cursor.visible || GettoolController(_this).IsInsideUI)
            {

                RenderOverlay2(_this, cameraInfo);

                return;
            }
            Color color;
            if (zoning || dezoning)
            {
                color = Singleton<ZoneManager>.instance.m_properties.m_activeColor;
            }
            else if (ExtendedZone <= ExtendedItemClass.Zone.Unzoned || dezoning)
            {
                color = Singleton<ZoneManager>.instance.m_properties.m_unzoneColor;
            }
            else
            {
                //color = new Color { r = 1, g = 1, b = 1, a = 1 };
                //Grab zone colors from new color array. 
                Debug.Log("Color array index is: " + (int)ExtendedZone);
                color = NewZoneColorManager.NewColors[(int)ExtendedZone];

            //
            }
            switch (_this.m_mode)
            {
                case ZoneTool.Mode.Select:
                    {
                        Vector3 a = (!zoning && !dezoning) ? mousePosition : startPosition;
                        Vector3 vector = mousePosition;
                        Vector3 a2 = (!zoning && !dezoning) ? mouseDirection : startDirection;
                        Vector3 a3 = new Vector3(a2.z, 0f, -a2.x);
                        float num = Mathf.Round(((vector.x - a.x) * a2.x + (vector.z - a.z) * a2.z) * 0.125f) * 8f;
                        float num2 = Mathf.Round(((vector.x - a.x) * a3.x + (vector.z - a.z) * a3.z) * 0.125f) * 8f;
                        float num3 = (num < 0f) ? -4f : 4f;
                        float num4 = (num2 < 0f) ? -4f : 4f;
                        Quad3 quad = default(Quad3);
                        quad.a = a - a2 * num3 - a3 * num4;
                        quad.b = a - a2 * num3 + a3 * (num2 + num4);
                        quad.c = a + a2 * (num + num3) + a3 * (num2 + num4);
                        quad.d = a + a2 * (num + num3) - a3 * num4;
                        if (num3 != num4)
                        {
                            Vector3 b = quad.b;
                            quad.b = quad.d;
                            quad.d = b;
                        }
                        ToolManager expr_32C_cp_0 = Singleton<ToolManager>.instance;
                        expr_32C_cp_0.m_drawCallData.m_overlayCalls = expr_32C_cp_0.m_drawCallData.m_overlayCalls + 1;
                        Singleton<RenderManager>.instance.OverlayEffect.DrawQuad(cameraInfo, color, quad, -1f, 1025f, false, true);
                        break;
                    }
                case ZoneTool.Mode.Brush:
                    {
                        ToolManager expr_368_cp_0 = Singleton<ToolManager>.instance;
                        expr_368_cp_0.m_drawCallData.m_overlayCalls = expr_368_cp_0.m_drawCallData.m_overlayCalls + 1;
                        Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, color, mousePosition, _this.m_brushSize, -1f, 1025f, false, true);
                        break;
                    }
                case ZoneTool.Mode.Fill:
                    {
                        Vector3 a4 = mouseDirection;
                        Vector3 a5 = new Vector3(a4.z, 0f, -a4.x);
                        int num5 = -1;
                        ulong num6 = GetFillBuffer3(_this)[0];
                        for (int j = 0; j <= 64; j++)
                        {
                            int num7 = -1;
                            if (j == 64 || GetFillBuffer3(_this)[j] != num6)
                            {
                                if (num5 != -1)
                                {
                                    int num8 = j - 1;
                                    for (int k = 0; k <= 64; k++)
                                    {
                                        if (k == 64 || (num6 & 1uL << k) == 0uL)
                                        {
                                            if (num7 != -1)
                                            {
                                                int num9 = k - 1;
                                                Vector3 a6 = mousePosition + a4 * (float)((num7 - 32) * 8) + a5 * (float)((num5 - 32) * 8);
                                                Vector3 vector2 = mousePosition + a4 * (float)((num9 - 32) * 8) + a5 * (float)((num8 - 32) * 8);
                                                float num10 = Mathf.Round(((vector2.x - a6.x) * a4.x + (vector2.z - a6.z) * a4.z) * 0.125f) * 8f;
                                                float num11 = Mathf.Round(((vector2.x - a6.x) * a5.x + (vector2.z - a6.z) * a5.z) * 0.125f) * 8f;
                                                float num12 = (num10 < 0f) ? -4f : 4f;
                                                float num13 = (num11 < 0f) ? -4f : 4f;
                                                Quad3 quad2 = default(Quad3);
                                                quad2.a = a6 - a4 * num12 - a5 * num13;
                                                quad2.b = a6 - a4 * num12 + a5 * (num11 + num13);
                                                quad2.c = a6 + a4 * (num10 + num12) + a5 * (num11 + num13);
                                                quad2.d = a6 + a4 * (num10 + num12) - a5 * num13;
                                                if (num12 != num13)
                                                {
                                                    Vector3 b2 = quad2.b;
                                                    quad2.b = quad2.d;
                                                    quad2.d = b2;
                                                }
                                                ToolManager expr_61E_cp_0 = Singleton<ToolManager>.instance;
                                                expr_61E_cp_0.m_drawCallData.m_overlayCalls = expr_61E_cp_0.m_drawCallData.m_overlayCalls + 1;
                                                Singleton<RenderManager>.instance.OverlayEffect.DrawQuad(cameraInfo, color, quad2, -1f, 1025f, false, false);
                                            }
                                            num7 = -1;
                                        }
                                        else if (num7 == -1)
                                        {
                                            num7 = k;
                                        }
                                    }
                                }
                                if (j != 64 && GetFillBuffer3(_this)[j] != 0uL)
                                {
                                    num5 = j;
                                    num6 = GetFillBuffer3(_this)[j];
                                }
                                else
                                {
                                    num5 = -1;
                                }
                            }
                            else if (num5 == -1 && GetFillBuffer3(_this)[j] != 0uL)
                            {
                                num5 = j;
                                num6 = GetFillBuffer3(_this)[j];
                            }
                        }
                        break;
                    }
            }
            RenderOverlay2(_this, cameraInfo);
        }

        private static void RenderOverlay2(ZoneTool _this, RenderManager.CameraInfo cameraInfo)
        {
            if (!GettoolController(_this).IsInsideUI && Cursor.visible)
            {
                GettoolController(_this).RenderBrush(cameraInfo);
            }
        }

        //called from ApplyBrush, ApplyFill, ApplyZoning
        private static void UsedZone(ExtendedItemClass.Zone zone)
        {

            if (zone != ExtendedItemClass.Zone.None)
            {
                ZoneManager instance = Singleton<ZoneManager>.instance;

                if (instance.m_zoneNotUsed.Length == 8) {
                    //instance.m_zoneNotUsed = new ZoneTypeGuide[16];
                } 
                instance.m_zonesNotUsed.Disable();
               // instance.m_zoneNotUsed[(int)zone].Disable();


                switch (zone)
                {
                    case ExtendedItemClass.Zone.ResidentialLow:
                    case ExtendedItemClass.Zone.ResidentialHigh:
                        instance.m_zoneDemandResidential.Deactivate();
                        break;
                    case ExtendedItemClass.Zone.CommercialLow:
                    case ExtendedItemClass.Zone.CommercialHigh:
                        instance.m_zoneDemandCommercial.Deactivate();
                        break;
                    case ExtendedItemClass.Zone.Industrial:
                    case ExtendedItemClass.Zone.Office:
                        instance.m_zoneDemandWorkplace.Deactivate();
                        break;
                }
            }
        }
    }
}
