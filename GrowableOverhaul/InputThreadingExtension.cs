using ICities;
using UnityEngine;

namespace GrowableOverhaul
{
    /// <summary>
    /// Watches ALT + UP/DOWN key combination to change the zone depth for new roads.
    /// </summary>
    public class InputThreadingExtension : ThreadingExtensionBase
    {
        // zone depth for new zone blocks (selected by user)
        public static int userSelectedColumnCount = 4;

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    userSelectedColumnCount = Mathf.Clamp(userSelectedColumnCount + 1, 0, 8);
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    userSelectedColumnCount = Mathf.Clamp(userSelectedColumnCount - 1, 0, 8);
                }
            }
        }
    }
}
