using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GrowableOverhaul.Redirection;
using ICities;
using UnityEngine;

namespace GrowableOverhaul
{
    public class DetourLoadingExtension : ILoadingExtension
    {
        private readonly Dictionary<MethodInfo, RedirectCallsState> redirectsOnLoaded = new Dictionary<MethodInfo, RedirectCallsState>();
        private readonly Dictionary<MethodInfo, RedirectCallsState> redirectsOnCreated = new Dictionary<MethodInfo, RedirectCallsState>();

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
                    RedirectionHelper.RevertRedirect(kvp.Key, kvp.Value);
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
