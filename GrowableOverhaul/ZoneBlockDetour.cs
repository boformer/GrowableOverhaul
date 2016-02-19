using ColossalFramework;
using ColossalFramework.Math;
using GrowableOverhaul.Redirection;
using UnityEngine;

namespace GrowableOverhaul
{
    [TargetType(typeof(ZoneBlock))]
    public static class ZoneBlockDetour
    {
        // mask for m_flags to store the zone block depth (shifted by 24 bits)
        public const uint FLAG_COLUMNS = 251658240;// 0000 1111 0000 0000 0000 0000 0000 0000

        public static int GetColumnCount(ref ZoneBlock block)
        {
            var count = (int) ((block.m_flags & FLAG_COLUMNS) >> 24);
            return count > 0 ? count : 4; // return 4 (vanilla depth) for blocks with unset column count
        }

        public static void SetColumnCount(ref ZoneBlock block, int value)
        {
            block.m_flags = block.m_flags & ~FLAG_COLUMNS | (uint)Mathf.Clamp(value, 1, 8) << 24;
        }

        // helper method
        public static ushort FindBlockId(ref ZoneBlock data)
        {
            var zoneManager = ZoneManager.instance;

            int gridX = Mathf.Clamp((int)((double)data.m_position.x / 64.0 + 75.0), 0, 149);
            int gridZ = Mathf.Clamp((int)((double)data.m_position.z / 64.0 + 75.0), 0, 149) * 150 + gridX;

            ushort blockID = zoneManager.m_zoneGrid[gridZ];
            int counter = 0;
            while (blockID != 0)
            {
                ushort nextBlockID = zoneManager.m_blocks.m_buffer[(int)blockID].m_nextGridBlock;

                if (nextBlockID == data.m_nextGridBlock) return blockID;

                blockID = nextBlockID;

                if (++counter >= 49152)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                    break;
                }
            }

            return 0;
        }


        /// <summary>
        /// Overlaps the quad of the zone block with a colliding quad and returns a "invalid" bitmask for the colliding cells. Internal helper method.
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="quad"></param>
        /// <returns></returns>
        private static ulong OverlapQuad(ref ZoneBlock _this, Quad2 quad)
        {
            // width of the zone block
            int rowCount = _this.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref _this); // modified

            // directions of the rows and columns based on zone block angle, multiplied by 8 (cell size)
            Vector2 columnDirection = new Vector2(Mathf.Cos(_this.m_angle), Mathf.Sin(_this.m_angle)) * 8f;
            Vector2 rowDirection = new Vector2(columnDirection.y, -columnDirection.x);

            // bounds of the colliding quad
            Vector2 collidingQuadMin = quad.Min();
            Vector2 collidingQuadMax = quad.Max();

            // origin of the zone block
            // this position is in the center of the 8x8 zone block (4 columns and 4 rows away from the lower corner)
            Vector2 positionXZ = VectorUtils.XZ(_this.m_position);

            // the "invalid" bitmask ("0" = valid, "1" = invalid)
            ulong invalid = 0;

            for (int row = 0; row < rowCount; ++row)
            {
                // calculate 2 relative row positions: 
                // * one 0.1m from previous row
                // * one 0.1m from next row
                Vector2 rowNearPreviousLength = ((float)row - 3.9f) * rowDirection;
                Vector2 rowNearNextLength = ((float)row - 3.1f) * rowDirection;

                for (int column = 0; column < columnCount; ++column)
                {
                    // calculate 2 relative column positions: 
                    // * one 0.1m from previous column
                    // * one 0.1m from next column
                    Vector2 columnNearPreviousLength = ((float)column - 3.9f) * columnDirection;
                    Vector2 columnNearNextLength = ((float)column - 3.1f) * columnDirection;

                    // middle position of the cell
                    Vector2 cellMiddlePos = positionXZ + (columnNearNextLength + columnNearPreviousLength + rowNearNextLength + rowNearPreviousLength) * 0.5f;

                    if ((double)collidingQuadMin.x <= (double)cellMiddlePos.x + 6.0 && (double)collidingQuadMin.y <= (double)cellMiddlePos.y + 6.0
                        && ((double)cellMiddlePos.x - 6.0 <= (double)collidingQuadMax.x && (double)cellMiddlePos.y - 6.0 <= (double)collidingQuadMax.y))
                    {
                        // Create a quad for the cell and intersect it with the colliding quad
                        if (quad.Intersect(new Quad2()
                        {
                            a = positionXZ + columnNearPreviousLength + rowNearPreviousLength,
                            b = positionXZ + columnNearNextLength + rowNearPreviousLength,
                            c = positionXZ + columnNearNextLength + rowNearNextLength,
                            d = positionXZ + columnNearPreviousLength + rowNearNextLength
                        }))
                        {
                            // if the cell is colliding, mark it as "1"
                            invalid |= 1uL << (row << 3 | column);
                        }
                    }
                }
            }
            return invalid;
        }

        /// <summary>
        /// This method marks zone cells overlapped by network segments as invalid. Called by CalculateBlock1.
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="blockID"></param>
        /// <param name="segmentID"></param>
        /// <param name="data"></param>
        /// <param name="valid"></param>
        /// <param name="minX"></param>
        /// <param name="minZ"></param>
        /// <param name="maxX"></param>
        /// <param name="maxZ"></param>
        private static void CalculateImplementation1(ref ZoneBlock _this, ushort blockID, ushort segmentID, ref NetSegment data, ref ulong valid, float minX, float minZ, float maxX, float maxZ)
        {
            // do nothing if the block belongs to the network segment
            if ((int)data.m_blockStartLeft == (int)blockID || (int)data.m_blockStartRight == (int)blockID ||
                ((int)data.m_blockEndLeft == (int)blockID || (int)data.m_blockEndRight == (int)blockID))
            {
                return;
            }

            NetInfo info = data.Info;
            if (!info.m_canCollide) return; // water pipes etc.

            float collisionHalfWidth = info.m_netAI.GetCollisionHalfWidth();

            NetNode[] netNodeArray = Singleton<NetManager>.instance.m_nodes.m_buffer;

            // calculate network bezier curve
            Bezier3 bezier = new Bezier3();
            bezier.a = netNodeArray[(int)data.m_startNode].m_position;
            bezier.d = netNodeArray[(int)data.m_endNode].m_position;
            NetSegment.CalculateMiddlePoints(bezier.a, data.m_startDirection, bezier.d, data.m_endDirection, true, true, out bezier.b, out bezier.c);

            // remove vertical component
            Bezier2 bezierXZ = Bezier2.XZ(bezier);

            // do nothing if the collision hitbox is outside of the hitbox of the zone block
            Vector2 collisionAreaMin = bezierXZ.Min() + new Vector2(-collisionHalfWidth, -collisionHalfWidth);
            Vector2 collisionAreaMax = bezierXZ.Max() + new Vector2(collisionHalfWidth, collisionHalfWidth);
            if ((double)collisionAreaMin.x > (double)maxX || (double)collisionAreaMin.y > (double)maxZ
                || ((double)minX > (double)collisionAreaMax.x || (double)minZ > (double)collisionAreaMax.y))
            {
                return;
            }

            // width of the zone block
            int rowCount = _this.RowCount;

            // directions of the rows and columns based on zone block angle, multiplied by 8 (cell size)
            Vector2 columnDirection = new Vector2(Mathf.Cos(_this.m_angle), Mathf.Sin(_this.m_angle)) * 8f;
            Vector2 rowDirection = new Vector2(columnDirection.y, -columnDirection.x);

            // origin of the zone block
            // this position is in the center of the 8x8 zone block (4 columns and 4 rows away from the lower corner)
            Vector2 positionXZ = VectorUtils.XZ(_this.m_position);

            // area of the zone block (8x8 cells)
            Quad2 zoneBlockQuad = new Quad2
            {
                a = positionXZ - 4f * columnDirection - 4f * rowDirection,
                b = positionXZ + 4f * columnDirection - 4f * rowDirection,
                c = positionXZ + 4f * columnDirection + (float)(rowCount - 4) * rowDirection,
                d = positionXZ - 4f * columnDirection + (float)(rowCount - 4) * rowDirection
            };

            // Calculate the bounds of the network segment at the start node
            float start;
            float end;
            info.m_netAI.GetTerrainModifyRange(out start, out end);
            float halfStart = start * 0.5f; // e.g. 0.25f ---> 0.125f
            float halfEnd = (float)(1.0 - (1.0 - (double)end) * 0.5); // e.g. 0.75f --> 0.875f
            float t = halfStart;
            Vector2 startBezierPos = bezierXZ.Position(halfStart);
            Vector2 startBezierTan = bezierXZ.Tangent(halfStart);
            Vector2 startOrthogonalNormalized = new Vector2(-startBezierTan.y, startBezierTan.x).normalized; // tangent rotated by -90 deg = orthogonal

            Quad2 bezierQuad = new Quad2();

            // set the initial a/b bounds
            if ((double)t < 0.00999999977648258 && (info.m_clipSegmentEnds || (netNodeArray[(int)data.m_startNode].m_flags & NetNode.Flags.Bend) != NetNode.Flags.None))
            {
                Vector2 ortho4m = startOrthogonalNormalized * 4f;
                bezierQuad.a = startBezierPos + ortho4m - VectorUtils.XZ(data.m_startDirection) * 4f;
                bezierQuad.d = startBezierPos - ortho4m - VectorUtils.XZ(data.m_startDirection) * 4f;
            }
            else
            {
                Vector2 orthoHalfWidth = startOrthogonalNormalized * collisionHalfWidth;
                bezierQuad.a = startBezierPos + orthoHalfWidth;
                bezierQuad.d = startBezierPos - orthoHalfWidth;
            }

            // overlap 8 quads describing the position
            int steps = 8;
            for (int step = 1; step <= steps; ++step)
            {
                float interp = halfStart + (halfEnd - halfStart) * (float)step / (float)steps;
                Vector2 interpBezierPos = bezierXZ.Position(interp);
                Vector2 interpBezierTangent = bezierXZ.Tangent(interp);
                interpBezierTangent = new Vector2(-interpBezierTangent.y, interpBezierTangent.x).normalized;

                // set the c/d bounds
                if ((double)interp > 0.990000009536743 && (info.m_clipSegmentEnds || (netNodeArray[(int)data.m_endNode].m_flags & NetNode.Flags.Bend) != NetNode.Flags.None))
                {
                    interpBezierTangent *= 4f;
                    bezierQuad.b = interpBezierPos + interpBezierTangent - VectorUtils.XZ(data.m_endDirection) * 4f;
                    bezierQuad.c = interpBezierPos - interpBezierTangent - VectorUtils.XZ(data.m_endDirection) * 4f;
                }
                else
                {
                    interpBezierTangent *= collisionHalfWidth;
                    bezierQuad.b = interpBezierPos + interpBezierTangent;
                    bezierQuad.c = interpBezierPos - interpBezierTangent;
                }
                Vector2 quadMin = bezierQuad.Min();
                Vector2 quadMax = bezierQuad.Max();

                // Overlap the quad with the zone block quad
                if ((double)quadMin.x <= (double)maxX && (double)quadMin.y <= (double)maxZ && ((double)minX <= (double)quadMax.x && (double)minZ <= (double)quadMax.y) && zoneBlockQuad.Intersect(bezierQuad))
                {
                    // mark colliding cells as invalid
                    valid = valid & ~OverlapQuad(ref _this, bezierQuad);
                }

                // set the a/b bounds for the next quad
                bezierQuad.a = bezierQuad.b;
                bezierQuad.d = bezierQuad.c;
            }
        }

        /// <summary>
        /// Updates the "valid" bitmask. Called by ZoneManager.
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="blockID"></param>
        [RedirectMethod(true)]
        public static void CalculateBlock1(ref ZoneBlock _this, ushort blockID)
        {
            // skip zone blocks which are not in use
            if (((int)_this.m_flags & 3) != ZoneBlock.FLAG_CREATED) return;

            // width of the zone block
            int rowCount = _this.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref _this); // modified

            // directions of the rows and columns based on zone block angle, multiplied by 8 (cell size)
            Vector2 columnDirection = new Vector2(Mathf.Cos(_this.m_angle), Mathf.Sin(_this.m_angle)) * 8f;
            Vector2 rowDirection = new Vector2(columnDirection.y, -columnDirection.x);

            // origin of the zone block
            // this position is in the center of the 8x8 zone block (4 columns and 4 rows away from the lower corner)
            Vector2 positionXZ = VectorUtils.XZ(_this.m_position);

            // area of the zone block (8x4 cells)
            Quad2 zoneBlockQuad = new Quad2()
            {
                a = positionXZ - 4f * columnDirection - 4f * rowDirection,
                b = positionXZ + (columnCount - 4f) * columnDirection - 4f * rowDirection,
                c = positionXZ + (columnCount - 4f) * columnDirection + (float)(rowCount - 4) * rowDirection,
                d = positionXZ - 4f * columnDirection + (float)(rowCount - 4) * rowDirection
            };

            Vector2 quadMin = zoneBlockQuad.Min();
            Vector2 quadMax = zoneBlockQuad.Max();

            NetManager netManager = Singleton<NetManager>.instance;

            // calculate which net segment grid cells are touched by this zone block
            int gridMinX = Mathf.Max((int)(((double)quadMin.x - 64.0) / 64.0 + 135.0), 0);
            int gridMinY = Mathf.Max((int)(((double)quadMin.y - 64.0) / 64.0 + 135.0), 0);
            int gridMaxX = Mathf.Min((int)(((double)quadMax.x + 64.0) / 64.0 + 135.0), 269);
            int gridMaxY = Mathf.Min((int)(((double)quadMax.y + 64.0) / 64.0 + 135.0), 269);

            // This bitmask stores which which cells are "valid" and which are "invalid" 
            // (e.g. colliding with existing buildings or height too steep)
            // This mask limits the maximum size of a zone block to 64 cells (e.g. 8x8)
            // Sort order: (row 8|col 8)(row 8|col 7)...(row 8|col 2)(row 8|col 1)(row 7|col 4)(row 7|col 3)...(row 1|col 1)
            ulong valid = ulong.MaxValue;
            bool quadOutOfArea = Singleton<GameAreaManager>.instance.QuadOutOfArea(zoneBlockQuad);

            // Mark cells which are on too steep terrain or outside of the purchased tiles as invalid
            for (int row = 0; row < rowCount; ++row)
            {
                // calculate 3 relative row positions: 
                // * One in between 2 cell grid points 
                // * one 0.1m from previous row
                // * one 0.1m from next row
                Vector2 rowMiddleLength = ((float)row - 3.5f) * rowDirection;
                Vector2 rowNearPreviousLength = ((float)row - 3.9f) * rowDirection;
                Vector2 rowNearNextLength = ((float)row - 3.1f) * rowDirection;

                // calculate terrain height of the row (5 columns away from zone block origin)
                // that's where the road is
                float height = Singleton<TerrainManager>.instance.SampleRawHeightSmooth(VectorUtils.X_Y(positionXZ + rowMiddleLength - 5f * columnDirection));

                for (int column = 0; column < columnCount; ++column)
                {
                    // calculate 2 relative column positions: 
                    // * one 0.1m from previous column
                    // * one 0.1m from next column
                    Vector2 columnNearPreviousLength = ((float)column - 3.9f) * columnDirection;
                    Vector2 columnNearNextLength = ((float)column - 3.1f) * columnDirection;

                    // calculate terrain height of the cell (row middle, side away from road)
                    float cellHeight = Singleton<TerrainManager>.instance.SampleRawHeightSmooth(VectorUtils.X_Y(positionXZ + rowMiddleLength + columnNearNextLength));

                    // if the height difference between road and cell is greater than 8m, mark the cell as invalid
                    if ((double)Mathf.Abs(cellHeight - height) > 8.0) // TODO maybe this should be raised for 8 cell deep zones?
                    {
                        valid &= ~(1UL << (row << 3 | column));
                    }
                    else if (quadOutOfArea)
                    {
                        // if the cell is outside of the purchased tiles, mark the cell as invalid
                        if (Singleton<GameAreaManager>.instance.QuadOutOfArea(new Quad2()
                        {
                            a = positionXZ + columnNearPreviousLength + rowNearPreviousLength,
                            b = positionXZ + columnNearNextLength + rowNearPreviousLength,
                            c = positionXZ + columnNearNextLength + rowNearNextLength,
                            d = positionXZ + columnNearPreviousLength + rowNearNextLength
                        }))
                            valid &= ~(1UL << (row << 3 | column));
                    }
                }
            }

            // Mark cells which are overlapped by network segments as invalid
            for (int gridY = gridMinY; gridY <= gridMaxY; ++gridY)
            {
                for (int gridX = gridMinX; gridX <= gridMaxX; ++gridX)
                {
                    // cycle through all net segments in the grid cell
                    ushort segmentID = netManager.m_segmentGrid[gridY * 270 + gridX];
                    int counter = 0;
                    while ((int)segmentID != 0)
                    {
                        if (netManager.m_segments.m_buffer[(int)segmentID].Info.m_class.m_layer == ItemClass.Layer.Default)
                        {
                            ushort startNode = netManager.m_segments.m_buffer[(int)segmentID].m_startNode;
                            ushort endNode = netManager.m_segments.m_buffer[(int)segmentID].m_endNode;
                            Vector3 startNodePos = netManager.m_nodes.m_buffer[(int)startNode].m_position;
                            Vector3 endNodePos = netManager.m_nodes.m_buffer[(int)endNode].m_position;

                            // check if the segment (one of its nodes) is in the area of zone block
                            if ((double)Mathf.Max(Mathf.Max(quadMin.x - 64f - startNodePos.x, quadMin.y - 64f - startNodePos.z), Mathf.Max((float)((double)startNodePos.x - (double)quadMax.x - 64.0), (float)((double)startNodePos.z - (double)quadMax.y - 64.0))) < 0.0
                                || (double)Mathf.Max(Mathf.Max(quadMin.x - 64f - endNodePos.x, quadMin.y - 64f - endNodePos.z), Mathf.Max((float)((double)endNodePos.x - (double)quadMax.x - 64.0), (float)((double)endNodePos.z - (double)quadMax.y - 64.0))) < 0.0)
                            {
                                // Mark zone cells overlapped by network segments as invalid
                                CalculateImplementation1(ref _this, blockID, segmentID, ref netManager.m_segments.m_buffer[(int)segmentID], ref valid, quadMin.x, quadMin.y, quadMax.x, quadMax.y);
                            }
                        }
                        // next segment in grid cell (linked list)
                        segmentID = netManager.m_segments.m_buffer[(int)segmentID].m_nextGridSegment;
                        if (++counter >= 36864)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                            break;
                        }
                    }
                }
            }

            // This part marks all cells as invalid which are behind existing invalid cells (so that there are no cells with no road access)
            // 0000 0100 0000 0100 0000 0100 0000 0100
            // 0000 0100 0000 0100 0000 0100 0000 0100
            ulong mask = 144680345676153346;
            for (int iteration = 0; iteration < 7; ++iteration)
            {
                valid = valid & ~mask | valid & valid << 1 & mask;
                mask <<= 1;
            }

            // apply the new mask, reset shared mask
            _this.m_valid = valid;
            _this.m_shared = 0UL;
        }

        /// <summary>
        /// Intersects zone block with other zone block, updates "valid" and "shared" masks.
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="blockID"></param>
        /// <param name="other"></param>
        /// <param name="valid"></param>
        /// <param name="shared"></param>
        /// <param name="minX"></param>
        /// <param name="minZ"></param>
        /// <param name="maxX"></param>
        /// <param name="maxZ"></param>
        private static void CalculateImplementation2(ref ZoneBlock _this, ushort otherBlockID, ushort blockID, ref ZoneBlock other, ref ulong valid, ref ulong shared, float minX, float minZ, float maxX, float maxZ)
        {
            // 92 = sqrt(64^2+64^2)
            // if the other zone block is not marked as "created" or too far away, do nothing
            if (((int)other.m_flags & ZoneBlock.FLAG_CREATED) == 0 || (double)Mathf.Abs(other.m_position.x - _this.m_position.x) >= 92.0 || (double)Mathf.Abs(other.m_position.z - _this.m_position.z) >= 92.0)
            {
                return;
            }

            // checks if the other zone block is marked as "deleted"
            bool deleted = ((int)other.m_flags & ZoneBlock.FLAG_DELETED) != 0;

            // width of block and other block
            int rowCount = _this.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref _this); // modified
            int otherRowCount = other.RowCount;
            int otherColumnCount = ZoneBlockDetour.GetColumnCount(ref other); // modified

            // directions of the rows and columns of the block, multiplied by 8 (cell size)
            Vector2 columnDirection = new Vector2(Mathf.Cos(_this.m_angle), Mathf.Sin(_this.m_angle)) * 8f;
            Vector2 rowDirection = new Vector2(columnDirection.y, -columnDirection.x);

            // directions of the rows and columns of the other block, multiplied by 8 (cell size)
            Vector2 otherColumnDirection = new Vector2(Mathf.Cos(other.m_angle), Mathf.Sin(other.m_angle)) * 8f;
            Vector2 otherRowDirection = new Vector2(otherColumnDirection.y, -otherColumnDirection.x);

            // origin of the other block
            Vector2 otherPositionXZ = VectorUtils.XZ(other.m_position);

            // area of the other zone block
            Quad2 otherZoneBlockQuad = new Quad2
            {
                a = otherPositionXZ - 4f * otherColumnDirection - 4f * otherRowDirection,
                b = otherPositionXZ + (otherColumnCount - 4f) * otherColumnDirection - 4f * otherRowDirection,
                c = otherPositionXZ + (otherColumnCount - 4f) * otherColumnDirection + (float)(otherRowCount - 4) * otherRowDirection,
                d = otherPositionXZ - 4f * otherColumnDirection + (float)(otherRowCount - 4) * otherRowDirection
            };

            Vector2 otherQuadMin = otherZoneBlockQuad.Min();
            Vector2 otherQuadMax = otherZoneBlockQuad.Max();

            // return if there is no chance that the 2 quads collide
            if ((double)otherQuadMin.x > (double)maxX || (double)otherQuadMin.y > (double)maxZ || ((double)minX > (double)otherQuadMax.x || (double)minZ > (double)otherQuadMax.y))
            {
                return;
            }

            // origin of the block
            Vector2 positionXZ = VectorUtils.XZ(_this.m_position);

            // area of the zone block (8x4 cells)
            Quad2 zoneBlockQuad = new Quad2
            {
                a = positionXZ - 4f * columnDirection - 4f * rowDirection,
                b = positionXZ + (columnCount - 4f) * columnDirection - 4f * rowDirection,
                c = positionXZ + (columnCount - 4f) * columnDirection + (float)(rowCount - 4) * rowDirection,
                d = positionXZ - 4f * columnDirection + (float)(rowCount - 4) * rowDirection
            };

            // return if the quads are not intersecting
            if (!zoneBlockQuad.Intersect(otherZoneBlockQuad)) return;

            for (int row = 0; row < rowCount; ++row)
            {
                // calculate 2 relative row positions: 
                // * one 0.01m from previous row
                // * one 0.01m from next row
                Vector2 rowNearPreviousLength = ((float)row - 3.99f) * rowDirection;
                Vector2 rowNearNextLength = ((float)row - 3.01f) * rowDirection;

                // set the quad to the row (4 cells)
                zoneBlockQuad.a = positionXZ - 4f * columnDirection + rowNearPreviousLength;
                zoneBlockQuad.b = positionXZ + (columnCount - 4f) * columnDirection + rowNearPreviousLength;
                zoneBlockQuad.c = positionXZ + (columnCount - 4f) * columnDirection + rowNearNextLength;
                zoneBlockQuad.d = positionXZ - 4f * columnDirection + rowNearNextLength;

                // Intersect the row quad with the other zone block quad
                if (zoneBlockQuad.Intersect(otherZoneBlockQuad))
                {
                    for (int column = 0; column < columnCount && (valid & 1uL << (row << 3 | column)) != 0uL; ++column)
                    {
                        // calculate 2 relative column positions: 
                        // * one 0.01m from previous column
                        // * one 0.01m from next column
                        Vector2 columnNearPreviousLength = ((float)column - 3.99f) * columnDirection;
                        Vector2 columnNearNextLength = ((float)column - 3.01f) * columnDirection;

                        // middle position of the cell
                        Vector2 cellMiddlePos = positionXZ + (columnNearNextLength + columnNearPreviousLength + rowNearNextLength + rowNearPreviousLength) * 0.5f;

                        // check if the middle position of the cell is contained in the quad of the other zone block (1 cell tolerance)
                        if (Quad2.Intersect(otherZoneBlockQuad.a - otherColumnDirection - otherRowDirection, otherZoneBlockQuad.b + otherColumnDirection - otherRowDirection,
                            otherZoneBlockQuad.c + otherColumnDirection + otherRowDirection, otherZoneBlockQuad.d - otherColumnDirection + otherRowDirection, cellMiddlePos))
                        {
                            // Create a quad for the cell
                            Quad2 cellQuad = new Quad2
                            {
                                a = positionXZ + columnNearPreviousLength + rowNearPreviousLength,
                                b = positionXZ + columnNearNextLength + rowNearPreviousLength,
                                c = positionXZ + columnNearNextLength + rowNearNextLength,
                                d = positionXZ + columnNearPreviousLength + rowNearNextLength
                            };

                            // cycle through the cells of the other zone block
                            bool cellIsValid = true;
                            bool shareCell = false;
                            for (int otherRow = 0; otherRow < otherRowCount && cellIsValid; ++otherRow)
                            {
                                // calculate 2 relative row positions for the cell in the other zone block: 
                                // * one 0.01m from previous row
                                // * one 0.01m from next row
                                Vector2 otherRowNearPreviousLength = ((float)otherRow - 3.99f) * otherRowDirection;
                                Vector2 otherRowNearNextLength = ((float)otherRow - 3.01f) * otherRowDirection;

                                for (int otherColumn = 0; otherColumn < otherColumnCount && cellIsValid; ++otherColumn)
                                {
                                    // checks if the cell is marked as valid in the valid mask of the other block, and that it is not contained in the shared mask
                                    if ((other.m_valid & ~other.m_shared & 1uL << (otherRow << 3 | otherColumn)) != 0uL)
                                    {
                                        // calculate 2 relative column positions for the cell in the other zone block: 
                                        // * one 0.01m from previous column
                                        // * one 0.01m from next column
                                        Vector2 otherColumnNearPreviousLength = ((float)otherColumn - 3.99f) * otherColumnDirection;
                                        Vector2 otherColumnNearNextLength = ((float)otherColumn - 3.01f) * otherColumnDirection;

                                        // squared distance between the 2 cell middle positions
                                        float cellMiddleDist = Vector2.SqrMagnitude(otherPositionXZ + (otherColumnNearNextLength + otherColumnNearPreviousLength + 
                                            otherRowNearNextLength + otherRowNearPreviousLength) * 0.5f - cellMiddlePos);

                                        // check if the 2 cells can touch
                                        if ((double)cellMiddleDist < 144.0)
                                        {
                                            if (!deleted) // other zone block not deleted:
                                            {
                                                // difference of 2 radian angles (360 deg = 2*PI * 0.6366197f = 4f)
                                                // that means an angle difference of 90 deg would result in 1f
                                                float angleDiff = Mathf.Abs(other.m_angle - _this.m_angle) * 0.6366197f;
                                                float rightAngleDiff = angleDiff - Mathf.Floor(angleDiff); // difference from 90 deg

                                                // if the 2 cells are almost in the same spot with an angle difference of 0 90 180 270 deg, mark one of them as shared
                                                if ((double)cellMiddleDist < 0.00999999977648258 && ((double)rightAngleDiff < 0.00999999977648258 || (double)rightAngleDiff > 0.990000009536743))
                                                {
                                                    // The cell closer to road (or that was created earler) is kept, the other marked as shared
                                                    if (column < otherColumn || column == otherColumn && _this.m_buildIndex < other.m_buildIndex)
                                                        other.m_shared |= 1UL << (otherRow << 3 | otherColumn);
                                                    else
                                                        shareCell = true;
                                                }
                                                // angles not right or not in the same place: Intersect the 2 cells
                                                else if (cellQuad.Intersect(new Quad2()
                                                {
                                                    a = otherPositionXZ + otherColumnNearPreviousLength + otherRowNearPreviousLength,
                                                    b = otherPositionXZ + otherColumnNearNextLength + otherRowNearPreviousLength,
                                                    c = otherPositionXZ + otherColumnNearNextLength + otherRowNearNextLength,
                                                    d = otherPositionXZ + otherColumnNearPreviousLength + otherRowNearNextLength
                                                }))
                                                {
                                                    // mark the cell which is further away from the road (or was created later) as invalid
                                                    // TODO adapt for 8 cell zones (low priority)
                                                    if (otherColumn >= 4 && column >= 4 || otherColumn < 4 && column < 4)
                                                    {
                                                        if (otherColumn >= 2 && column >= 2 || otherColumn < 2 && column < 2)
                                                        {
                                                            if (_this.m_buildIndex < other.m_buildIndex)
                                                                other.m_valid &= ~(1UL << (otherRow << 3 | otherColumn));
                                                            else
                                                                cellIsValid = false;
                                                        }
                                                        else if (otherColumn < 2)
                                                            cellIsValid = false;
                                                        else
                                                            other.m_valid &= ~(1UL << (otherRow << 3 | otherColumn));
                                                    }
                                                    else if (otherColumn < 4)
                                                        cellIsValid = false;
                                                    else
                                                        other.m_valid &= ~(1UL << (otherRow << 3 | otherColumn));
                                                }
                                            }
                                            // distance between cell middle pos < 6 = cells colliding
                                            // if the cell is unzoned, take over the zone type of the other one
                                            if ((double)cellMiddleDist < 36.0 && column < 8 && otherColumn < 8) // modifed 4 --> 8
                                            {
                                                ItemClass.Zone zone1 = GetZoneDeep(ref _this, blockID, column, row);
                                                ItemClass.Zone zone2 = GetZoneDeep(ref other, otherBlockID, otherColumn, otherRow);
                                                if (zone1 == ItemClass.Zone.Unzoned)
                                                    SetZoneDeep(ref _this, blockID, column, row, zone2);
                                                else if (zone2 == ItemClass.Zone.Unzoned && !deleted)
                                                    SetZoneDeep(ref other, otherBlockID, otherColumn, otherRow, zone1);
                                            }
                                        }
                                    }
                                }
                            }
                            if (!cellIsValid)
                            {
                                valid = valid & ~(1UL << (row << 3 | column));
                                break;
                            }
                            if (shareCell)
                                shared = shared | 1UL << (row << 3 | column);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Intersects the block with other zone blocks near it, marks shared cells. Called by ZoneManager.
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="blockID"></param>
        [RedirectMethod(true)]
        public static void CalculateBlock2(ref ZoneBlock _this, ushort blockID)
        {
            // skip zone blocks which are not in use
            if (((int)_this.m_flags & 3) != ZoneBlock.FLAG_CREATED) return;

            // width of the zone block
            int rowCount = _this.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref _this); // modified

            // directions of the rows and columns based on zone block angle, multiplied by 8 (cell size)
            Vector2 columnDirection = new Vector2(Mathf.Cos(_this.m_angle), Mathf.Sin(_this.m_angle)) * 8f;
            Vector2 rowDirection = new Vector2(columnDirection.y, -columnDirection.x);

            Vector2 positionXZ = VectorUtils.XZ(_this.m_position);

            // bounds of the zone block
            Vector2 a = positionXZ - 4f * columnDirection - 4f * rowDirection;
            Vector2 b = positionXZ + (columnCount - 4f) * columnDirection - 4f * rowDirection;
            Vector2 c = positionXZ + (columnCount - 4f) * columnDirection + (float)(rowCount - 4) * rowDirection;
            Vector2 d = positionXZ - 4f * columnDirection + (float)(rowCount - 4) * rowDirection;
            float minX = Mathf.Min(Mathf.Min(a.x, b.x), Mathf.Min(c.x, d.x));
            float minZ = Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y));
            float maxX = Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Max(c.x, d.x));
            float maxZ = Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y));

            // "valid" mask
            ulong valid = _this.m_valid;

            // "shared" mask
            ulong shared = 0;

            ZoneManager zoneManager = Singleton<ZoneManager>.instance;

            // check if cached zone blocks are intersecting (updates valid and shared masks)
            for (int i = 0; i < zoneManager.m_cachedBlocks.m_size; ++i)
            {
                ushort otherBlockID = ZoneManagerDetour.cachedBlockIDs[i]; // custom
                CalculateImplementation2(ref _this, otherBlockID, blockID, ref zoneManager.m_cachedBlocks.m_buffer[i], ref valid, ref shared, minX, minZ, maxX, maxZ);
            }

            // calculate which zone block grid cells are touched by this zone block
            int gridMinX = Mathf.Max((int)(((double)minX - 46.0) / 64.0 + 75.0), 0);
            int gridMinZ = Mathf.Max((int)(((double)minZ - 46.0) / 64.0 + 75.0), 0);
            int gridMaxX = Mathf.Min((int)(((double)maxX + 46.0) / 64.0 + 75.0), 149);
            int gridMaxZ = Mathf.Min((int)(((double)maxZ + 46.0) / 64.0 + 75.0), 149);

            // Cycle through all zone blocks in touched grid cells
            for (int gridZ = gridMinZ; gridZ <= gridMaxZ; ++gridZ)
            {
                for (int gridX = gridMinX; gridX <= gridMaxX; ++gridX)
                {
                    // Cycle through all zone blocks in grid cell
                    ushort otherBlockId = zoneManager.m_zoneGrid[gridZ * 150 + gridX];
                    int counter = 0;
                    while ((int)otherBlockId != 0)
                    {
                        Vector3 otherPosition = zoneManager.m_blocks.m_buffer[(int)otherBlockId].m_position;
                        // 46 = 0.5 * sqrt(64^2+64^2)
                        // if the block is not too far away and not the same instance, intersect it (updates valid and shared masks)
                        if ((double)Mathf.Max(Mathf.Max(minX - 46f - otherPosition.x, minZ - 46f - otherPosition.z),
                            Mathf.Max((float)((double)otherPosition.x - (double)maxX - 46.0), (float)((double)otherPosition.z - (double)maxZ - 46.0))) < 0.0
                            && (int)otherBlockId != (int)blockID)
                        {
                            CalculateImplementation2(ref _this, otherBlockId, blockID, ref zoneManager.m_blocks.m_buffer[(int)otherBlockId], ref valid, ref shared, minX, minZ, maxX, maxZ);
                        }

                        // next zone block in grid cell (linked list)
                        otherBlockId = zoneManager.m_blocks.m_buffer[(int)otherBlockId].m_nextGridBlock;

                        if (++counter >= 49152)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            // This part marks all cells as invalid which are behind existing invalid cells (so that there are no cells with no road access)
            // 0000 0100 0000 0100 0000 0100 0000 0100
            // 0000 0100 0000 0100 0000 0100 0000 0100
            ulong mask = 144680345676153346;
            for (int iteration = 0; iteration < 7; ++iteration)
            {
                valid = valid & ~mask | valid & valid << 1 & mask;
                mask <<= 1;
            }

            // apply the new masks
            _this.m_valid = valid;
            _this.m_shared = shared;
        }

        /// <summary>
        /// Marks cells colliding with buildings as occupied and removes the zoning if the building is plopable.
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="blockID"></param>
        /// <param name="info"></param>
        /// <param name="building"></param>
        /// <param name="occupied1"></param>
        /// <param name="occupied2"></param>
        /// <param name="zone1"></param>
        /// <param name="zone2"></param>
        /// <param name="minX"></param>
        /// <param name="minZ"></param>
        /// <param name="maxX"></param>
        /// <param name="maxZ"></param>
        private static void CalculateImplementation3(ref ZoneBlock _this, ref ulong zone3, ref ulong zone4, ushort blockID, BuildingInfo info, ref Building building, ref ulong occupied1, ref ulong occupied2, ref ulong zone1, ref ulong zone2, float minX, float minZ, float maxX, float maxZ)
        {
            // width of the zone block
            int rowCount = _this.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref _this); // modified

            // directions of the rows and columns based on zone block angle, multiplied by 8 (cell size)
            Vector2 columnDirection = new Vector2(Mathf.Cos(_this.m_angle), Mathf.Sin(_this.m_angle)) * 8f;
            Vector2 rowDirection = new Vector2(columnDirection.y, -columnDirection.x);

            // size of the building
            int width = building.Width;
            int length = building.Length;

            // direction of building width and length vectors
            Vector2 widthDirection = new Vector2(Mathf.Cos(building.m_angle), Mathf.Sin(building.m_angle));
            Vector2 lengthDirection = new Vector2(widthDirection.y, -widthDirection.x);

            // building width and length vectors (-0.8m tolerance)
            Vector2 halfWidthVector = widthDirection * (float)((double)width * 4.0 - 0.800000011920929);
            Vector2 halfLengthVector = lengthDirection * (float)((double)length * 4.0 - 0.800000011920929);
            if (info.m_circular)
            {
                halfWidthVector *= 0.7f;
                halfLengthVector *= 0.7f;
            }

            // position of the building
            Vector2 buildingPositionXZ = VectorUtils.XZ(building.m_position);

            // quad of the building lot
            Quad2 buildingQuad = new Quad2
            {
                a = buildingPositionXZ - halfWidthVector - halfLengthVector,
                b = buildingPositionXZ + halfWidthVector - halfLengthVector,
                c = buildingPositionXZ + halfWidthVector + halfLengthVector,
                d = buildingPositionXZ - halfWidthVector + halfLengthVector
            };

            Vector2 quadMin = buildingQuad.Min();
            Vector2 quadMax = buildingQuad.Max();

            // return if building not in collision range
            if ((double)quadMin.x > (double)maxX || (double)quadMin.y > (double)maxZ || ((double)minX > (double)quadMax.x || (double)minZ > (double)quadMax.y))
            {
                return;
            }

            // zone block position
            Vector2 positionXZ = VectorUtils.XZ(_this.m_position);

            // return if zone block quad does not intersect with building quad
            if (!new Quad2()
            {
                a = (positionXZ - 4f * columnDirection - 4f * rowDirection),
                b = (positionXZ + (columnCount - 4f) * columnDirection - 4f * rowDirection),
                c = (positionXZ + (columnCount - 4f) * columnDirection + (float)(rowCount - 4) * rowDirection),
                d = (positionXZ - 4f * columnDirection + (float)(rowCount - 4) * rowDirection)
            }.Intersect(buildingQuad))
            {
                return;
            }

            // Calculate which cells are colliding with the building
            ulong overlapCellMask = OverlapQuad(ref _this, buildingQuad);

            if (info.m_buildingAI.ClearOccupiedZoning()) // for non-growables
            {
                // set cells as occupied (use occupied2 mask)
                occupied2 = occupied2 | overlapCellMask;

                // Use zone1 mask for cells close to road (column 1 and 2)
                // 72340172838076673 - do not shift
                // 0000 0001 0000 0001 0000 0001 0000 0001
                // 0000 0001 0000 0001 0000 0001 0000 0001
                // 144680345676153346 - shift 3 to left
                // 0000 0010 0000 0010 0000 0010 0000 0010
                // 0000 0010 0000 0010 0000 0010 0000 0010
                ulong zoneClearMask = overlapCellMask & 72340172838076673UL | (overlapCellMask & 144680345676153346UL) << 3;
                zoneClearMask = zoneClearMask | zoneClearMask << 1;
                zoneClearMask = zoneClearMask | zoneClearMask << 2; // clear all 4 bits of each cell

                zone1 = zone1 & ~zoneClearMask;

                // Use zone2 mask for cells away to road (column 3 and 4)
                // 289360691352306692 - shift 2 to right
                // 0000 0100 0000 0100 0000 0100 0000 0100 
                // 0000 0100 0000 0100 0000 0100 0000 0100 
                // 578721382704613384 - shift 1 to left
                // 0000 1000 0000 1000 0000 1000 0000 1000 
                // 0000 1000 0000 1000 0000 1000 0000 1000 
                zoneClearMask = (overlapCellMask & 289360691352306692UL) >> 2 | (overlapCellMask & 578721382704613384UL) << 1;
                zoneClearMask = zoneClearMask | zoneClearMask << 1;
                zoneClearMask = zoneClearMask | zoneClearMask << 2; // clear all 4 bits of each cell

                zone2 = zone2 & ~zoneClearMask;

                // --- support for deeper zones ---

                // Use zone3 mask for cells away to road (column 5 and 6)
                // 1157442765409226768 - shift 4 to right
                // 0001 0000 0001 0000 0001 0000 0001 0000 
                // 0001 0000 0001 0000 0001 0000 0001 0000 
                // 2314885530818453536 - shift 1 to right
                // 0010 0000 0010 0000 0010 0000 0010 0000 
                // 0010 0000 0010 0000 0010 0000 0010 0000
                zoneClearMask = (overlapCellMask & 1157442765409226768UL) >> 4 | (overlapCellMask & 2314885530818453536UL) >> 1;
                zoneClearMask = zoneClearMask | zoneClearMask << 1;
                zoneClearMask = zoneClearMask | zoneClearMask << 2; // clear all 4 bits of each cell

                zone3 = zone3 & ~zoneClearMask;

                // Use zone4 mask for cells away to road (column 7 and 8)
                // 4629771061636907072 - shift 6 to right
                // 0100 0000 0100 0000 0100 0000 0100 0000 
                // 0100 0000 0100 0000 0100 0000 0100 0000 
                // 0x8080808080808080 - shift 3 to right
                // 1000 0000 1000 0000 1000 0000 1000 0000 
                // 1000 0000 1000 0000 1000 0000 1000 0000 
                zoneClearMask = (overlapCellMask & 4629771061636907072UL) >> 6 | (overlapCellMask & 0x8080808080808080UL) >> 3;
                zoneClearMask = zoneClearMask | zoneClearMask << 1;
                zoneClearMask = zoneClearMask | zoneClearMask << 2; // clear all 4 bits of each cell

                zone4 = zone4 & ~zoneClearMask;
            }
            else
                // set cells as occupied (use occupied1 mask)
                occupied1 = occupied1 | overlapCellMask;
        }

        /// <summary>
        /// Checks if buildings collide with the zone block and updates the "occupied" and "zone" masks. Called by ZoneManager.
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="blockID"></param>
        [RedirectMethod(true)]
        public static void CalculateBlock3(ref ZoneBlock _this, ushort blockID)
        {
            // skip zone blocks which are not in use
            if (((int)_this.m_flags & 3) != ZoneBlock.FLAG_CREATED) return;

            // width of the zone block
            int rowCount = _this.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref _this); // modified

            Vector2 columnDirection = new Vector2(Mathf.Cos(_this.m_angle), Mathf.Sin(_this.m_angle)) * 8f;
            Vector2 rowDirection = new Vector2(columnDirection.y, -columnDirection.x);
            Vector2 positionXZ = VectorUtils.XZ(_this.m_position);

            // bounds of the zone block
            Vector2 a = positionXZ - 4f * columnDirection - 4f * rowDirection;
            Vector2 b = positionXZ + (columnCount - 4f) * columnDirection - 4f * rowDirection;
            Vector2 c = positionXZ + (columnCount - 4f) * columnDirection + (float)(rowCount - 4) * rowDirection;
            Vector2 d = positionXZ - 4f * columnDirection + (float)(rowCount - 4) * rowDirection;
            float minX = Mathf.Min(Mathf.Min(a.x, b.x), Mathf.Min(c.x, d.x));
            float minZ = Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y));
            float maxX = Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Max(c.x, d.x));
            float maxZ = Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y));

            BuildingManager buildingManager = Singleton<BuildingManager>.instance;

            // calculate which building grid cells are touched by this zone block
            int gridMinX = Mathf.Max((int)(((double)minX - 72.0) / 64.0 + 135.0), 0);
            int gridMinZ = Mathf.Max((int)(((double)minZ - 72.0) / 64.0 + 135.0), 0);
            int gridMaxX = Mathf.Min((int)(((double)maxX + 72.0) / 64.0 + 135.0), 269);
            int gridMaxZ = Mathf.Min((int)(((double)maxZ + 72.0) / 64.0 + 135.0), 269);

            // masks for zones and occupation
            ulong occupied1 = 0;
            ulong occupied2 = 0;
            ulong zone1 = _this.m_zone1;
            ulong zone2 = _this.m_zone2;

            // --- support for deeper zones ---
            ulong zone3 = DataExtension.zones3 != null ? DataExtension.zones3[blockID] : 0;
            ulong zone4 = DataExtension.zones4 != null ? DataExtension.zones4[blockID] : 0;

            // Cycle through all touched grid cells
            for (int gridZ = gridMinZ; gridZ <= gridMaxZ; ++gridZ)
            {
                for (int gridX = gridMinX; gridX <= gridMaxX; ++gridX)
                {
                    // Cycle through all buildings in grid cell
                    ushort buildingID = buildingManager.m_buildingGrid[gridZ * 270 + gridX];
                    int counter = 0;
                    while ((int)buildingID != 0)
                    {
                        BuildingInfo info;
                        int width;
                        int length;
                        buildingManager.m_buildings.m_buffer[(int)buildingID].GetInfoWidthLength(out info, out width, out length);
                        if (info.m_class.m_layer == ItemClass.Layer.Default)
                        {
                            Vector3 buildingPosition = buildingManager.m_buildings.m_buffer[(int)buildingID].m_position;

                            // check if the zone block can touch the building
                            float num7 = Mathf.Min(72f, (float)(width + length) * 4f);
                            if ((double)Mathf.Max(Mathf.Max(minX - num7 - buildingPosition.x, minZ - num7 - buildingPosition.z), Mathf.Max(buildingPosition.x - maxX - num7, buildingPosition.z - maxZ - num7)) < 0.0)
                            {
                                // Mark cells colliding with the building as occupied (and remove the zoning)
                                CalculateImplementation3(ref _this, ref zone3, ref zone4, blockID, info, ref buildingManager.m_buildings.m_buffer[(int)buildingID], ref occupied1, ref occupied2, ref zone1, ref zone2, minX, minZ, maxX, maxZ);
                            }
                        }

                        // next building in grid cell (linked list)
                        buildingID = buildingManager.m_buildings.m_buffer[(int)buildingID].m_nextGridBuilding;

                        if (++counter >= 49152)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                            break;
                        }
                    }
                }
            }

            // apply new masks
            _this.m_occupied1 = occupied1;
            _this.m_occupied2 = occupied2;
            _this.m_zone1 = zone1;
            _this.m_zone2 = zone2;

            // --- support for deeper zones ---
            if (DataExtension.zones3 != null) DataExtension.zones3[blockID] = zone3;
            if (DataExtension.zones4 != null) DataExtension.zones4[blockID] = zone4;
        }

        /// <summary>
        /// Called by ZoneManager#SimulationStepImpl.
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="blockID"></param>
        [RedirectMethod(true)]
        public static void UpdateBlock(ref ZoneBlock _this, ushort blockID)
        {
            // skip zone blocks which are not in use
            if (((int)_this.m_flags & 1) == 0) return;

            // width of the zone
            int rowCount = _this.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref _this); // modified

            // directions of the rows and columns based on zone block angle, multiplied by 8 (cell size)
            Vector3 columnDirection = new Vector3(Mathf.Cos(_this.m_angle), 0.0f, Mathf.Sin(_this.m_angle)) * 8f;
            Vector3 rowDirection = new Vector3(columnDirection.z, 0.0f, -columnDirection.x);

            // bounds of the zone block
            Vector3 a = _this.m_position - 4f * columnDirection - 4f * rowDirection;
            Vector3 b = _this.m_position + (columnCount - 4f) * columnDirection - 4f * rowDirection;
            Vector3 c = _this.m_position + (columnCount - 4f) * columnDirection + (float)(rowCount - 4) * rowDirection;
            Vector3 d = _this.m_position - 4f * columnDirection + (float)(rowCount - 4) * rowDirection;
            float minX = Mathf.Min(Mathf.Min(a.x, b.x), Mathf.Min(c.x, d.x));
            float minZ = Mathf.Min(Mathf.Min(a.z, b.z), Mathf.Min(c.z, d.z));
            float maxX = Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Max(c.x, d.x));
            float maxZ = Mathf.Max(Mathf.Max(a.z, b.z), Mathf.Max(c.z, d.z));

            // do stuff
            TerrainModify.UpdateArea(minX, minZ, maxX, maxZ, false, false, true); // rendering-related
            Singleton<BuildingManager>.instance.ZonesUpdated(minX, minZ, maxX, maxZ); // notifies buildings that zones were updated
        }

        /// <summary>
        /// Calculate the distance between the point and the nearest cell of the zone block.
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="point"></param>
        /// <param name="minDistanceSq"></param>
        /// <returns></returns>
        [RedirectMethod(true)]
        public static float PointDistanceSq(ref ZoneBlock _this, Vector3 point, float minDistanceSq)
        {
            // width of the zone
            int rowCount = _this.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref _this); // modified

            // directions of the rows and columns based on zone block angle, multiplied by 8 (cell size)
            Vector3 columnDirection = new Vector3(Mathf.Cos(_this.m_angle), 0.0f, Mathf.Sin(_this.m_angle)) * 8f;
            Vector3 rowDirection = new Vector3(columnDirection.z, 0.0f, -columnDirection.x);

            float minDistance = Mathf.Sqrt(minDistanceSq);

            // bounds of the zone block
            Vector3 a = _this.m_position - 4f * columnDirection - 4f * rowDirection;
            Vector3 b = _this.m_position + (columnCount - 4f) * columnDirection - 4f * rowDirection;
            Vector3 c = _this.m_position + (columnCount - 4f) * columnDirection + (float)(rowCount - 4) * rowDirection;
            Vector3 d = _this.m_position - 4f * columnDirection + (float)(rowCount - 4) * rowDirection;

            // note the minDistance tolerance
            float minX = Mathf.Min(Mathf.Min(a.x, b.x), Mathf.Min(c.x, d.x)) - minDistance;
            float minZ = Mathf.Min(Mathf.Min(a.z, b.z), Mathf.Min(c.z, d.z)) - minDistance;
            float maxX = Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Max(c.x, d.x)) + minDistance;
            float maxZ = Mathf.Max(Mathf.Max(a.z, b.z), Mathf.Max(c.z, d.z)) + minDistance;

            // check if point is in the zone block (+ minDistance tolerance)
            if ((double)point.x <= (double)maxX && (double)point.z <= (double)maxZ && ((double)minX <= (double)point.x && (double)minZ <= (double)point.z))
            {
                for (int row = 0; row < rowCount; ++row)
                {
                    // Calculate distance between row (center) and point
                    Vector3 distancePointToRow = _this.m_position - point + ((float)row - 3.5f) * rowDirection;
                    for (int column = 0; column < columnCount; ++column)
                    {
                        if ((_this.m_valid & 1uL << (row << 3 | column)) != 0uL)
                        {
                            // relative column position
                            Vector3 columnMiddleLength = ((float)column - 3.5f) * columnDirection;

                            // total distance between cell and point
                            Vector3 distancePointToCell = distancePointToRow + columnMiddleLength;

                            // increase distance if cell is shared by adding y component (so that the cell of the other zone block is closer), otherwise set y = 0
                            distancePointToCell.y = (_this.m_shared & 1uL << (row << 3 | column)) == 0uL ? 0.0f : 4f;

                            float newDistance = Vector3.SqrMagnitude(distancePointToCell);

                            // set as new minDistance if it is shorter
                            if ((double)newDistance < (double)minDistanceSq)
                            {
                                minDistanceSq = newDistance;
                            }
                        }
                    }
                }
            }
            return minDistanceSq;
        }

        /// <summary>
        /// Sets a cell to a zone type.
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        [RedirectMethod(true)]
        public static bool SetZone(ref ZoneBlock _this, int x, int z, ItemClass.Zone zone)
        {
            // Calling this method should be avoided! Use SetZoneDeep instead

            return SetZoneDeep(ref _this, FindBlockId(ref _this), x, z, zone);
        }

        public static bool SetZoneDeep(ref ZoneBlock _this, ushort blockID, int x, int z, ItemClass.Zone zone)
        {
            if (zone == ItemClass.Zone.Distant)
            {
                zone = ItemClass.Zone.Unzoned;
            }
            // 0000 0000 0000 0000 0000 0000 00zz zx00
            // 0|0, 2|0 --> 0
            // 0|1, 2|1 --> 4
            // 1|0, 1|2 --> 8
            // 1|1, 3|1 --> 12
            // 0|2, 2|2 --> 16
            int posShift = z << 3 | (x & 1) << 2;

            // 4 bits for every cell to store the zone type
            // that means 16 zone types are the maximum
            // this mask resets the 4 bits of the cell
            ulong invertedCellMask = ~(15UL << posShift);
            if (x < 2) // use zone1
            {
                ulong newZoneMask = _this.m_zone1 & invertedCellMask | (ulong)zone << posShift;
                if (newZoneMask != _this.m_zone1)
                {
                    _this.m_zone1 = newZoneMask;
                    return true;
                }
            }
            else if (x < 4) // use zone2
            {
                ulong newZoneMask = _this.m_zone2 & invertedCellMask | (ulong)zone << posShift;
                if (newZoneMask != _this.m_zone2)
                {
                    _this.m_zone2 = newZoneMask;
                    return true;
                }
            }

            // --- support for deeper zones ---
            else if (x < 6) // use zone3
            {
                if (DataExtension.zones3 != null)
                {
                    ulong newZoneMask = DataExtension.zones3[blockID] & invertedCellMask | (ulong)zone << posShift;
                    if (newZoneMask != DataExtension.zones3[blockID])
                    {
                        DataExtension.zones3[blockID] = newZoneMask;
                        return true;
                    }
                }
            }
            else if (x < 8) // use zone4
            {
                if (DataExtension.zones4 != null)
                {
                    ulong newZoneMask = DataExtension.zones4[blockID] & invertedCellMask | (ulong)zone << posShift;
                    if (newZoneMask != DataExtension.zones4[blockID])
                    {
                        DataExtension.zones4[blockID] = newZoneMask;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the zone type of a cell
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        [RedirectMethod(true)]
        public static ItemClass.Zone GetZone(ref ZoneBlock _this, int x, int z)
        {
            // Calling this method should be avoided! Use GetZoneDeep instead

            return GetZoneDeep(ref _this, FindBlockId(ref _this), x, z);
        }

        public static ItemClass.Zone GetZoneDeep(ref ZoneBlock _this, ushort blockID, int x, int z)
        {
            if(x >= ZoneBlockDetour.GetColumnCount(ref _this)) return ItemClass.Zone.Distant;

            int num = z << 3 | (x & 1) << 2;

            if (x < 2) return (ItemClass.Zone)(_this.m_zone1 >> num & 15L);
            else if (x < 4) return (ItemClass.Zone)(_this.m_zone2 >> num & 15L);

            // --- support for deeper zones ---
            else if (x < 6 && DataExtension.zones3 != null) return (ItemClass.Zone)(DataExtension.zones3[blockID] >> num & 15L);
            else if (x < 8 && DataExtension.zones4 != null) return (ItemClass.Zone)(DataExtension.zones4[blockID] >> num & 15L);

            return ItemClass.Zone.Distant;
        }

        /// <summary>
        /// Called by ZoneManager#TerrainUpdated. Sends the zone cell data to the rendering pipeline (Terrain render system)
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="blockID"></param>
        /// <param name="minX"></param>
        /// <param name="minZ"></param>
        /// <param name="maxX"></param>
        /// <param name="maxZ"></param>
        [RedirectMethod(true)]
        public static void ZonesUpdated(ref ZoneBlock _this, ushort blockID, float minX, float minZ, float maxX, float maxZ)
        {
            // skip zone blocks which are not in use
            if (((int)_this.m_flags & 3) != ZoneBlock.FLAG_CREATED) return;

            // width of the zone block
            int rowCount = _this.RowCount;
            int columnCount = ZoneBlockDetour.GetColumnCount(ref _this); // modified

            Vector2 columnDirection = new Vector2(Mathf.Cos(_this.m_angle), Mathf.Sin(_this.m_angle)) * 8f;
            Vector2 rowDirection = new Vector2(columnDirection.y, -columnDirection.x);

            Vector2 positionXZ = VectorUtils.XZ(_this.m_position);

            // bounds of the zone block
            Vector2 a = positionXZ - 4f * columnDirection - 4f * rowDirection;
            Vector2 b = positionXZ + 0.0f * columnDirection - 4f * rowDirection;
            Vector2 c = positionXZ + 0.0f * columnDirection + (float)(rowCount - 4) * rowDirection;
            Vector2 d = positionXZ - 4f * columnDirection + (float)(rowCount - 4) * rowDirection;
            float minX2 = Mathf.Min(Mathf.Min(a.x, b.x), Mathf.Min(c.x, d.x));
            float minZ2 = Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y));
            float maxX2 = Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Max(c.x, d.x));
            float maxZ2 = Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y));

            // return if the area is not in hitbox range of this block
            if ((double)maxX2 < (double)minX || (double)minX2 > (double)maxX || ((double)maxZ2 < (double)minZ || (double)minZ2 > (double)maxZ))
            {
                return;
            }

            bool isAssetEditor = (Singleton<ToolManager>.instance.m_properties.m_mode & ItemClass.Availability.AssetEditor) != ItemClass.Availability.None;

            // absolute position of cell at row 5, column 5
            Vector2 positionR5C5 = positionXZ + columnDirection * 0.5f + rowDirection * 0.5f;

            // combined masks for valid and occupied
            ulong validMask = _this.m_valid & ~_this.m_shared;
            ulong occupiedMask = _this.m_occupied1 | _this.m_occupied2;

            for (int row = 0; row < rowCount; ++row)
            {
                int row2 = row;

                // calculate relative position between previous row and current row
                Vector2 rowPreviousLength = ((float)row - 4f) * rowDirection;

                for (; row + 1 < rowCount; ++row) // continue cycling through rows
                {
                    int column;
                    for (column = 0; column < columnCount; ++column)
                    {
                        ulong bitOfOuterRow = 1UL << (row2 << 3 | column);
                        ulong bitOfInnerRow = 1UL << (row + 1 << 3 | column);

                        // if the cell (row2) differs from the other cell (row) in any way, break
                        if ((validMask & bitOfOuterRow) != 0UL != ((validMask & bitOfInnerRow) != 0UL)
                            || (occupiedMask & bitOfOuterRow) != 0UL != ((occupiedMask & bitOfInnerRow) != 0UL)
                            || !isAssetEditor && GetZoneDeep(ref _this, blockID, column, row + 1) != GetZoneDeep(ref _this, blockID, column, row2))
                        {
                            break;
                        }
                    }
                    if (column < columnCount) break; // no idea what this does
                }

                // calculate relative position between current row and next row
                Vector2 rowNextLength = ((float)row - 3f) * rowDirection;

                for (int column = 0; column < columnCount; ++column)
                {
                    ulong bitOfOuterColumn = 1UL << (row2 << 3 | column);

                    if ((validMask & bitOfOuterColumn) != 0UL) // if cell is valid
                    {
                        bool occupied = (occupiedMask & bitOfOuterColumn) != 0UL; // is cell occupied?
                        int column2 = column;
                        ItemClass.Zone zone = !isAssetEditor ? GetZoneDeep(ref _this, blockID, column, row2) : ItemClass.Zone.ResidentialLow;

                        // calculate relative position between previous column and current column
                        Vector2 columnPreviousLength = ((float)column - 4f) * columnDirection;

                        for (; column != 3 && column + 1 < 8; ++column)
                        {
                            ulong bitOfInnerColumn = 1UL << (row2 << 3 | column + 1);

                            // break if cell is invalid or occupied status differs from other cell or zone differs
                            if ((validMask & bitOfInnerColumn) == 0UL
                                || (occupiedMask & bitOfInnerColumn) != 0UL != occupied
                                || !isAssetEditor && GetZoneDeep(ref _this, blockID, column + 1, row2) != zone)
                            {
                                break;
                            }
                        }

                        // calculate relative position between current column and next column
                        Vector2 columnNextLength = ((float)column - 3f) * columnDirection;

                        // Send zone cell to rendering pipeline
                        TerrainModify.ApplyQuad(positionXZ + columnPreviousLength + rowPreviousLength, positionXZ + columnNextLength + rowPreviousLength,
                            positionXZ + columnNextLength + rowNextLength, positionXZ + columnPreviousLength + rowNextLength,
                            zone, occupied, _this.m_angle, positionR5C5, columnDirection, rowDirection, 4 - column, 4 - column2, 4 - row, 4 - row2);
                    }
                }
            }
        }

        /// <summary>
        /// Compares zone block with other block.
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="other"></param>
        /// <param name="xBuffer"></param>
        /// <param name="zone"></param>
        /// <param name="startPos"></param>
        /// <param name="xDir"></param>
        /// <param name="zDir"></param>
        /// <param name="quad"></param>
        private static void CheckBlock(ref ZoneBlock _this, ushort otherBlockID, ref ZoneBlock other, int[] xBuffer, ItemClass.Zone zone, Vector2 startPos, Vector2 xDir, Vector2 zDir, Quad2 quad)
        {
            // difference of 2 radian angles (360 deg = 2*PI * 0.6366197f = 4f)
            // that means an angle difference of 90 deg would result in 1f
            float angleDiff = Mathf.Abs(other.m_angle - _this.m_angle) * 0.6366197f;
            float rightAngleDiff = angleDiff - Mathf.Floor(angleDiff);

            // check if the zone block and the other zone block are in right angle (0 90 180 270 deg), otherwise return
            if ((double)rightAngleDiff >= 0.00999999977648258 && (double)rightAngleDiff <= 0.990000009536743) return;

            // width of other block
            int otherRowCount = other.RowCount;
            int otherColumnCount = ZoneBlockDetour.GetColumnCount(ref other); // modified

            Vector2 otherColumnDirection = new Vector2(Mathf.Cos(other.m_angle), Mathf.Sin(other.m_angle)) * 8f;
            Vector2 otherRowDirection = new Vector2(otherColumnDirection.y, -otherColumnDirection.x);

            ulong otherValidFreeCellMask = other.m_valid & ~(other.m_occupied1 | other.m_occupied2);

            Vector2 otherPositionXZ = VectorUtils.XZ(other.m_position);

            // check if the zone block quad of the other block intersects with the zone block, otherwise return
            if (!quad.Intersect(new Quad2()
            {
                a = otherPositionXZ - 4f * otherColumnDirection - 4f * otherRowDirection,
                b = otherPositionXZ + (otherColumnCount - 4f) * otherColumnDirection - 4f * otherRowDirection,
                c = otherPositionXZ + (otherColumnCount - 4f) * otherColumnDirection + (float)(otherRowCount - 4) * otherRowDirection,
                d = otherPositionXZ - 4f * otherColumnDirection + (float)(otherRowCount - 4) * otherRowDirection
            }))
            {
                return;
            }

            // Cycle through all cells of the other block
            for (int row = 0; row < otherRowCount; ++row)
            {
                Vector2 rowMiddleLength = ((float)row - 3.5f) * otherRowDirection;
                for (int column = 0; column < otherColumnCount; ++column)
                {
                    // check if the cell is unoccupied and zoned correctly
                    if ((otherValidFreeCellMask & 1UL << (row << 3 | column)) != 0UL && GetZoneDeep(ref other, otherBlockID, column, row) == zone)
                    {
                        Vector2 columnMiddleLength = ((float)column - 3.5f) * otherColumnDirection;

                        // Calculate the distance between the seed point and the current cell
                        Vector2 cellStartPosDistance = otherPositionXZ + columnMiddleLength + rowMiddleLength - startPos;

                        // dot product divided by 8*8 (normalized to cell size)
                        // cell distance in x direction between the seed cell and the current cell
                        float distColumnDirection = (float)(((double)cellStartPosDistance.x * (double)xDir.x + (double)cellStartPosDistance.y * (double)xDir.y) * (1.0 / 64.0));

                        // cell distance in z direction (rowDirection between the seed cell and the current cell
                        float distRowDirection = (float)(((double)cellStartPosDistance.x * (double)zDir.x + (double)cellStartPosDistance.y * (double)zDir.y) * (1.0 / 64.0));

                        // rounded distances
                        int roundedDistColumnDirection = Mathf.RoundToInt(distColumnDirection); // must be >=0 (behind road) and <=6 (7 cells to the back)
                        int roundedDistRowDirection = Mathf.RoundToInt(distRowDirection); // must be >=-6 and <=6 (6 cells to the left or 6 cells to the right)

                        // TODO raise numbers and array size for 8x8 lot support

                        if (roundedDistColumnDirection >= 0 && roundedDistColumnDirection <= 6 && (roundedDistRowDirection >= -6 && roundedDistRowDirection <= 6)
                            // cells must be aligned in the same grid + 1% tolerance
                            && ((double)Mathf.Abs(distColumnDirection - (float)roundedDistColumnDirection) < 0.0125000001862645
                            && (double)Mathf.Abs(distRowDirection - (float)roundedDistRowDirection) < 0.0125000001862645
                            // must have road access or be behind one of the cells touching the road of the seed block 
                            // column == 0 means access to the road belonging to the zone block
                            // roundedDistColumnDirection != 0
                            && (column == 0 || roundedDistColumnDirection != 0)))
                        {
                            // Mark the cell in the column mask (in the row buffer array)
                            xBuffer[roundedDistRowDirection + 6] |= 1 << roundedDistColumnDirection;

                            // If the column touches the road, also mark it in the second part of the int mask
                            if (column == 0)
                            {
                                xBuffer[roundedDistRowDirection + 6] |= 1 << roundedDistColumnDirection + 16; // shift by 16
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if position has access to electricity.
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private static bool IsGoodPlace(ref ZoneBlock _this, Vector2 position)
        {
            // calculate which building grid cells are in range of this position
            int gridMinX = Mathf.Max((int)(((double)position.x - 104.0) / 64.0 + 135.0), 0);
            int gridMinZ = Mathf.Max((int)(((double)position.y - 104.0) / 64.0 + 135.0), 0);
            int gridMaxX = Mathf.Min((int)(((double)position.x + 104.0) / 64.0 + 135.0), 269);
            int gridMaxZ = Mathf.Min((int)(((double)position.y + 104.0) / 64.0 + 135.0), 269);

            Array16<Building> buildings = Singleton<BuildingManager>.instance.m_buildings;
            ushort[] buildingGrid = Singleton<BuildingManager>.instance.m_buildingGrid;

            // Cycle through all relevant grid cells
            for (int gridZ = gridMinZ; gridZ <= gridMaxZ; ++gridZ)
            {
                for (int gridX = gridMinX; gridX <= gridMaxX; ++gridX)
                {
                    // Cycle through all buildings in grid cell
                    ushort buildingID = buildingGrid[gridZ * 270 + gridX];
                    int counter = 0;
                    while ((int)buildingID != 0)
                    {
                        // only look at existing buildings
                        if ((buildings.m_buffer[(int)buildingID].m_flags & (Building.Flags.Created | Building.Flags.Deleted)) == Building.Flags.Created)
                        {
                            BuildingInfo info;
                            int width;
                            int length;
                            buildings.m_buffer[(int)buildingID].GetInfoWidthLength(out info, out width, out length);

                            if (info != null)
                            {
                                // check if spot has access to electricity
                                float electricityGridRadius = info.m_buildingAI.ElectricityGridRadius();
                                if ((double)electricityGridRadius > 0.100000001490116 || info.m_class.m_service == ItemClass.Service.Electricity)
                                {
                                    Vector2 buildingPositionXZ = VectorUtils.XZ(buildings.m_buffer[(int)buildingID].m_position);
                                    float radius2 = Mathf.Max(8f, electricityGridRadius) + 32f;
                                    if ((double)Vector2.SqrMagnitude(position - buildingPositionXZ) < (double)radius2 * (double)radius2)
                                        return true;
                                }
                            }
                        }
                        // next building in grid cell (linked list)
                        buildingID = buildings.m_buffer[(int)buildingID].m_nextGridBuilding;

                        if (++counter >= 49152)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Spawns new growables on empty zones.
        /// </summary>
        /// <param name="_this"></param>
        /// <param name="blockID"></param>
        [RedirectMethod(true)]
        public static void SimulationStep(ref ZoneBlock _this, ushort blockID)
        {
            ZoneManager zoneManager = Singleton<ZoneManager>.instance;

            // width of the zone block
            int rowCount = _this.RowCount;

            // directions of the rows and columns based on zone block angle, multiplied by 8 (cell size)
            Vector2 columnDirection = new Vector2(Mathf.Cos(_this.m_angle), Mathf.Sin(_this.m_angle)) * 8f;
            Vector2 rowDirection = new Vector2(columnDirection.y, -columnDirection.x);

            // bitmask of valid cells that are not occupied
            ulong validFreeCellMask = _this.m_valid & ~(_this.m_occupied1 | _this.m_occupied2);

            // select a random zoned, unoccupied row and get its zone type
            // this will be our seed row
            int seedRow = 0;
            ItemClass.Zone zone = ItemClass.Zone.Unzoned;
            for (int index = 0; index < 4 && zone == ItemClass.Zone.Unzoned; ++index)
            {
                seedRow = Singleton<SimulationManager>.instance.m_randomizer.Int32((uint)rowCount);
                if ((validFreeCellMask & 1UL << (seedRow << 3)) != 0UL)
                {
                    zone = GetZoneDeep(ref _this, blockID, 0, seedRow);
                }
            }

            // get the demand for the given zone type in the district
            DistrictManager districtManager = Singleton<DistrictManager>.instance;
            byte district = districtManager.GetDistrict(_this.m_position);
            int demand;
            switch (zone)
            {
                case ItemClass.Zone.ResidentialLow:
                    demand = zoneManager.m_actualResidentialDemand + districtManager.m_districts.m_buffer[(int)district].CalculateResidentialLowDemandOffset();
                    break;
                case ItemClass.Zone.ResidentialHigh:
                    demand = zoneManager.m_actualResidentialDemand + districtManager.m_districts.m_buffer[(int)district].CalculateResidentialHighDemandOffset();
                    break;
                case ItemClass.Zone.CommercialLow:
                    demand = zoneManager.m_actualCommercialDemand + districtManager.m_districts.m_buffer[(int)district].CalculateCommercialLowDemandOffset();
                    break;
                case ItemClass.Zone.CommercialHigh:
                    demand = zoneManager.m_actualCommercialDemand + districtManager.m_districts.m_buffer[(int)district].CalculateCommercialHighDemandOffset();
                    break;
                case ItemClass.Zone.Industrial:
                    demand = zoneManager.m_actualWorkplaceDemand + districtManager.m_districts.m_buffer[(int)district].CalculateIndustrialDemandOffset();
                    break;
                case ItemClass.Zone.Office:
                    demand = zoneManager.m_actualWorkplaceDemand + districtManager.m_districts.m_buffer[(int)district].CalculateOfficeDemandOffset();
                    break;
                default:
                    return;
            }

            // origin of the zone block
            Vector2 positionXZ = VectorUtils.XZ(_this.m_position);

            // middle position of random row (roadside seed cell)
            Vector2 seedCellMiddlePosition = positionXZ - 3.5f * columnDirection + ((float)seedRow - 3.5f) * rowDirection;

            // This buffer contains 13 masks (for 13 rows)
            // The masks are split into two 16-bit segments
            // The higher 16 bits store which columns have road access
            // The lower 16 bits store which columns are unoccupied
            int[] xBuffer = zoneManager.m_tmpXBuffer; // TODO maybe use a bigger buffer?
            for (int index = 0; index < 13; ++index) xBuffer[index] = 0; // reset the buffer

            // put the surrounding area of the seed cell into a quad
            // TODO maybe check a bigger area?
            Quad2 seedPointAreaQuad = new Quad2
            {
                a = positionXZ - 4f * columnDirection + ((float)seedRow - 10f) * rowDirection,
                b = positionXZ + 3f * columnDirection + ((float)seedRow - 10f) * rowDirection,
                c = positionXZ + 3f * columnDirection + ((float)seedRow + 2f) * rowDirection,
                d = positionXZ - 4f * columnDirection + ((float)seedRow + 2f) * rowDirection
            };
            Vector2 seedPointAreaMin = seedPointAreaQuad.Min();
            Vector2 seedPointAreaMax = seedPointAreaQuad.Max();

            // calculate which zone block grid cells are touched by this zone block
            int gridMinX = Mathf.Max((int)(((double)seedPointAreaMin.x - 46.0) / 64.0 + 75.0), 0);
            int gridMinZ = Mathf.Max((int)(((double)seedPointAreaMin.y - 46.0) / 64.0 + 75.0), 0);
            int gridMaxX = Mathf.Min((int)(((double)seedPointAreaMax.x + 46.0) / 64.0 + 75.0), 149);
            int gridMaxZ = Mathf.Min((int)(((double)seedPointAreaMax.y + 46.0) / 64.0 + 75.0), 149);

            // Cycle through all touched grid cells
            for (int gridZ = gridMinZ; gridZ <= gridMaxZ; ++gridZ)
            {
                for (int gridX = gridMinX; gridX <= gridMaxX; ++gridX)
                {
                    // Cycle through all zone blocks in grid cell
                    ushort otherBlockID = zoneManager.m_zoneGrid[gridZ * 150 + gridX];
                    int counter = 0;
                    while ((int)otherBlockID != 0)
                    {
                        Vector3 otherPosition = zoneManager.m_blocks.m_buffer[(int)otherBlockID].m_position;

                        // check if other zone block is in range
                        if ((double)Mathf.Max(Mathf.Max(seedPointAreaMin.x - 46f - otherPosition.x, seedPointAreaMin.y - 46f - otherPosition.z),
                            Mathf.Max((float)((double)otherPosition.x - (double)seedPointAreaMax.x - 46.0), (float)((double)otherPosition.z - (double)seedPointAreaMax.y - 46.0))) < 0.0)
                        {
                            // Checks if the other block intersects and extends this block (orthogonal) and marks unoccupied cells and cells with road access in the XBuffer
                            CheckBlock(ref _this, otherBlockID, ref zoneManager.m_blocks.m_buffer[(int)otherBlockID], xBuffer, zone, seedCellMiddlePosition, columnDirection, rowDirection, seedPointAreaQuad);
                        }

                        // next zone block in grid cell (linked list)
                        otherBlockID = zoneManager.m_blocks.m_buffer[(int)otherBlockID].m_nextGridBlock;

                        if (++counter >= 49152)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            for (int row = 0; row < 13; ++row)
            {
                uint columnMask = (uint)xBuffer[row];
                int columnCount = 0; // counts the unoccupied columns

                // check if the first the 2 cells in the column have road access
                // that means the area is suitable for corner buildings
                // 196608 = 0000 0000 0000 0011 0000 0000 0000 0000
                bool cornerRoadAccess = ((int)columnMask & 196608) == 196608;

                // stores if the last checked cell has road access
                bool backsideRoadAccess = false;

                // count unoccupied cells in this column
                while (((int)columnMask & 1) != 0)
                {
                    ++columnCount;

                    // check if the cell has road access
                    // 65536 = 0000 0000 0000 0001 0000 0000 0000 0000
                    backsideRoadAccess = ((int)columnMask & 65536) != 0;

                    // move on to the next cell
                    columnMask >>= 1;
                }

                if (columnCount == 5 || columnCount == 6)
                {
                    // if the last checked cell has road access, decrease max depth to number between 2 and 4, otherwise set it to 4
                    // always set the "depth shortened flag" (131072)
                    columnCount = (!backsideRoadAccess ? 4 : columnCount - (Singleton<SimulationManager>.instance.m_randomizer.Int32(2U) + 2)) | 131072;
                }
                else if (columnCount == 7)
                {
                    // 131072 = 0000 0000 0000 0010 0000 0000 0000 0000
                    // set the "depth shortened flag" and and set max depth to 4
                    columnCount = 4 | 131072;
                }
                // TODO add support for larger lots! (8,9,10,11,12,13,14,15)
                if (cornerRoadAccess)
                {
                    // set corner flag
                    // 65536 = 0000 0000 0000 0001 0000 0000 0000 0000
                    columnCount |= 65536;
                }

                // store result in buffer
                xBuffer[row] = columnCount;
            }

            // use bitmask to read depth at seed row
            // 0000 0000 0000 0000 1111 1111 1111 1111 
            int targetColumnCount = xBuffer[6] & 65535;

            // all columns at seed row occupied? bad seed row!
            if (targetColumnCount == 0) return;

            // checks if there is electricity available
            bool isGoodPlace = IsGoodPlace(ref _this, seedCellMiddlePosition);

            // if demand is too low, only report that a good area was found and return
            // (or instantly return if area is bad)
            if (Singleton<SimulationManager>.instance.m_randomizer.Int32(100U) >= demand)
            {
                if (isGoodPlace) // no electricity? fuck this.
                {
                    zoneManager.m_goodAreaFound[(int)zone] = (short)1024; // note that a good area was found
                }
                return;
            }
            // if area is bad but a good area was found some time ago
            else if (!isGoodPlace && (int)zoneManager.m_goodAreaFound[(int)zone] > -1024)
            {
                if ((int)zoneManager.m_goodAreaFound[(int)zone] == 0)
                {
                    zoneManager.m_goodAreaFound[(int)zone] = (short)-1; // note that no good area was found today
                }
                return;
            }
            // let's spawn a building!
            // STEP 1: this calculates a left and right row range for the building to spawn
            else
            {
                int calculatedLeftRow = 6;
                int calculatedRightRow = 6;
                bool firstTry = true;

                // search loop for plot size finding
                // The goal is to avoid one cell width holes in the zone grid
                // the minimum targeted width is 2
                while (true)
                {
                    if (firstTry) // in first try search for exact matching rows
                    {
                        // search for rows left and right of seed row with a similar depth
                        while (calculatedLeftRow != 0 && (xBuffer[calculatedLeftRow - 1] & 65535) == targetColumnCount)
                            --calculatedLeftRow;
                        while (calculatedRightRow != 12 && (xBuffer[calculatedRightRow + 1] & 65535) == targetColumnCount)
                            ++calculatedRightRow;
                    }
                    else // in the second/third try search for any matching rows
                    {
                        // search for rows left and right of seed row with a similar or larger depth
                        while (calculatedLeftRow != 0 && (xBuffer[calculatedLeftRow - 1] & 65535) >= targetColumnCount)
                            --calculatedLeftRow;
                        while (calculatedRightRow != 12 && (xBuffer[calculatedRightRow + 1] & 65535) >= targetColumnCount)
                            ++calculatedRightRow;
                    }

                    int extraLeftRange = calculatedLeftRow;
                    int extraRightRange = calculatedRightRow;
                    // search for further rows with a min depth of 2
                    while (extraLeftRange != 0 && (xBuffer[extraLeftRange - 1] & 65535) >= 2)
                        --extraLeftRange;
                    while (extraRightRange != 12 && (xBuffer[extraRightRange + 1] & 65535) >= 2)
                        ++extraRightRange;

                    // checks if a exactly one single extra row with min depth of 2 was found
                    // if that is the case, the algorithm will try to preserve space on that side so a 2 cells width building can fit
                    bool exactlyOneRowLeftFound = extraLeftRange != 0 && extraLeftRange == calculatedLeftRow - 1;
                    bool exactlyOneRowRightFound = extraRightRange != 12 && extraRightRange == calculatedRightRow + 1;

                    // 1-cell space found on both sides
                    // goal: preserve space on both sides
                    if (exactlyOneRowLeftFound && exactlyOneRowRightFound)
                    {
                        // if 3 or less regular rows found
                        if (calculatedRightRow - calculatedLeftRow <= 2)
                        {
                            // if target depth 2 or less
                            if (targetColumnCount <= 2)
                            {
                                if (!firstTry) // if second try
                                {
                                    // 1x1, 1x2, 2x1, 2x2, 3x1, 3x2
                                    goto selectRandomRows;  // --> next step
                                }

                                // --> search again (exact mode off)
                            }
                            // if target depth is at least 3 (but only 3 or less rows found)
                            else
                            {
                                // decrease target depth by one
                                --targetColumnCount;

                                // --> search again (exact mode off)
                            }
                        }
                        // 4 regular rows found 
                        else
                        {
                            // reserve space on both sides for 2-cell wide buildings
                            // (++rowLeft;--rowRight;)
                            break; // --> next step
                        }
                    }

                    // 1-cell space found on left side only
                    else if (exactlyOneRowLeftFound)
                    {
                        // if 2 or less regular rows found
                        if (calculatedRightRow - calculatedLeftRow <= 1)
                        {
                            // if target depth 2 or less
                            if (targetColumnCount <= 2)
                            {
                                if (!firstTry) // if second try
                                {
                                    goto selectRandomRows;  // --> next step
                                }

                                // --> search again (exact mode off)
                            }
                            // if target depth is at least 3 (but only 2 or less rows found)
                            else
                            {
                                // decrease target depth by one
                                --targetColumnCount;

                                // --> search again (exact mode off)
                            }
                        }
                        // 4 regular rows found 
                        else
                        {
                            // reserve space on left side for 2-cell wide buildings
                            // (++rowLeft;)
                            goto selectRandomRowsPreserveL;  // --> next step
                        }
                    }

                    // 1-cell space found on right side only
                    else if (exactlyOneRowRightFound)
                    {
                        // if 2 or less regular rows found
                        if (calculatedRightRow - calculatedLeftRow <= 1)
                        {
                            // if target depth 2 or less
                            if (targetColumnCount <= 2)
                            {
                                if (!firstTry) // if second try
                                {
                                    goto selectRandomRows;  // --> next step
                                }
                            }
                            // if target depth is at least 3 (but only 2 or less rows found)
                            else
                            {
                                // decrease target depth by one
                                --targetColumnCount;

                                // --> search again (exact mode off)
                            }
                        }
                        // 4 regular rows found 
                        else
                        {
                            // reserve space on right side for 2-cell wide buildings
                            // (--rowRight;)
                            goto selectRandomRowsPreserveR;  // --> next step
                        }
                    }
                    // only one row found
                    // we don't want 1-cell wide buildings!
                    else if (calculatedLeftRow == calculatedRightRow)
                    {
                        // if target depth 2 or less
                        if (targetColumnCount <= 2)
                        {
                            if (!firstTry) // if second try
                            {
                                goto selectRandomRows;  // --> next step
                            }

                            // --> search again (exact mode off)
                        }
                        // if target depth is at least 3 (but only 1 row found)
                        else
                        {
                            --targetColumnCount;

                            // --> search again (exact mode off)
                        }
                    }
                    // no 1-cell spaces found. Everything is ok!
                    else
                    {
                        goto selectRandomRows;  // --> next step
                    }

                    firstTry = false; // turn off exact mode and search again
                }

                // fix 1-cell space on both sides
                // selectRandomRowsPreserveBoth:
                ++calculatedLeftRow;
                --calculatedRightRow;
                goto selectRandomRows;

                // fix 1-cell space on left side
                selectRandomRowsPreserveL:
                ++calculatedLeftRow;
                goto selectRandomRows;

                // fix 1-cell space on right side
                selectRandomRowsPreserveR:
                --calculatedRightRow;

                // NEXT STEP: Create an alternative randomized row range for the building to spawn
                // (alternative width and spawn position)
                // Goal: Leave no small gaps
                selectRandomRows:

                // the randomized row values
                int randomozedLeftRow;
                int randomizedRightRow;

                // if only one cell deep, but 2 or more cells wide
                // select one of the rows and set the width to 1
                if (targetColumnCount == 1 && calculatedRightRow - calculatedLeftRow >= 1)
                {
                    // select a random row in the valid range
                    calculatedLeftRow += Singleton<SimulationManager>.instance.m_randomizer.Int32((uint)(calculatedRightRow - calculatedLeftRow));
                    calculatedRightRow = calculatedLeftRow + 1;
                    randomozedLeftRow = calculatedLeftRow + Singleton<SimulationManager>.instance.m_randomizer.Int32(2U);
                    randomizedRightRow = randomozedLeftRow;
                }
                else
                {
                    do
                    {
                        randomozedLeftRow = calculatedLeftRow;
                        randomizedRightRow = calculatedRightRow;
                        if (calculatedRightRow - calculatedLeftRow == 2) // 3 cells wide
                        {
                            // coin toss: reduce width by 1 (taking only from one side)
                            if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2U) == 0)
                                --randomizedRightRow;
                            else
                                ++randomozedLeftRow;
                        }
                        else if (calculatedRightRow - calculatedLeftRow == 3) // 4 cells wide
                        {
                            // coin toss: reduce width by 2 (taking only from one side)
                            if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2U) == 0)
                                randomizedRightRow -= 2;
                            else
                                randomozedLeftRow += 2;
                        }
                        else if (calculatedRightRow - calculatedLeftRow == 4) // 5 cells wide
                        {
                            // coin toss: reduce width by 2 from one side and 3 from the other
                            if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2U) == 0)
                            {
                                calculatedRightRow -= 2;
                                randomizedRightRow -= 3;
                            }
                            else
                            {
                                calculatedLeftRow += 2;
                                randomozedLeftRow += 3;
                            }
                        }
                        else if (calculatedRightRow - calculatedLeftRow == 5) // 6 cells wide
                        {
                            // coin toss: reduce width by 2 from one side and 3 from the other
                            if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2U) == 0)
                            {
                                calculatedRightRow -= 3;
                                randomizedRightRow -= 2;
                            }
                            else
                            {
                                calculatedLeftRow += 3;
                                randomozedLeftRow += 2;
                            }
                        }
                        else if (calculatedRightRow - calculatedLeftRow >= 6) // 7 cells wide
                        {
                            // check if one range is far away from seed point
                            // reduce width by 2 from that side, also reduce that range by 3
                            if (calculatedLeftRow == 0 || calculatedRightRow == 12)
                            {
                                if (calculatedLeftRow == 0)
                                {
                                    calculatedLeftRow = 3;
                                    randomozedLeftRow = 2;
                                }
                                if (calculatedRightRow == 12)
                                {
                                    calculatedRightRow = 9;
                                    randomizedRightRow = 10;
                                }
                            }

                            // otherwise:
                            // coin toss: reduce width by 2 from one side and 3 from the other
                            else if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2U) == 0)
                            {
                                calculatedRightRow = calculatedLeftRow + 3;
                                randomizedRightRow = randomozedLeftRow + 2;
                            }
                            else
                            {
                                calculatedLeftRow = calculatedRightRow - 3;
                                randomozedLeftRow = randomizedRightRow - 2;
                            }
                        }
                    }

                    // do this while the selected width or the width range are greater than 4
                    // TODO this needs to be changed to 8
                    while (calculatedRightRow - calculatedLeftRow > 3 || randomizedRightRow - randomozedLeftRow > 3);
                }


                // STEP 3: Calculate final position, width, depth and zoning mode based on calculated row range
                int calculatedDepth = 4;
                int calculatedWidth = calculatedRightRow - calculatedLeftRow + 1;
                BuildingInfo.ZoningMode calculatedZoningMode = BuildingInfo.ZoningMode.Straight;

                // stores if there is reserve space for a higher depth
                bool calculatedSpaceBehindAllColumns = true;

                for (int row = calculatedLeftRow; row <= calculatedRightRow; ++row)
                {
                    // calculate the maximum possible depth in the range
                    calculatedDepth = Mathf.Min(calculatedDepth, xBuffer[row] & 65535);
                    if ((xBuffer[row] & 131072) == 0) // check for depth shortened flag
                    {
                        calculatedSpaceBehindAllColumns = false;
                    }
                }

                if (calculatedRightRow > calculatedLeftRow) // width at least 2
                {
                    // check for left corner flag
                    if ((xBuffer[calculatedLeftRow] & 65536) != 0)
                    {
                        // move building to left side, set corner mode
                        calculatedZoningMode = BuildingInfo.ZoningMode.CornerLeft;
                        randomizedRightRow = calculatedLeftRow + randomizedRightRow - randomozedLeftRow;
                        randomozedLeftRow = calculatedLeftRow;
                    }

                    // check for right corner flag (coin toss if left corner flag found)
                    if ((xBuffer[calculatedRightRow] & 65536) != 0 && (calculatedZoningMode != BuildingInfo.ZoningMode.CornerLeft || Singleton<SimulationManager>.instance.m_randomizer.Int32(2U) == 0))
                    {
                        // move building to right side, set corner mode
                        calculatedZoningMode = BuildingInfo.ZoningMode.CornerRight;
                        randomozedLeftRow = calculatedRightRow + randomozedLeftRow - randomizedRightRow;
                        randomizedRightRow = calculatedRightRow;
                    }
                }

                // STEP 4: Calculate final position, width, depth and zoning mode based on randomized row range

                int randomizedDepth = 4;
                int randomizedWidth = randomizedRightRow - randomozedLeftRow + 1;
                BuildingInfo.ZoningMode randomizedZoningMode = BuildingInfo.ZoningMode.Straight;

                // stores if there is reserve space for a higher depth
                bool randomizedSpaceBehindAllColumns = true;

                for (int row = randomozedLeftRow; row <= randomizedRightRow; ++row)
                {
                    // calculate the maximum possible depth in the range
                    randomizedDepth = Mathf.Min(randomizedDepth, xBuffer[row] & (int)ushort.MaxValue);

                    if ((xBuffer[row] & 131072) == 0) // check for depth shortened flag
                    {
                        randomizedSpaceBehindAllColumns = false;
                    }
                }
                if (randomizedRightRow > randomozedLeftRow) // width at least 2
                {
                    // check for left corner flag
                    if ((xBuffer[randomozedLeftRow] & 65536) != 0)
                    {
                        randomizedZoningMode = BuildingInfo.ZoningMode.CornerLeft; // set corner mode
                    }

                    // check for right corner flag (coin toss if left corner flag found)
                    if ((xBuffer[randomizedRightRow] & 65536) != 0 && (randomizedZoningMode != BuildingInfo.ZoningMode.CornerLeft || Singleton<SimulationManager>.instance.m_randomizer.Int32(2U) == 0))
                    {
                        randomizedZoningMode = BuildingInfo.ZoningMode.CornerRight; // set corner mode
                    }
                }

                // STEP 5: Assemble ItemClass information
                ItemClass.SubService subService = ItemClass.SubService.None;
                ItemClass.Level level = ItemClass.Level.Level1;
                ItemClass.Service service;
                switch (zone)
                {
                    case ItemClass.Zone.ResidentialLow:
                        service = ItemClass.Service.Residential;
                        subService = ItemClass.SubService.ResidentialLow;
                        break;
                    case ItemClass.Zone.ResidentialHigh:
                        service = ItemClass.Service.Residential;
                        subService = ItemClass.SubService.ResidentialHigh;
                        break;
                    case ItemClass.Zone.CommercialLow:
                        service = ItemClass.Service.Commercial;
                        subService = ItemClass.SubService.CommercialLow;
                        break;
                    case ItemClass.Zone.CommercialHigh:
                        service = ItemClass.Service.Commercial;
                        subService = ItemClass.SubService.CommercialHigh;
                        break;
                    case ItemClass.Zone.Industrial:
                        service = ItemClass.Service.Industrial;
                        break;
                    case ItemClass.Zone.Office:
                        service = ItemClass.Service.Office;
                        subService = ItemClass.SubService.None;
                        break;
                    default:
                        return;
                }

                // STEP 6: Find a prefab using either calculated or randomized size, spawn position and zoning mode
                // The fallback if no corner was found is always straight mode

                BuildingInfo info = (BuildingInfo)null;
                Vector3 buildingSpawnPos = Vector3.zero;

                int finalSpawnRowDouble = 0; // this is the doubled relative spawn position
                int finalDepth = 0;
                int finalWidth = 0;
                BuildingInfo.ZoningMode finalZoningMode = BuildingInfo.ZoningMode.Straight;

                for (int iteration = 0; iteration < 6; ++iteration)
                {
                    switch (iteration)
                    {
                        case 0: //corner, calculated
                            if (calculatedZoningMode != BuildingInfo.ZoningMode.Straight)
                            {
                                finalSpawnRowDouble = calculatedLeftRow + calculatedRightRow + 1;
                                finalDepth = calculatedDepth;
                                finalWidth = calculatedWidth;
                                finalZoningMode = calculatedZoningMode;
                                goto default;
                            }
                            else
                                break;
                        case 1: //corner, randomized
                            if (randomizedZoningMode != BuildingInfo.ZoningMode.Straight)
                            {
                                finalSpawnRowDouble = randomozedLeftRow + randomizedRightRow + 1;
                                finalDepth = randomizedDepth;
                                finalWidth = randomizedWidth;
                                finalZoningMode = randomizedZoningMode;
                                goto default;
                            }
                            else
                                break;
                        case 2: //corner, calculated, limited depth
                            if (calculatedZoningMode != BuildingInfo.ZoningMode.Straight && calculatedDepth >= 4)
                            {
                                finalSpawnRowDouble = calculatedLeftRow + calculatedRightRow + 1;
                                finalDepth = !calculatedSpaceBehindAllColumns ? 2 : 3; // prevent 1-cell gaps
                                finalWidth = calculatedWidth;
                                finalZoningMode = calculatedZoningMode;
                                goto default;
                            }
                            else
                                break;
                        case 3: //corner, randomized, limited depth
                            if (randomizedZoningMode != BuildingInfo.ZoningMode.Straight && randomizedDepth >= 4)
                            {
                                finalSpawnRowDouble = randomozedLeftRow + randomizedRightRow + 1;
                                finalDepth = !randomizedSpaceBehindAllColumns ? 2 : 3; // prevent 1-cell gaps
                                finalWidth = randomizedWidth;
                                finalZoningMode = randomizedZoningMode;
                                goto default;
                            }
                            else
                                break;
                        case 4: // straight, calculated
                            finalSpawnRowDouble = calculatedLeftRow + calculatedRightRow + 1;
                            finalDepth = calculatedDepth;
                            finalWidth = calculatedWidth;
                            finalZoningMode = BuildingInfo.ZoningMode.Straight;
                            goto default;
                        case 5: // straight, randomized
                            finalSpawnRowDouble = randomozedLeftRow + randomizedRightRow + 1;
                            finalDepth = randomizedDepth;
                            finalWidth = randomizedWidth;
                            finalZoningMode = BuildingInfo.ZoningMode.Straight;
                            goto default;
                        default:
                            // calculate building spawn position (plot center)
                            buildingSpawnPos = _this.m_position + VectorUtils.X_Y(
                                (float)((double)finalDepth * 0.5 - 4.0) * columnDirection +
                                (float)((double)finalSpawnRowDouble * 0.5 + ((double)seedRow - 6) - 4) * rowDirection);

                            // industrial specialisations
                            if (zone == ItemClass.Zone.Industrial)
                            {
                                ZoneBlock.GetIndustryType(buildingSpawnPos, out subService, out level);
                            }
                            // commercial specialisations
                            else if (zone == ItemClass.Zone.CommercialLow || zone == ItemClass.Zone.CommercialHigh)
                            {
                                ZoneBlock.GetCommercialType(buildingSpawnPos, zone, finalWidth, finalDepth, out subService, out level);
                            }

                            // get district style
                            byte buildingDistrict = districtManager.GetDistrict(buildingSpawnPos);
                            ushort style = districtManager.m_districts.m_buffer[(int)buildingDistrict].m_Style;

                            // find random building
                            info = Singleton<BuildingManager>.instance.GetRandomBuildingInfo(ref Singleton<SimulationManager>.instance.m_randomizer, service, subService, level, finalWidth, finalDepth, finalZoningMode, (int)style);

                            // no building found? go back to switch statement and use different calculations
                            if (info == null) break;

                            // spawn the building!
                            goto spawnBuilding;
                    }
                }

                // STEP 7: Spawn the building!
                spawnBuilding:

                // No underwater buildings!
                if (info == null || (double)Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(buildingSpawnPos)) > (double)buildingSpawnPos.y) return;

                // Rotate corner buildings correctly
                float angle = _this.m_angle + 1.570796f; // 0.5*PI = 180 deg
                if (finalZoningMode == BuildingInfo.ZoningMode.CornerLeft && info.m_zoningMode == BuildingInfo.ZoningMode.CornerRight)
                {
                    angle -= 1.570796f;
                    finalDepth = finalWidth;
                }
                else if (finalZoningMode == BuildingInfo.ZoningMode.CornerRight && info.m_zoningMode == BuildingInfo.ZoningMode.CornerLeft)
                {
                    angle += 1.570796f;
                    finalDepth = finalWidth;
                }

                // Try to spawn the building
                ushort buildingID;
                if (Singleton<BuildingManager>.instance.CreateBuilding(out buildingID, ref Singleton<SimulationManager>.instance.m_randomizer, info, buildingSpawnPos, angle, finalDepth, Singleton<SimulationManager>.instance.m_currentBuildIndex))
                {
                    ++Singleton<SimulationManager>.instance.m_currentBuildIndex;

                    // Lower demand
                    switch (service)
                    {
                        case ItemClass.Service.Residential:
                            zoneManager.m_actualResidentialDemand = Mathf.Max(0, zoneManager.m_actualResidentialDemand - 5);
                            break;
                        case ItemClass.Service.Commercial:
                            zoneManager.m_actualCommercialDemand = Mathf.Max(0, zoneManager.m_actualCommercialDemand - 5);
                            break;
                        case ItemClass.Service.Industrial:
                            zoneManager.m_actualWorkplaceDemand = Mathf.Max(0, zoneManager.m_actualWorkplaceDemand - 5);
                            break;
                        case ItemClass.Service.Office:
                            zoneManager.m_actualWorkplaceDemand = Mathf.Max(0, zoneManager.m_actualWorkplaceDemand - 5);
                            break;
                    }

                    // Apply high-density building flag
                    switch (zone)
                    {
                        case ItemClass.Zone.ResidentialHigh:
                        case ItemClass.Zone.CommercialHigh:
                            Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)buildingID].m_flags |= Building.Flags.HighDensity;
                            break;
                    }
                }
                zoneManager.m_goodAreaFound[(int)zone] = (short)1024; // note that a good area was found and a building was spawned
            }
        }
    }
}
