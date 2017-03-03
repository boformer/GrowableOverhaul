using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GrowableOverhaul.Redirection;
using UnityEngine;
using ColossalFramework.UI;
using System.Reflection;


namespace GrowableOverhaul
{
    //Reason for detour: To add additional zoning option buttons to panel. This is a lazy test. 
    [TargetType(typeof(ZoningPanel))]

    public class ZoningPanelDetour
    {
        public static Redirector OnButtonClickedRedirector = null;

        //When button is clicked, pass zoning tool new zone type 
        [RedirectMethod(true)]
        public static void OnButtonClicked(ZoningPanel _this, UIComponent comp)
        {

            ZoneTool zoneTool = ToolsModifierControl.SetTool<ZoneTool>();
            {
                if (comp.name == "Industrial") ZoneToolDetour.ExtendedZone = ExtendedItemClass.Zone.Industrial;
                else if (comp.name == "Office") ZoneToolDetour.ExtendedZone = ExtendedItemClass.Zone.OfficeHigh;
                else if (comp.name == "ResidentialLow") ZoneToolDetour.ExtendedZone = ExtendedItemClass.Zone.ResidentialLow;
                else if (comp.name == "ResidentialHigh") ZoneToolDetour.ExtendedZone = ExtendedItemClass.Zone.ResidentialMedium;
                else if (comp.name == "CommercialLow") ZoneToolDetour.ExtendedZone = ExtendedItemClass.Zone.ResidentialHigh;
                else if (comp.name == "CommercialHigh") ZoneToolDetour.ExtendedZone = ExtendedItemClass.Zone.CommercialHigh;
                else ZoneToolDetour.ExtendedZone = ExtendedItemClass.Zone.ResidentialLow;
            }

            OnButtonClickedRedirector.Revert();
            OnButtonClickedAlt(_this, comp);
            OnButtonClickedRedirector.Apply();


        }

        [RedirectReverse(true)]
        private static void OnButtonClickedAlt(ZoningPanel _this, UIComponent comp)
        {
            Debug.Log("Yay");
        }

    }
}