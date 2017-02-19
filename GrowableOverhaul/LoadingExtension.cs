using System;
using System.Collections.Generic;
using System.Reflection;
using GrowableOverhaul.Redirection.Extensions;
using GrowableOverhaul.Redirection;
using GrowableOverhaul.Detours;
using ICities;
using UnityEngine;

namespace GrowableOverhaul
{
    public class LoadingExtension : LoadingExtensionBase
    {


        private PrefabManager PrefabManager;


        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);

            if (loading.currentMode == AppMode.Game)
            {
               
            }
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            PrefabManager = new PrefabManager();
            PrefabManager.ConvertPrefab();

            Redirector<BuildingDetour>.Deploy();
            Redirector<BuildingToolDetour>.Deploy();
            Redirector<NetManagerDetour>.Deploy();
            Redirector<RoadAIDetour>.Deploy();
            Redirector<TerrainManagerDetour>.Deploy();
            Redirector<ZoneBlockDetour>.Deploy();
            Redirector<ZoneManagerDetour>.Deploy();
            Redirector<ZoneToolDetour>.Deploy();
        }

        public override void OnLevelUnloading()
        {
           
        }

        public override void OnReleased()
        {
            base.OnReleased();

            Redirector<BuildingDetour>.Revert();
            Redirector<BuildingToolDetour>.Revert();
            Redirector<NetManagerDetour>.Revert();
            Redirector<RoadAIDetour>.Revert();
            Redirector<TerrainManagerDetour>.Revert();
            Redirector<ZoneBlockDetour>.Revert();
            Redirector<ZoneManagerDetour>.Revert();
            Redirector<ZoneToolDetour>.Revert();

        }

    }
}
