using ColossalFramework.Math;
using GrowableOverhaul.Redirection;
using UnityEngine;

namespace GrowableOverhaul
{
    /// <summary>
    /// These detours are required to make variable zone depth work.
    /// When an existing segment is splitted into 2 new segments 
    /// or otherwise replaced by a new segment, the zone depth value must be transferred
    /// </summary>
    [TargetType(typeof(NetManager))]
    public static class NetManagerDetour
    {
        public static Redirector CreateSegmentRedirector;
        public static Redirector ReleaseSegmentRedirector;

        // The ZoneManagerDetour.CreateBlock uses this value
        public static int newBlockColumnCount;

        // reset both on CreateSegment call by CreateNode
        private static int SplitSegment_releasedColumnCount;
        private static int MoveMiddleNode_releasedColumnCount;

        [RedirectMethod(false)]
        public static bool CreateSegment(NetManager _this, out ushort segmentID, ref Randomizer randomizer, NetInfo info, ushort startNode, ushort endNode, Vector3 startDirection, Vector3 endDirection, uint buildIndex, uint modifiedIndex, bool invert)
        {
            var ai = info.m_netAI as RoadAI;
            if (ai != null && ai.m_enableZoning)
            {
                var caller = new System.Diagnostics.StackFrame(1).GetMethod().Name;

                switch (caller)
                {
                    case "MoveMiddleNode": // segment that was modified because user added network, apply style of previous segment
                        newBlockColumnCount = MoveMiddleNode_releasedColumnCount >= 0 ?
                            MoveMiddleNode_releasedColumnCount : InputThreadingExtension.userSelectedColumnCount;
                        break;

                    case "SplitSegment": // segment that was split by new node, apply style of previous segment
                        newBlockColumnCount = SplitSegment_releasedColumnCount >= 0 ?
                            SplitSegment_releasedColumnCount : InputThreadingExtension.userSelectedColumnCount;
                        break;

                    default: // unknown caller (e.g. new road placed), set to depth selected by user
                        newBlockColumnCount = InputThreadingExtension.userSelectedColumnCount;
                        SplitSegment_releasedColumnCount = -1;
                        MoveMiddleNode_releasedColumnCount = -1;
                        break;
                }
            }

            // Call original method
            CreateSegmentRedirector.Revert();
            var success = _this.CreateSegment(out segmentID, ref randomizer, info, startNode, endNode, startDirection, endDirection, buildIndex, modifiedIndex, invert);
            CreateSegmentRedirector.Apply();

            return success;
        }

        [RedirectMethod(false)]
        public static void ReleaseSegment(NetManager _this, ushort segmentID, bool keepNodes)
        {
            var segment = _this.m_segments.m_buffer[segmentID];

            int columnCount = 0;
            FindColumnCount(segment.m_blockEndLeft, ref columnCount);
            FindColumnCount(segment.m_blockEndRight, ref columnCount);
            FindColumnCount(segment.m_blockStartLeft, ref columnCount);
            FindColumnCount(segment.m_blockStartRight, ref columnCount);

            var caller = new System.Diagnostics.StackFrame(1).GetMethod().Name;

            //Debug.Log($"ReleaseSegment called by {caller}, id: {segmentID}, type: {segment.Info.name}");

            switch (caller)
            {
                case "MoveMiddleNode": // segment that was modified because user added network, keep data until replacement segments were created

                    // Save segment id
                    MoveMiddleNode_releasedColumnCount = columnCount;
                    break;

                case "SplitSegment": // segment that was split by new node, keep data until replacement segments were created

                    // Save segment id
                    SplitSegment_releasedColumnCount = columnCount;
                    break;

                default: // unknown caller
                    break;
            }

            // Call original method
            ReleaseSegmentRedirector.Revert();
            _this.ReleaseSegment(segmentID, keepNodes);
            ReleaseSegmentRedirector.Apply();
        }

        public static void FindColumnCount(ushort blockID, ref int columnCount)
        {
            if (blockID > 0)
            {
                columnCount = Mathf.Max(columnCount, ZoneBlockDetour.GetColumnCount(ref ZoneManager.instance.m_blocks.m_buffer[blockID]));
            }
        }
    }
}
