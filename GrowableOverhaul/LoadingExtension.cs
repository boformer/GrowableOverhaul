using System;
using System.Collections.Generic;
using System.Reflection;
using GrowableOverhaul.Redirection;
using ICities;
using UnityEngine;
using ColossalFramework.UI;
using ColossalFramework;

namespace GrowableOverhaul
{
    public class LoadingExtension : ILoadingExtension
    {
        private readonly Dictionary<MethodInfo, Redirector> redirectsOnLoaded = new Dictionary<MethodInfo, Redirector>();
        private readonly Dictionary<MethodInfo, Redirector> redirectsOnCreated = new Dictionary<MethodInfo, Redirector>();

        private bool created;
        private bool loaded;

        public void OnCreated(ILoading loading)
        {
            Debug.Log("GrowableOverhaul OnCreated!");

            if (created) return;

            // TODO detect conflicting mods (Building Themes, 81 tiles, etc.)

            Redirect(true);
            created = true;
        }

        public void OnLevelLoaded(LoadMode mode)
        {


            var uiView = UIView.GetAView();
            var refButton = new UIButton();

            refButton = uiView.FindUIComponent<UIButton>("ResidentialLow");

            //NewZoneColorManager.SetNewColors();
            //TerrainPatch.Refresh();

            //Shader testshaderl = new Shader();

            //ZoneProperties newprops = new ZoneProperties();

            //newprops.m_zoneColors = NewZoneColorManager.NewColors;

            //newprops.m_zoneShader = Singleton<ZoneManager>.instance.m_properties.m_zoneShader;

            //Singleton<ZoneManager>.instance.InitializeProperties(newprops);

            //ZoneManager.instance.m_zoneMaterial.SetColor(10, NewZoneColorManager.NewColors[9]);
            //ZoneManager.instance.m_zoneMaterial.SetColor(9, NewZoneColorManager.NewColors[9]);
            //ZoneManager.instance.m_zoneMaterial.SetColor(8, NewZoneColorManager.NewColors[9]);
            //ZoneManager.instance.m_zoneMaterial.SetColor("_ZoneColor8", NewZoneColorManager.NewColors[10]);
            //ZoneManager.instance.m_zoneMaterial.SetColor("_ZoneColor9", NewZoneColorManager.NewColors[10]);
            //ZoneManager.instance.m_zoneMaterial.SetColor("_ZoneColor10", NewZoneColorManager.NewColors[10]);

            var color2 = ZoneManager.instance.m_zoneMaterial.GetColor(3);

            Debug.Log("Zone color 3 is: " + color2);

            //var  array ZoneManager.instance.m_zoneMaterial.GetColor();
            //ZoneProperties.DestroyObject();


            Debug.Log("GrowableOverhaul OnLevelLoaded!");

            if (!created || loaded) return;

            Redirect(false);
            loaded = true;
        }

        public void OnLevelUnloading()
        {
            if (!created || !loaded) return;

            RevertRedirect(false);
            loaded = false;
        }

        public void OnReleased()
        {
            if (!created) return;

            if(loaded) OnLevelUnloading();
            RevertRedirect(false);
            created = false;
        }

        private void Redirect(bool onCreated)
        {
            var redirects = onCreated ? redirectsOnCreated : redirectsOnLoaded;
            redirects.Clear();

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                try
                {
                    var r = RedirectionUtil.RedirectType(type, onCreated);
                    if (r == null) continue;
                    foreach (var pair in r) redirects.Add(pair.Key, pair.Value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"An error occured while applying {type.Name} redirects!");
                    Debug.LogException(e);
                }
            }
        }

        private void RevertRedirect(bool onCreated)
        {
            var redirects = onCreated ? redirectsOnCreated : redirectsOnLoaded;
            foreach (var kvp in redirects)
            {
                try
                {
                    kvp.Value.Revert();
                }
                catch (Exception e)
                {
                    Debug.LogError($"An error occured while reverting {kvp.Key.Name} redirect!");
                    Debug.LogException(e);
                }
            }
            redirects.Clear();
        }
    }
}
