using ICities;

namespace GrowableOverhaul
{
    public class GrowableOverhaulMod : IUserMod
    {
        // zone depth for new zone blocks
        public static int newBlockColumnCount = 6;

        public string Name => "Growable Overhaul";
        public string Description => "Larger zones, Larger growables, Better spawning algoritm";
    }
}
