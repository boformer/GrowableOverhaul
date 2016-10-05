using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GrowableOverhaul.Redirection;
using UnityEngine;

namespace GrowableOverhaul
{
    [TargetType(typeof(PrivateBuildingAI))]
    public class PrivateBuildingAIDetour
    {
        [RedirectMethod(true)] // Detour reason: Set maximum growable plot size to 8x8
        public static void GetWidthRange(PrivateBuildingAI _this, out int minWidth, out int maxWidth)
        {
            minWidth = 1;
            maxWidth = 8;
        }

        [RedirectMethod(true)] // Detour reason: Set maximum growable plot size to 8x8
        public static void GetLengthRange(PrivateBuildingAI _this, out int minLength, out int maxLength)
        {
            minLength = 1;
            maxLength = 8;
        }

        [RedirectMethod(true)] // Detour reason: Set decoration area to 8x8
        public static void GetDecorationArea(PrivateBuildingAI _this, out int width, out int length, out float offset)
        {
            width = ((_this.m_info.m_zoningMode != BuildingInfo.ZoningMode.Straight) ? _this.m_info.m_cellWidth : 8);
            length = ((_this.m_info.m_zoningMode != BuildingInfo.ZoningMode.Straight) ? _this.m_info.m_cellLength : 8);
            offset = (float)(length - _this.m_info.m_cellLength) * 4f;
            if (!_this.m_info.m_expandFrontYard)
            {
                offset = -offset;
            }
        }
    }
}
