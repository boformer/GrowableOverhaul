using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GrowableOverhaul
{
    public class LargerResidential : ResidentialBuildingAI
    {

        public override void GetWidthRange(out int minWidth, out int maxWidth)
        {
            minWidth = 1;
            maxWidth = 16;
        }

        public override void GetLengthRange(out int minLength, out int maxLength)
        {
            minLength = 1;
            maxLength = 16;
        }
    }
}