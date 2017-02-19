using System.Reflection;
using ColossalFramework.Math;
using GrowableOverhaul.Redirection.Attributes;
using UnityEngine;

namespace GrowableOverhaul.Detours
{
    [TargetType(typeof(ZoneTool))]
    public class ZoneToolDetour
    {
        private static FieldInfo m_fillBuffer1_field;
        private static FieldInfo m_zoning_field;
        private static FieldInfo m_dezoning_field;

        private static void FindFieldInfos()
        {
            if (m_fillBuffer1_field == null || m_zoning_field == null || m_dezoning_field == null)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                m_fillBuffer1_field = typeof (ZoneTool).GetField("m_fillBuffer1", flags);
                m_zoning_field = typeof(ZoneTool).GetField("m_zoning", flags);
                m_dezoning_field = typeof(ZoneTool).GetField("m_dezoning", flags);
            }
        }

        private static ulong[] GetFillBuffer(ZoneTool _this)
        {
            FindFieldInfos();
            return (ulong[]) m_fillBuffer1_field.GetValue(_this);
        }

        private static bool IsZoningEnabled(ZoneTool _this)
        {
            FindFieldInfos();
            return (bool) m_zoning_field.GetValue(_this);
        }

        private static bool IsDezoningEnabled(ZoneTool _this)
        {
            FindFieldInfos();
            return (bool)m_dezoning_field.GetValue(_this);
        }

        [RedirectMethod]
        private static void Snap(ZoneTool _this, ref Vector3 point, ref Vector3 direction, ref ItemClass.Zone zone, ref bool occupied1, ref bool occupied2, ref ZoneBlock block)
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
            zone = block.GetZone(num1 + 4, num2 + 4); // keep old method (it's only a single call)
            occupied1 = block.IsOccupied1(num1 + 4, num2 + 4);
            occupied2 = block.IsOccupied2(num1 + 4, num2 + 4);
        }

        [RedirectMethod]
        private static void CalculateFillBuffer(ZoneTool _this, Vector3 position, Vector3 direction, float angle, ushort blockIndex, ref ZoneBlock block, ItemClass.Zone requiredZone, bool occupied1, bool occupied2)
        {
            float f1 = Mathf.Abs(block.m_angle - angle) * 0.6366197f;
            float num1 = f1 - Mathf.Floor(f1);
            if ((double)num1 >= 0.00999999977648258 && (double)num1 <= 0.990000009536743)
                return;
            int rowCount = block.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref block); // modified
            Vector3 vector3_1 = new Vector3(Mathf.Cos(block.m_angle), 0.0f, Mathf.Sin(block.m_angle)) * 8f;
            Vector3 vector3_2 = new Vector3(vector3_1.z, 0.0f, -vector3_1.x);

            var m_fillBuffer1 = GetFillBuffer(_this); // modified
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
                            if (requiredZone == ItemClass.Zone.Unzoned && ((long)block.m_occupied1 & 1L << (z << 3 | x)) == 0L)
                                continue;
                        }
                        else if (occupied2)
                        {
                            if (requiredZone == ItemClass.Zone.Unzoned && ((long)block.m_occupied2 & 1L << (z << 3 | x)) == 0L)
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

        [RedirectMethod]
        private static bool ApplyFillBuffer(ZoneTool _this, Vector3 position, Vector3 direction, float angle, ushort blockIndex, ref ZoneBlock block)
        {
            var m_zoning = IsZoningEnabled(_this); // custom
            var m_dezoning = IsDezoningEnabled(_this); // custom
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

                    var m_fillBuffer1 = GetFillBuffer(_this); // modified

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
                                    if ((_this.m_zone == ItemClass.Zone.Unzoned || ZoneBlockDetour.GetZoneDeep(ref block, blockID, x, z) == ItemClass.Zone.Unzoned) 
                                        && ZoneBlockDetour.SetZoneDeep(ref block, blockID, x, z, _this.m_zone))
                                        flag1 = true;
                                }
                                else if (m_dezoning && ZoneBlockDetour.SetZoneDeep(ref block, blockID, x, z, ItemClass.Zone.Unzoned))
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

        [RedirectMethod]
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

            var m_zoning = IsZoningEnabled(_this); // custom
            var m_dezoning = IsDezoningEnabled(_this); // custom
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
                            if ((_this.m_zone == ItemClass.Zone.Unzoned || ZoneBlockDetour.GetZoneDeep(ref data, blockID, x, z) == ItemClass.Zone.Unzoned) 
                                && ZoneBlockDetour.SetZoneDeep(ref data, blockID, x, z, _this.m_zone))
                                flag = true;
                        }
                        else if (m_dezoning && ZoneBlockDetour.SetZoneDeep(ref data, blockID, x, z, ItemClass.Zone.Unzoned))
                            flag = true;
                    }
                }
            }
            if (!flag)
                return false;
            data.RefreshZoning(blockIndex);
            return true;
        }

        [RedirectMethod]
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

            var m_zoning = IsZoningEnabled(_this); // custom
            var m_dezoning = IsDezoningEnabled(_this); // custom
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
                            if ((_this.m_zone == ItemClass.Zone.Unzoned || ZoneBlockDetour.GetZoneDeep(ref data, blockID, x, z) == ItemClass.Zone.Unzoned)
                                && ZoneBlockDetour.SetZoneDeep(ref data, blockID, x, z, _this.m_zone))
                                flag = true;
                        }
                        else if (m_dezoning && ZoneBlockDetour.SetZoneDeep(ref data, blockID, x, z, ItemClass.Zone.Unzoned))
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

        [RedirectReverse]
        private static void UsedZone(ZoneTool _this, ItemClass.Zone zone)
        {
            Debug.Log($"Dummy code: {zone}");
        }
    }
}
