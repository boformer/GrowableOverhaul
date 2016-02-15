using ICities;

namespace GrowableOverhaul
{
    // TODO serialization
    public class DataExtension : SerializableDataExtensionBase
    {
        public static ulong[] zones3;
        public static ulong[] zones4;

        public override void OnLoadData()
        {
            base.OnLoadData();
            zones3 = new ulong[ZoneManager.MAX_BLOCK_COUNT];
            zones4 = new ulong[ZoneManager.MAX_BLOCK_COUNT];
        }

        public override void OnSaveData()
        {
            base.OnSaveData();
        }
    }
}
