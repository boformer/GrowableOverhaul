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
    //Reason for detour: To add additional zoning option buttons to panel. 
    [TargetType(typeof(ZoningPanel))]

    public class ZoningPanelDetour : GeneratedScrollPanel
    {

      public override ItemClass.Service service
        {
            get
            {
                return ItemClass.Service.None;
            }
        }

        /*
        [RedirectMethod(true)]
        public void RefreshPanel(ZoningPanel _this)
        {
            _this.CleanPanel();
            _this.m_ObjectIndex = 0;


            //pull from new list of zones
            for (int i = 0; i < 10; i++)
            {
                base.SpawnEntry(ExtendedItemClass.Zone.ResidentialLow, i);
               
                //GeneratedScrollPanel.spawne
            }

        }
        */

        //When button is clicked, pass zoning tool new zone type 
        [RedirectMethod(true)]
        public static void OnButtonClicked(ZoningPanel _this, UIComponent comp) {

            ZoneTool zoneTool = ToolsModifierControl.SetTool<ZoneTool>();
            {
                //GeneratedScrollPanel.ShowZoningOptionPanel();

                //pass tone tool new zone types
                zoneTool.m_zone = ItemClass.Zone.ResidentialLow;
                //zoneTool.m_zone = ZoningPanel.kZones[comp.zOrder].enumValue;

                ZoneToolDetour.ExtendedZone = ExtendedItemClass.Zone.ResidentialLow;
            }
        }

    }

}
