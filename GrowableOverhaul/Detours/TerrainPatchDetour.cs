using GrowableOverhaul.Redirection;
using ColossalFramework;
using System;
using System.Threading;
using UnityEngine;

namespace GrowableOverhaul
{
    [TargetType(typeof(TerrainPatch))]
    public static class TerrainPatchDetour
    {

        [RedirectMethod(true)]
        public static void SetTerrainMaterialProperties(TerrainPatch _this, Material material)
        {
            TerrainManager instance = Singleton<TerrainManager>.instance;
            material.SetTexture(instance.ID_MainTex, _this.m_heightMap);
            material.SetTexture(instance.ID_SurfaceTexA, _this.m_surfaceMapA);
            material.SetTexture(instance.ID_SurfaceTexB, _this.m_surfaceMapB);

            //_this.m_zoneLayout = new Texture2D(512, 512, TextureFormat.ARGB32, false, true);
            //_this.m_zoneLayout.wrapMode = TextureWrapMode.Clamp;
            //_this.m_zoneLayout.filterMode = FilterMode.Point;

            //material.SetTexture(instance.ID_ZoneLayout, _this.m_zoneLayout);
            material.SetTexture(instance.ID_ZoneLayout, _this.m_zoneLayout);

            material.SetVector(instance.ID_HeightMapping, (_this.m_rndDetailIndex == 0) ? _this.m_heightMappingRaw : _this.m_heightMappingDetail);
            material.SetVector(instance.ID_SurfaceMapping, (_this.m_rndDetailIndex == 0) ? _this.m_surfaceMappingRaw : _this.m_surfaceMappingDetail);


        }

        [RedirectMethod(true)]
        public static void Refresh(TerrainPatch _this, bool updateWater, uint waterFrame)
        {
            TerrainManager instance = Singleton<TerrainManager>.instance;
            WaterSimulation waterSimulation = instance.WaterSimulation;
            ushort[] finalHeights = instance.FinalHeights;
            ushort[] blockHeightTargets = instance.BlockHeightTargets;
            TerrainManager.SurfaceCell[] rawSurface = instance.RawSurface;
            int num = 120;
            float num2 = 17280f;
            float num3 = (float)num * 16f;
            int num4 = num * _this.m_x;
            int num5 = num * (_this.m_x + 1);
            int num6 = num * _this.m_z;
            int num7 = num * (_this.m_z + 1);
            TerrainManager.CellBounds bounds = instance.GetBounds(_this.m_x, _this.m_z, 0);
            _this.m_terrainMinY = (float)bounds.m_minHeight * 0.015625f;
            _this.m_terrainMaxY = (float)bounds.m_maxHeight * 0.015625f;
            _this.m_terrainPosition.x = ((float)_this.m_x + 0.5f) * num3 - num2 * 0.5f;
            _this.m_terrainPosition.y = (_this.m_terrainMinY + _this.m_terrainMaxY) * 0.5f;
            _this.m_terrainPosition.z = ((float)_this.m_z + 0.5f) * num3 - num2 * 0.5f;
            int num8 = _this.m_rndDetailIndex;
            while (!Monitor.TryEnter(_this.m_modifiedLock, SimulationManager.SYNCHRONIZE_TIMEOUT))
            {
            }
            try
            {
                TerrainArea terrainArea = instance.m_tmpSurfaceModified;
                instance.m_tmpSurfaceModified = _this.m_surfaceModified;
                terrainArea.m_x = instance.m_tmpSurfaceModified.m_x;
                terrainArea.m_z = instance.m_tmpSurfaceModified.m_z;
                terrainArea.Reset();
                _this.m_surfaceModified = terrainArea;
                terrainArea = instance.m_tmpZonesModified;
                instance.m_tmpZonesModified = _this.m_zonesModified;
                terrainArea.m_x = instance.m_tmpZonesModified.m_x;
                terrainArea.m_z = instance.m_tmpZonesModified.m_z;
                terrainArea.Reset();
                _this.m_zonesModified = terrainArea;
                num8 = _this.m_tmpDetailIndex;
            }
            finally
            {
                Monitor.Exit(_this.m_modifiedLock);
            }
            if (num8 != _this.m_rndDetailIndex)
            {
                if (num8 != 0 != (_this.m_rndDetailIndex != 0))
                {
                    _this.ResizeControlTextures(num8 != 0);
                }
                _this.m_rndDetailIndex = num8;
            }
            int num9 = 128 - num >> 1;
            if (instance.m_tmpSurfaceModified.m_minX - num9 <= num5 && instance.m_tmpSurfaceModified.m_maxX >= num4 - num9 && instance.m_tmpSurfaceModified.m_minZ - num9 <= num7 && instance.m_tmpSurfaceModified.m_maxZ >= num6 - num9)
            {
                if (_this.m_rndDetailIndex != 0)
                {
                    num9 = 16;
                    int num10 = 256 - 480 * _this.m_x - 240;
                    int num11 = 256 - 480 * _this.m_z - 240;
                    int b = Mathf.Max((num4 << 2) - num9, instance.m_tmpSurfaceModified.m_minX << 2);
                    int b2 = Mathf.Min((num5 << 2) + num9 - 1, instance.m_tmpSurfaceModified.m_maxX << 2);
                    int num12 = Mathf.Max((num6 << 2) - num9, instance.m_tmpSurfaceModified.m_minZ << 2);
                    int num13 = Mathf.Min((num7 << 2) + num9 - 1, instance.m_tmpSurfaceModified.m_maxZ << 2);
                    float num14 = 0.001953125f;
                    for (int i = num12; i <= num13; i++)
                    {
                        int num15;
                        int num16;
                        instance.m_tmpSurfaceModified.GetLimitsX(i >> 2, out num15, out num16);
                        num15 = Mathf.Max(num15 << 2, b);
                        num16 = Mathf.Min(num16 << 2, b2);
                        int x = Mathf.Max(0, num15);
                        int z = Mathf.Max(0, i);
                        int z2 = Mathf.Min(4320, i + 1);
                        float num17 = instance.GetDetailHeight(x, z);
                        float num18 = instance.GetDetailHeight(x, z2);
                        float num19 = instance.GetBlockHeight(x, z);
                        for (int j = num15; j <= num16; j++)
                        {
                            int x2 = Mathf.Min(4320, j + 1);
                            float detailHeight = instance.GetDetailHeight(x2, z);
                            float detailHeight2 = instance.GetDetailHeight(x2, z2);
                            float blockHeight = instance.GetBlockHeight(x2, z);
                            int num20;
                            int num21;
                            instance.m_tmpSurfaceModified.GetLimitsZ(j >> 2, out num20, out num21);
                            if (i >= num20 << 2 && i <= num21 << 2)
                            {
                                int num22 = Mathf.RoundToInt(num17 * 256f);
                                int num23 = Mathf.RoundToInt(num19 * 256f);
                                Vector3 vector = new Vector3((num17 + num18 - detailHeight - detailHeight2) * num14, 1f, (num17 + detailHeight - num18 - detailHeight2) * num14);
                                vector.Normalize();
                                TerrainManager.SurfaceCell surfaceCell = instance.GetSurfaceCell(Mathf.Clamp(j, 0, 4319), Mathf.Clamp(i, 0, 4319));
                                Color color;
                                color.r = (float)(num22 >> 16) * 0.003921569f;
                                color.g = (float)(num22 >> 8 & 255) * 0.003921569f;
                                color.b = (float)(num23 >> 16) * 0.003921569f;
                                color.a = (float)(num23 >> 8 & 255) * 0.003921569f;
                                Color color2;
                                color2.r = (float)surfaceCell.m_clipped * 0.003921569f;
                                color2.g = (float)surfaceCell.m_pavementA * 0.003921569f;
                                color2.b = vector.x * 0.5f + 0.5f;
                                color2.a = vector.z * 0.5f + 0.5f;
                                Color color3;
                                color3.r = (float)surfaceCell.m_ruined * 0.003921569f;
                                color3.g = (float)surfaceCell.m_pavementB * 0.003921569f;
                                color3.b = (float)surfaceCell.m_gravel * 0.003921569f;
                                color3.a = (float)surfaceCell.m_field * 0.003921569f;
                                _this.m_heightMap.SetPixel(j + num10, i + num11, color);
                                _this.m_surfaceMapA.SetPixel(j + num10, i + num11, color2);
                                _this.m_surfaceMapB.SetPixel(j + num10, i + num11, color3);
                            }
                            num17 = detailHeight;
                            num18 = detailHeight2;
                            num19 = blockHeight;
                        }
                    }
                }
                else
                {
                    int num24 = 64 - num * _this.m_x - (num >> 1);
                    int num25 = 64 - num * _this.m_z - (num >> 1);
                    int b3 = Mathf.Max(num4 - num9, instance.m_tmpSurfaceModified.m_minX);
                    int b4 = Mathf.Min(num5 + num9 - 1, instance.m_tmpSurfaceModified.m_maxX);
                    int num26 = Mathf.Max(num6 - num9, instance.m_tmpSurfaceModified.m_minZ);
                    int num27 = Mathf.Min(num7 + num9 - 1, instance.m_tmpSurfaceModified.m_maxZ);
                    float num28 = 0.00048828125f;
                    for (int k = num26; k <= num27; k++)
                    {
                        int num29;
                        int num30;
                        instance.m_tmpSurfaceModified.GetLimitsX(k, out num29, out num30);
                        num29 = Mathf.Max(num29, b3);
                        num30 = Mathf.Min(num30, b4);
                        for (int l = num29; l <= num30; l++)
                        {
                            int num31;
                            int num32;
                            instance.m_tmpSurfaceModified.GetLimitsZ(l, out num31, out num32);
                            if (k >= num31 && k <= num32)
                            {
                                int num33 = Mathf.Max(0, l);
                                int num34 = Mathf.Min(1080, l + 1);
                                int num35 = Mathf.Max(0, k);
                                int num36 = Mathf.Min(1080, k + 1);
                                int num37 = (int)finalHeights[num35 * 1081 + num33];
                                int num38 = (int)finalHeights[num35 * 1081 + num34];
                                int num39 = (int)finalHeights[num36 * 1081 + num33];
                                int num40 = (int)finalHeights[num36 * 1081 + num34];
                                int num41 = (int)blockHeightTargets[num35 * 1081 + num33];
                                int num42 = num37 << 8;
                                int num43 = num41 << 8;
                                Vector3 vector2 = new Vector3((float)(num37 + num39 - num38 - num40) * num28, 1f, (float)(num37 + num38 - num39 - num40) * num28);
                                vector2.Normalize();
                                TerrainManager.SurfaceCell surfaceCell2 = rawSurface[Mathf.Clamp(k, 0, 1079) * 1080 + Mathf.Clamp(l, 0, 1079)];
                                Color color4;
                                color4.r = (float)(num42 >> 16) * 0.003921569f;
                                color4.g = (float)(num42 >> 8 & 255) * 0.003921569f;
                                color4.b = (float)(num43 >> 16) * 0.003921569f;
                                color4.a = (float)(num43 >> 8 & 255) * 0.003921569f;
                                Color color5;
                                color5.r = (float)surfaceCell2.m_clipped * 0.003921569f;
                                color5.g = (float)surfaceCell2.m_pavementA * 0.003921569f;
                                color5.b = vector2.x * 0.5f + 0.5f;
                                color5.a = vector2.z * 0.5f + 0.5f;
                                Color color6;
                                color6.r = (float)surfaceCell2.m_ruined * 0.003921569f;
                                color6.g = (float)surfaceCell2.m_pavementB * 0.003921569f;
                                color6.b = (float)surfaceCell2.m_gravel * 0.003921569f;
                                color6.a = (float)surfaceCell2.m_field * 0.003921569f;
                                _this.m_heightMap.SetPixel(l + num24, k + num25, color4);
                                _this.m_surfaceMapA.SetPixel(l + num24, k + num25, color5);
                                _this.m_surfaceMapB.SetPixel(l + num24, k + num25, color6);
                            }
                        }
                    }
                }
                _this.m_heightMap.Apply(false);
                _this.m_surfaceMapA.Apply(false);
                _this.m_surfaceMapB.Apply(false);
            }
            num9 = 128 - num >> 1;
            if (instance.m_tmpZonesModified.m_minX - num9 <= num5 && instance.m_tmpZonesModified.m_maxX >= num4 - num9 && instance.m_tmpZonesModified.m_minZ - num9 <= num7 && instance.m_tmpZonesModified.m_maxZ >= num6 - num9 && _this.m_rndDetailIndex != 0)
            {
                num9 = 16;
                int num44 = 256 - 480 * _this.m_x - 240;
                int num45 = 256 - 480 * _this.m_z - 240;
                int b5 = Mathf.Max((num4 << 2) - num9, instance.m_tmpZonesModified.m_minX << 2);
                int b6 = Mathf.Min((num5 << 2) + num9 - 1, instance.m_tmpZonesModified.m_maxX << 2);
                int num46 = Mathf.Max((num6 << 2) - num9, instance.m_tmpZonesModified.m_minZ << 2);
                int num47 = Mathf.Min((num7 << 2) + num9 - 1, instance.m_tmpZonesModified.m_maxZ << 2);
                for (int m = num46; m <= num47; m++)
                {
                    int z3 = Mathf.Max(0, m);
                    int num48;
                    int num49;
                    instance.m_tmpZonesModified.GetLimitsX(m >> 2, out num48, out num49);
                    num48 = Mathf.Max(num48 << 2, b5);
                    num49 = Mathf.Min(num49 << 2, b6);

                    Color NewColor = new Color { r = .5f, g = .5f, b = 0f, a = .1f };


                    for (int n = num48; n <= num49; n++)
                    {
                        int num50;
                        int num51;
                        instance.m_tmpZonesModified.GetLimitsZ(n >> 2, out num50, out num51);
                        if (m >= num50 << 2 && m <= num51 << 2)
                        {
                            int x3 = Mathf.Max(0, n);
                            TerrainManager.ZoneCell zoneCell = instance.GetZoneCell(x3, z3);
                            Color color7;
                            color7.r = (float)zoneCell.m_offsetX * 0.003921569f + 0.5019608f;
                            color7.g = (float)zoneCell.m_angle * 0.00390625f;
                            color7.b = (float)zoneCell.m_offsetZ * 0.003921569f + 0.5019608f;
                            //color7.a = (float)4 * 0.0322580636f;

                            color7.a = (float)zoneCell.m_zone * 0.0322580636f;
                            //Debug.Log((float)zoneCell.m_zone);
                            //color7.a = (float)(byte)11 * 0.0322580636f;



                            _this.m_zoneLayout.SetPixel(n + num44, m + num45, color7);
                            //Debug.Log( "x is: " + (n + num44) + "y is: " + (m + num45) + "color is: " + NewColor);
                        }
                    }
                }
                _this.m_zoneLayout.Apply(false);
            }
            bool flag = waterSimulation.WaterExists(num4, num6, num5, num7);
            if ((flag || _this.m_waterExists != 0 || !_this.m_waterInitialized) && (updateWater || !_this.m_waterInitialized))
            {
                if (!_this.m_waterInitialized)
                {
                    for (uint num52 = 0u; num52 < 2u; num52 += 1u)
                    {
                        _this.m_waterHeight[(int)((UIntPtr)num52)].m_waterHeight.LoadRawTextureData(waterSimulation.m_heightMaps[_this.m_z * 9 + _this.m_x]);
                        _this.m_waterHeight[(int)((UIntPtr)num52)].m_waterHeight.Apply(false);
                        Vector4 vector3 = waterSimulation.m_heightBounds[_this.m_z * 9 + _this.m_x];
                        float num53 = Mathf.Min(vector3.x, vector3.z);
                        float num54 = Mathf.Max(vector3.y, vector3.w);
                        Vector3 terrainPosition = _this.m_terrainPosition;
                        terrainPosition.y = (num53 + num54) * 0.5f;
                        _this.m_waterHeight[(int)((UIntPtr)num52)].m_waterPosition = terrainPosition;
                        _this.m_waterHeight[(int)((UIntPtr)num52)].m_waterMinY = num53;
                        _this.m_waterHeight[(int)((UIntPtr)num52)].m_waterMaxY = num54;
                    }
                    for (uint num55 = 0u; num55 < 3u; num55 += 1u)
                    {
                        _this.m_waterSurfaceA[(int)((UIntPtr)num55)].LoadRawTextureData(waterSimulation.m_surfaceMapsA[_this.m_z * 9 + _this.m_x]);
                        _this.m_waterSurfaceA[(int)((UIntPtr)num55)].Apply(false);
                        _this.m_waterSurfaceB[(int)((UIntPtr)num55)].LoadRawTextureData(waterSimulation.m_surfaceMapsB[_this.m_z * 9 + _this.m_x]);
                        _this.m_waterSurfaceB[(int)((UIntPtr)num55)].Apply(false);
                    }
                }
                else
                {
                    uint num56 = waterFrame >> 6 & 1u;
                    uint num57 = (waterFrame >> 6) % 3u;
                    _this.m_waterHeight[(int)((UIntPtr)num56)].m_waterHeight.LoadRawTextureData(waterSimulation.m_heightMaps[_this.m_z * 9 + _this.m_x]);
                    _this.m_waterHeight[(int)((UIntPtr)num56)].m_waterHeight.Apply(false);
                    Vector4 vector4 = waterSimulation.m_heightBounds[_this.m_z * 9 + _this.m_x];
                    float num58 = Mathf.Min(vector4.x, vector4.z);
                    float num59 = Mathf.Max(vector4.y, vector4.w);
                    Vector3 terrainPosition2 = _this.m_terrainPosition;
                    terrainPosition2.y = (num58 + num59) * 0.5f;
                    _this.m_waterHeight[(int)((UIntPtr)num56)].m_waterPosition = terrainPosition2;
                    _this.m_waterHeight[(int)((UIntPtr)num56)].m_waterMinY = num58;
                    _this.m_waterHeight[(int)((UIntPtr)num56)].m_waterMaxY = num59;
                    _this.m_waterSurfaceA[(int)((UIntPtr)num57)].LoadRawTextureData(waterSimulation.m_surfaceMapsA[_this.m_z * 9 + _this.m_x]);
                    _this.m_waterSurfaceA[(int)((UIntPtr)num57)].Apply(false);
                    _this.m_waterSurfaceB[(int)((UIntPtr)num57)].LoadRawTextureData(waterSimulation.m_surfaceMapsB[_this.m_z * 9 + _this.m_x]);
                    _this.m_waterSurfaceB[(int)((UIntPtr)num57)].Apply(false);
                }
                _this.m_waterInitialized = true;
                if (flag)
                {
                    _this.m_waterExists = 3;
                }
                else
                {
                    _this.m_waterExists = Mathf.Max(0, _this.m_waterExists - 1);
                }
            }
            uint num60 = Singleton<SimulationManager>.instance.m_referenceFrameIndex + 32u;
            uint num61 = num60 - 64u >> 6 & 1u;
            float num62 = Mathf.Min(_this.m_terrainMinY, _this.m_waterHeight[(int)((UIntPtr)num61)].m_waterMinY);
            float num63 = Mathf.Max(_this.m_terrainMaxY, _this.m_waterHeight[(int)((UIntPtr)num61)].m_waterMaxY);
            Vector3 center = new Vector3(_this.m_terrainPosition.x, (num62 + num63) * 0.5f, _this.m_terrainPosition.z);
            Vector3 size = new Vector3(num3, num63 - num62 + 100f, num3);
            _this.m_bounds = new Bounds(center, size);
        }

    }
}
