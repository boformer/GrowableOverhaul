using ColossalFramework;
using GrowableOverhaul.Redirection.Attributes;
using UnityEngine;

namespace GrowableOverhaul.Detours
{
    [TargetType(typeof(BuildingTool))]
    public class BuildingToolDetour
    {
        [RedirectMethod]
        private static void FindClosestZone(BuildingTool _this, BuildingInfo info, ushort block, Vector3 refPos, ref float minD, ref float min2, ref Vector3 minPos, ref float minAngle)
        {
            if ((int)block == 0)
                return;
            ZoneBlock zoneBlock = Singleton<ZoneManager>.instance.m_blocks.m_buffer[(int)block];
            if ((double)Mathf.Abs(zoneBlock.m_position.x - refPos.x) >= 52.0 || (double)Mathf.Abs(zoneBlock.m_position.z - refPos.z) >= 52.0)
                return;
            int rowCount = zoneBlock.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref zoneBlock); // modified
            Vector3 lhs = new Vector3(Mathf.Cos(zoneBlock.m_angle), 0.0f, Mathf.Sin(zoneBlock.m_angle)) * 8f;
            Vector3 vector3_1 = new Vector3(lhs.z, 0.0f, -lhs.x);
            for (int row = 0; row < rowCount; ++row)
            {
                Vector3 vector3_2 = ((float)row - 3.5f) * vector3_1;
                for (int column = 0; (long)column < columnCount; ++column) // modified
                {
                    if (((long)zoneBlock.m_valid & 1L << (row << 3 | column)) != 0L)
                    {
                        Vector3 vector3_3 = ((float)column - 3.5f) * lhs;
                        Vector3 vector3_4 = zoneBlock.m_position + vector3_3 + vector3_2;
                        float num1 = Mathf.Sqrt((float)(((double)vector3_4.x - (double)refPos.x) * ((double)vector3_4.x - (double)refPos.x) + ((double)vector3_4.z - (double)refPos.z) * ((double)vector3_4.z - (double)refPos.z)));
                        float num2 = Vector3.Dot(lhs, refPos - zoneBlock.m_position);
                        if ((double)num1 <= (double)minD - 0.200000002980232 || (double)num1 < (double)minD + 0.200000002980232 && (double)num2 < (double)min2)
                        {
                            minD = num1;
                            min2 = num2;
                            if ((info.m_cellWidth & 1) == 0)
                            {
                                Vector3 vector3_5 = vector3_4 + vector3_1 * 0.5f;
                                Vector3 vector3_6 = vector3_4 - vector3_1 * 0.5f;
                                minPos = ((double)vector3_5.x - (double)refPos.x) * ((double)vector3_5.x - (double)refPos.x) + ((double)vector3_5.z - (double)refPos.z) * ((double)vector3_5.z - (double)refPos.z) >= ((double)vector3_6.x - (double)refPos.x) * ((double)vector3_6.x - (double)refPos.x) + ((double)vector3_6.z - (double)refPos.z) * ((double)vector3_6.z - (double)refPos.z) ? zoneBlock.m_position + (float)((double)info.m_cellLength * 0.5 - 4.0) * lhs + ((float)row - 4f) * vector3_1 : zoneBlock.m_position + (float)((double)info.m_cellLength * 0.5 - 4.0) * lhs + ((float)row - 3f) * vector3_1;
                            }
                            else
                                minPos = zoneBlock.m_position + (float)((double)info.m_cellLength * 0.5 - 4.0) * lhs + ((float)row - 3.5f) * vector3_1;
                            minPos.y = refPos.y;
                            minAngle = zoneBlock.m_angle + 1.570796f;
                        }
                    }
                }
            }
        }
    }
}
