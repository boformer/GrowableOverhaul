using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GrowableOverhaul
{
   public class ExtendedItemClass
    {
        //Adds new zones. Used in place of old ItemClass
        public enum Zone
        {
            Unzoned,
            Distant,

            ResidentialLow,           
            ResidentialMedium,
            ResidentialHigh,

            CommercialLow,
            CommercialHigh,

            IndustrialHigh,
            Industrial,

            Office,
            OfficeHigh,

            None = 15
        }
    }
}
