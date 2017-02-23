using ColossalFramework.Math;
using GrowableOverhaul.Redirection;
using UnityEngine;
using System.Reflection;


namespace GrowableOverhaul
{
    [TargetType(typeof(ZoneManager))]
    public static class ZoneManagerDetour
    {
        // This list must be in sync with ZoneManager#m_cachedBlocks
        public static FastList<ushort> cachedBlockIDs = new FastList<ushort>();

        public static Redirector CreateBlockRedirector = null;
        public static Redirector ReleaseBlockRedirector = null;
        public static Redirector SimulationStepImplRedirector = null;

        
        [RedirectMethod(true)] // Detour reason: Keep cachedBlockIDs in sync
        private static void SimulationStepImpl(ZoneManager _this, int subStep)
        {
            bool blocksUpdated = _this.m_blocksUpdated;

            // Call original method
            SimulationStepImplRedirector.Revert();
            SimulationStepImplAlt(_this, subStep);
            SimulationStepImplRedirector.Apply();

            if(blocksUpdated) cachedBlockIDs.Clear();
        }

        [RedirectReverse(true)]
        private static void SimulationStepImplAlt(ZoneManager _this, int subStep)
        {
            Debug.Log($"SimulationStepImpl {subStep}");
        }

        [RedirectMethod(true)] // Detour reason: Keep cachedBlockIDs in sync
        public static void ReleaseBlock(ZoneManager _this, ushort block)
        {
            if (_this.m_blocks.m_buffer[block].m_flags != 0u)
            {
                cachedBlockIDs.Add(block);
            }

            // Call original method
            ReleaseBlockRedirector.Revert();
            _this.ReleaseBlock(block);
            ReleaseBlockRedirector.Apply();
        }

        /// <summary>
        /// Creates a new zone block. Called by RoadAI#CreateZoneBlocks
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="block"></param>
        /// <param name="randomizer"></param>
        /// <param name="position"></param>
        /// <param name="angle"></param>
        /// <param name="rows"></param>
        /// <param name="buildIndex"></param>
        /// <returns></returns>
        [RedirectMethod(true)] // Detour Reason: Deeper zones data storage, custom depth
        public static bool CreateBlock(ZoneManager _this, out ushort block, ref Randomizer randomizer, Vector3 position, float angle, int rows, uint buildIndex)
        {
            bool result;
            var columns = NetManagerDetour.newBlockColumnCount;

            if (columns == 0) // create no blocks if desired zone depth is 0
            {
                block = 0;
                result = false;
            }
            else
            {
                // Call original method
                CreateBlockRedirector.Revert();
                result = _this.CreateBlock(out block, ref randomizer, position, angle, rows, buildIndex);
                CreateBlockRedirector.Apply();

                if (result)
                {
                    // --- support for larger zones ---
                    if (DataExtension.zones3 != null) DataExtension.zones3[block] = 0UL;
                    if (DataExtension.zones4 != null) DataExtension.zones4[block] = 0UL;

                    // --- dynamic column count ---
                    // TODO should only affect new roads, not ones replaced or splitted by the game (see Network Skins source code)
                    ZoneBlockDetour.SetColumnCount(ref _this.m_blocks.m_buffer[(int) block], columns);
                }
            }
            return result;
        }

        

        /// <summary>
        /// Helper of the other CheckSpace method.
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="block"></param>
        /// <param name="position"></param>
        /// <param name="angle"></param>
        /// <param name="width"></param>
        /// <param name="length"></param>
        /// <param name="space1">building plot columns 1 - 4</param>
        /// <param name="space2">building plot columns 5 - 8</param>
        /// <param name="space3">building plot columns 9 - 12</param>
        /// <param name="space4">building plot columns 13 - 16</param>
        [RedirectMethod(true)]
        public static void CheckSpace(ZoneManager _this, ushort block, Vector3 position, float angle, int width, int length, ref ulong space1, ref ulong space2, ref ulong space3, ref ulong space4)
        {

         


            ZoneBlock zoneBlock = _this.m_blocks.m_buffer[(int)block];

            // difference of 2 radian angles (360 deg = 2*PI * 0.6366197f = 4f)
            // that means an angle difference of 90 deg would result in 1f
            float angleDiff = Mathf.Abs(zoneBlock.m_angle - angle) * 0.6366197f;
            float rightAngleDiff = angleDiff - Mathf.Floor(angleDiff);

            // check if the input angle and the zone block are in right angle (0 90 180 270 deg), otherwise return
            if ((double)rightAngleDiff >= 0.0199999995529652 && (double)rightAngleDiff <= 0.980000019073486) return;

            float searchRadius = Mathf.Min(72f, (float)(width + length) * 4f) + 6f;

            float minX = position.x - searchRadius;
            float minZ = position.z - searchRadius;
            float maxX = position.x + searchRadius;
            float maxZ = position.z + searchRadius;

            // check if the zone block is in the area of interest, otherwise return
            if ((double)zoneBlock.m_position.x + 46.0 < (double)minX || (double)zoneBlock.m_position.x - 46.0 > (double)maxX
                || ((double)zoneBlock.m_position.z + 46.0 < (double)minZ || (double)zoneBlock.m_position.z - 46.0 > (double)maxZ))
            {
                return;
            }

            // width of the zone block
            int rowCount = zoneBlock.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref zoneBlock); // modified

            // orientation of the zone block
            Vector3 columnDirection = new Vector3(Mathf.Cos(zoneBlock.m_angle), 0.0f, Mathf.Sin(zoneBlock.m_angle)) * 8f;
            Vector3 rowDirection = new Vector3(columnDirection.z, 0.0f, -columnDirection.x);

            // direction vectors for the given angle
            Vector3 angleParallelDirection = new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle)) * 8f;
            Vector3 angleOrthogonalDirection = new Vector3(angleParallelDirection.z, 0.0f, -angleParallelDirection.x);

            for (int row = 0; row < rowCount; ++row)
            {
                Vector3 rowMiddleLength = ((float)row - 3.5f) * rowDirection;

                for (int column = 0; (long)column < columnCount; ++column)
                {
                    // check if the current cell is valid (not shared, not occupied)
                    if (((long)zoneBlock.m_valid & ~(long)zoneBlock.m_shared & ~((long)zoneBlock.m_occupied1 | (long)zoneBlock.m_occupied2) & 1L << (row << 3 | column)) != 0L)
                    {
                        Vector3 columnMiddleLength = ((float)column - 3.5f) * columnDirection;

                        // absolute position of the zone block cell
                        Vector3 cellPosition = zoneBlock.m_position + columnMiddleLength + rowMiddleLength;

                        // check if the cell is in search radius
                        if ((double)Mathf.Abs(position.x - cellPosition.x) < (double)searchRadius && (double)Mathf.Abs(position.z - cellPosition.z) < (double)searchRadius)
                        {
                            // cycle through every cell of the building plot
                            // find the cell that is in the same position as the zone block cell
                            bool cellsMatch = false;
                            for (int plotColumn = 0; plotColumn < length && !cellsMatch; ++plotColumn)
                            {
                                Vector3 plotColumnMiddleLength = (float)((double)plotColumn - (double)length * 0.5 + 0.5) * angleOrthogonalDirection;

                                for (int plotRow = 0; plotRow < width && !cellsMatch; ++plotRow)
                                {
                                    Vector3 plotRowMiddleLength = (float)((double)plotRow - (double)width * 0.5 + 0.5) * angleParallelDirection;

                                    // absolute position of the building plot cell
                                    Vector3 plotCellPosition = position + plotRowMiddleLength + plotColumnMiddleLength;

                                    // check if zone block cell and building plot cell positions match
                                    if ((double)Mathf.Abs(plotCellPosition.x - cellPosition.x) < 0.200000002980232 && (double)Mathf.Abs(plotCellPosition.z - cellPosition.z) < 0.200000002980232)
                                    {
                                        cellsMatch = true;
                                        // depending on column, use one of the 4 masks to report that a cell was found
                                        if (plotColumn < 4)
                                            space1 = space1 | (ulong)(1L << (plotColumn << 4 | plotRow));
                                        else if (plotColumn < 8)
                                            space2 = space2 | (ulong)(1L << (plotColumn - 4 << 4 | plotRow));
                                        else if (plotColumn < 12)
                                            space3 = space3 | (ulong)(1L << (plotColumn - 8 << 4 | plotRow));
                                        else
                                            space4 = space4 | (ulong)(1L << (plotColumn - 12 << 4 | plotRow));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
