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


        private static FieldInfo m_dataLock_field;

        private static FieldInfo m_fillBuffer1_field;
        private static FieldInfo m_fillBuffer2_field;

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


        private static void FindFieldInfos()
        {
            if (m_fillBuffer1_field == null || m_zoning_field == null || m_dezoning_field == null)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

                m_dataLock_field = typeof(ZoneTool).GetField("m_dataLock", flags);

                m_fillBuffer1_field = typeof (ZoneTool).GetField("m_fillBuffer1", flags);
                m_fillBuffer2_field = typeof(ZoneTool).GetField("m_fillBuffer2", flags);

                m_zoning_field = typeof(ZoneTool).GetField("m_zoning", flags);
                m_dezoning_field = typeof(ZoneTool).GetField("m_dezoning", flags);

                m_mouseRay_field = typeof(ZoneTool).GetField("mouseRay", flags);
                m_mousePosition_field = typeof(ZoneTool).GetField("mousePosition", flags);
                m_mouseRayValid_field = typeof(ZoneTool).GetField("m_mouseRayValid", flags);
                m_mouseRayLength_field = typeof(ZoneTool).GetField("m_mouseRayLength", flags);
                m_mouseDirection_field = typeof(ZoneTool).GetField("m_mouseDirection", flags);

                m_cameraDirection_field = typeof(ZoneTool).GetField("m_cameraDirection", flags);

                m_closeSegmentCount_field = typeof(ZoneTool).GetField("m_closeSegmentCount", flags);
                m_closeSegments_field = typeof(ZoneTool).GetField("m_closeSegments", flags);

                m_validPosition_field = typeof(ZoneTool).GetField("m_validPosition", flags);
            }
        }

        private static ulong[] GetFillBuffer1(ZoneTool _this)
        {
            FindFieldInfos();
            return (ulong[]) m_fillBuffer1_field.GetValue(_this);
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

        private static void AssignMouseDirection(ref ZoneTool _this, Vector3 data ) {

            FindFieldInfos();
            m_mouseDirection_field.SetValue(_this, data);
        }

        private static void AssignMousePosition(ref ZoneTool _this, Vector3 data) {

            FindFieldInfos();
            m_mousePosition_field.SetValue(_this, data);
        }

        private static void AssignValidPosition(ref ZoneTool _this, bool data ) {

            FindFieldInfos();
            m_validPosition_field.SetValue(_this, data);
        }

        //Begin Detours!

        [RedirectMethod(false)]
        private static void SimulationStep(ZoneTool _this)
        {
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
            if (mouseRayValid && ToolBase.RayCast(input, out raycastOutput))
            {
                switch (_this.m_mode)
                {
                    case ZoneTool.Mode.Select:
                        if (!GetZoning(_this) && !GetDezoning(_this))
                        {
                            int closeSegmentCount = GetcloseSegmentCount(_this);

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
                            Singleton<NetManager>.instance.GetClosestSegments(raycastOutput.m_hitPos, _this.m_closeSegments, out _this.m_closeSegmentCount);
                            float num3 = 256f;
                            ushort num4 = 0;
                            for (int j = 0; j < _this.m_closeSegmentCount; j++)
                            {
                                Singleton<NetManager>.instance.m_segments.m_buffer[(int)_this.m_closeSegments[j]].GetClosestZoneBlock(raycastOutput.m_hitPos, ref num3, ref num4);
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
                                            _this.m_fillBuffer2[k] = GetFillBuffer1(_this)[k];
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

        //Called only from simulationstep
        private static bool CalculateFillBuffer(ZoneTool _this, Vector3 position, Vector3 direction, ExtendedItemClass.Zone requiredZone, bool occupied1, bool occupied2)
        {
            for (int i = 0; i < 64; i++)
            {
                _this.m_fillBuffer1[i] = 0uL;
            }
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
                                CalculateFillBuffer(_this, position, direction, angle, num9, ref instance.m_blocks.m_buffer[(int)num9], requiredZone, occupied1, occupied2);
                            }
                            num9 = instance.m_blocks.m_buffer[(int)num9].m_nextGridBlock;
                            if (++num10 >= 49152)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n");
                                break;
                            }
                        }
                    }
                }
            }
            if ((_this.m_fillBuffer1[32] & 4294967296uL) != 0uL)
            {
                _this.m_fillPositions.Clear();
                int l = 0;
                int num12 = 32;
                int num13 = 32;
                int num14 = 32;
                int num15 = 32;
                ZoneTool.FillPos fillPos;
                fillPos.m_x = 32;
                fillPos.m_z = 32;
               _this.m_fillPositions.Add(fillPos);
               _this.m_fillBuffer1[32] &= 18446744069414584319uL;
                while (l < _this.m_fillPositions.m_size)
                {
                    fillPos = _this.m_fillPositions.m_buffer[l++];
                    if (fillPos.m_z > 0)
                    {
                        ZoneTool.FillPos item = fillPos;
                        item.m_z -= 1;
                        if ((_this.m_fillBuffer1[(int)item.m_z] & 1uL << (int)item.m_x) != 0uL)
                        {
                            _this.m_fillPositions.Add(item);
                            _this.m_fillBuffer1[(int)item.m_z] &= ~(1uL << (int)item.m_x);
                            if ((int)item.m_z < num13)
                            {
                                num13 = (int)item.m_z;
                            }
                        }
                    }
                    if (fillPos.m_x > 0)
                    {
                        ZoneTool.FillPos item2 = fillPos;
                        item2.m_x -= 1;
                        if ((this.m_fillBuffer1[(int)item2.m_z] & 1uL << (int)item2.m_x) != 0uL)
                        {
                            this.m_fillPositions.Add(item2);
                            this.m_fillBuffer1[(int)item2.m_z] &= ~(1uL << (int)item2.m_x);
                            if ((int)item2.m_x < num12)
                            {
                                num12 = (int)item2.m_x;
                            }
                        }
                    }
                    if (fillPos.m_x < 63)
                    {
                        ZoneTool.FillPos item3 = fillPos;
                        item3.m_x += 1;
                        if ((this.m_fillBuffer1[(int)item3.m_z] & 1uL << (int)item3.m_x) != 0uL)
                        {
                            this.m_fillPositions.Add(item3);
                            this.m_fillBuffer1[(int)item3.m_z] &= ~(1uL << (int)item3.m_x);
                            if ((int)item3.m_x > num14)
                            {
                                num14 = (int)item3.m_x;
                            }
                        }
                    }
                    if (fillPos.m_z < 63)
                    {
                        ZoneTool.FillPos item4 = fillPos;
                        item4.m_z += 1;
                        if ((this.m_fillBuffer1[(int)item4.m_z] & 1uL << (int)item4.m_x) != 0uL)
                        {
                            this.m_fillPositions.Add(item4);
                            this.m_fillBuffer1[(int)item4.m_z] &= ~(1uL << (int)item4.m_x);
                            if ((int)item4.m_z > num15)
                            {
                                num15 = (int)item4.m_z;
                            }
                        }
                    }
                }
                for (int m = 0; m < 64; m++)
                {
                    this.m_fillBuffer1[m] = 0uL;
                }
                for (int n = 0; n < this.m_fillPositions.m_size; n++)
                {
                    ZoneTool.FillPos fillPos2 = this.m_fillPositions.m_buffer[n];
                    this.m_fillBuffer1[(int)fillPos2.m_z] |= 1uL << (int)fillPos2.m_x;
                }
                return true;
            }
            for (int num16 = 0; num16 < 64; num16++)
            {
                this.m_fillBuffer1[num16] = 0uL;
            }
            return false;
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
            float num = this.m_brushSize * 0.5f;
            Vector3 mousePosition = this.m_mousePosition;
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
            UsedZone(_this, _this.m_zone);
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

        [RedirectMethod(false)]
        private static void ApplyFill(ZoneTool _this)
        {
            if (!this.m_validPosition)
            {
                return;
            }
            Vector3 mousePosition = this.m_mousePosition;
            Vector3 mouseDirection = this.m_mouseDirection;
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
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            if (flag)
            {
                if (this.m_zoning)
                {
                    UsedZone(this.m_zone);
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

        [RedirectMethod(false)]
        private static void ApplyZoning(ZoneTool _this)
        {
            Vector2 a = VectorUtils.XZ(this.m_startPosition);
            Vector2 b = VectorUtils.XZ(this.m_mousePosition);
            Vector2 a2 = VectorUtils.XZ(this.m_startDirection);
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
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            if (flag)
            {
                if (this.m_zoning)
                {
                    UsedZone(this.m_zone);
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

        [RedirectMethod(false)]
        private static void OnToolUpdate(ZoneTool _this)
        {
            ItemClass.Zone zone;
            if (this.m_zone <= ItemClass.Zone.Unzoned || this.m_dezoning)
            {
                zone = ItemClass.Zone.Unzoned;
            }
            else
            {
                zone = this.m_zone;
            }
            if (this.m_zoneCursors != null && (ItemClass.Zone)this.m_zoneCursors.Length > zone)
            {
                base.ToolCursor = this.m_zoneCursors[(int)zone];
            }
        }

        [RedirectMethod(false)]
        private static void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            while (!Monitor.TryEnter(this.m_dataLock, SimulationManager.SYNCHRONIZE_TIMEOUT))
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
                zoning = this.m_zoning;
                dezoning = this.m_dezoning;
                validPosition = this.m_validPosition;
                startPosition = this.m_startPosition;
                mousePosition = this.m_mousePosition;
                startDirection = this.m_startDirection;
                mouseDirection = this.m_mouseDirection;
                for (int i = 0; i < 64; i++)
                {
                    this.m_fillBuffer3[i] = this.m_fillBuffer2[i];
                }
            }
            finally
            {
                Monitor.Exit(this.m_dataLock);
            }
            if ((!zoning && !dezoning && !validPosition) || !Cursor.visible || this.m_toolController.IsInsideUI)
            {
                base.RenderOverlay(cameraInfo);
                return;
            }
            Color color;
            if (zoning || dezoning)
            {
                color = Singleton<ZoneManager>.instance.m_properties.m_activeColor;
            }
            else if (this.m_zone <= ItemClass.Zone.Unzoned || dezoning)
            {
                color = Singleton<ZoneManager>.instance.m_properties.m_unzoneColor;
            }
            else
            {
                //Grab zone colors from new color array. 
                color = Singleton<ZoneManager>.instance.m_properties.m_zoneColors[(int)this.m_zone];
            }
            switch (this.m_mode)
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
                        Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, color, mousePosition, this.m_brushSize, -1f, 1025f, false, true);
                        break;
                    }
                case ZoneTool.Mode.Fill:
                    {
                        Vector3 a4 = mouseDirection;
                        Vector3 a5 = new Vector3(a4.z, 0f, -a4.x);
                        int num5 = -1;
                        ulong num6 = this.m_fillBuffer3[0];
                        for (int j = 0; j <= 64; j++)
                        {
                            int num7 = -1;
                            if (j == 64 || this.m_fillBuffer3[j] != num6)
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
                                if (j != 64 && this.m_fillBuffer3[j] != 0uL)
                                {
                                    num5 = j;
                                    num6 = this.m_fillBuffer3[j];
                                }
                                else
                                {
                                    num5 = -1;
                                }
                            }
                            else if (num5 == -1 && this.m_fillBuffer3[j] != 0uL)
                            {
                                num5 = j;
                                num6 = this.m_fillBuffer3[j];
                            }
                        }
                        break;
                    }
            }
            base.RenderOverlay(cameraInfo);
        }

        //called from ApplyBrush, ApplyFill, ApplyZoning
        private static void UsedZone(ZoneTool _this, ItemClass.Zone zone)
        {
            Debug.Log($"Dummy code: {zone}");
        }
    }
}
