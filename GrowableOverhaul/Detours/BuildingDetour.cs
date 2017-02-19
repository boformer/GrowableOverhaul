using GrowableOverhaul.Redirection.Attributes;
using UnityEngine;

namespace GrowableOverhaul.Detours
{
    [TargetType(typeof(Building))]
    public class BuildingDetour
    {
        [RedirectMethod]
        private static void CheckZoning(ref Building _this, ItemClass.Zone zone1, ItemClass.Zone zone2, ref uint validCells, ref bool secondary, ref ZoneBlock block)
        {
            BuildingInfo.ZoningMode zoningMode = _this.Info.m_zoningMode;
            int width = _this.Width;
            int length = _this.Length;
            Vector3 vector3_1 = new Vector3(Mathf.Cos(_this.m_angle), 0.0f, Mathf.Sin(_this.m_angle)) * 8f;
            Vector3 vector3_2 = new Vector3(vector3_1.z, 0.0f, -vector3_1.x);
            int rowCount = block.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref block); // modified
            Vector3 vector3_3 = new Vector3(Mathf.Cos(block.m_angle), 0.0f, Mathf.Sin(block.m_angle)) * 8f;
            Vector3 vector3_4 = new Vector3(vector3_3.z, 0.0f, -vector3_3.x);
            Vector3 vector3_5 = block.m_position - _this.m_position + vector3_1 * (float)((double)width * 0.5 - 0.5) + vector3_2 * (float)((double)length * 0.5 - 0.5);
            for (int z = 0; z < rowCount; ++z)
            {
                Vector3 vector3_6 = ((float)z - 3.5f) * vector3_4;
                for (int x = 0; (long)x < columnCount; ++x) // modified
                {
                    if (((long)block.m_valid & ~(long)block.m_shared & 1L << (z << 3 | x)) != 0L)
                    {
                        ItemClass.Zone zone = block.GetZone(x, z);
                        bool flag1 = zone == zone1;
                        if (zone == zone2 && zone2 != ItemClass.Zone.None)
                        {
                            flag1 = true;
                            secondary = true;
                        }
                        if (flag1)
                        {
                            Vector3 vector3_7 = ((float)x - 3.5f) * vector3_3;
                            Vector3 vector3_8 = vector3_5 + vector3_7 + vector3_6;
                            float num1 = (float)((double)vector3_1.x * (double)vector3_8.x + (double)vector3_1.z * (double)vector3_8.z);
                            float num2 = (float)((double)vector3_2.x * (double)vector3_8.x + (double)vector3_2.z * (double)vector3_8.z);
                            int num3 = Mathf.RoundToInt(num1 / 64f);
                            int num4 = Mathf.RoundToInt(num2 / 64f);
                            bool flag2 = false;
                            if (zoningMode == BuildingInfo.ZoningMode.Straight)
                                flag2 = num4 == 0;
                            else if (zoningMode == BuildingInfo.ZoningMode.CornerLeft)
                                flag2 = num4 == 0 && num3 >= width - 2 || num4 <= 1 && num3 == width - 1;
                            else if (zoningMode == BuildingInfo.ZoningMode.CornerRight)
                                flag2 = num4 == 0 && num3 <= 1 || num4 <= 1 && num3 == 0;
                            if ((!flag2 || x == 0) && (num3 >= 0 && num4 >= 0) && (num3 < width && num4 < length))
                                validCells = validCells | (uint)(1 << (num4 << 3) + num3);
                        }
                    }
                }
            }
        }
    }
}
