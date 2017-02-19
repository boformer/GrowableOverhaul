using System;
using System.IO;
using ColossalFramework.IO;
using ICities;
using UnityEngine;

namespace GrowableOverhaul
{
    /// <summary>
    /// Saves the extra zone type masks for deep zones, which can not be stored in the ZoneBlock struct.
    /// </summary>
    public class DataExtension : SerializableDataExtensionBase
    {
        private const string DataId = "GrowableOverhaul";
        private const uint DataVersion = 0;

        public static DataExtension instance;

        public static ulong[] zones3;
        public static ulong[] zones4;

        public override void OnCreated(ISerializableData serializableData)
        {
            base.OnCreated(serializableData);
            instance = this;
        }

        /// <summary>
        /// Instead of OnLoadData, we will use this custom hook.
        /// Called by TerrainManagerDetour#OnUpdateData 
        /// (zone data must be loaded before method executes)
        /// </summary>
        public void OnUpdateData()
        {
            zones3 = new ulong[ZoneManager.MAX_BLOCK_COUNT];
            zones4 = new ulong[ZoneManager.MAX_BLOCK_COUNT];

            var data = serializableDataManager.LoadData(DataId);

            if (data != null)
            {
                try
                {
                    using (var stream = new MemoryStream(data))
                    {
                        DataSerializer.Deserialize<Data>(stream, DataSerializer.Mode.Memory);
                    }

                    Debug.Log($"Growable Overhaul: Data loaded (Data length: {data.Length})");
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public override void OnSaveData()
        {
            base.OnSaveData();

            byte[] data;

            using (var stream = new MemoryStream())
            {
                DataSerializer.Serialize(stream, DataSerializer.Mode.Memory, DataVersion, new Data());
                data = stream.ToArray();
            }

            serializableDataManager.SaveData(DataId, data);

            Debug.Log($"Growable Overhaul: Data Saved (Data length: {data.Length})");
        }

        public override void OnReleased()
        {
            base.OnReleased();
            zones3 = null;
            zones4 = null;
            instance = null;
        }

        public class Data : IDataContainer
        {
            public void Serialize(DataSerializer s)
            {
                SerializeZoneMasks(s, zones3);
                SerializeZoneMasks(s, zones4);
            }

            public void Deserialize(DataSerializer s)
            {
                DeserializeZoneMasks(s, zones3);
                DeserializeZoneMasks(s, zones4);
            }

            public void AfterDeserialize(DataSerializer s)
            {
                CheckDataIntegrity(zones3, 5);
                CheckDataIntegrity(zones4, 7);
            }

            private static void SerializeZoneMasks(DataSerializer s, ulong[] array)
            {
                s.WriteInt32(array.Length);
                foreach (var zoneMask in array)
                {
                    s.WriteULong64(zoneMask);
                }
            }

            private static void DeserializeZoneMasks(DataSerializer s, ulong[] array)
            {
                var serializedLength = s.ReadInt32();
                for (var i = 0; i < serializedLength; i++)
                {
                    var zoneMask = s.ReadULong64();
                    if (i < array.Length)
                    {
                        array[i] = zoneMask;
                    }
                }
            }

            private static void CheckDataIntegrity(ulong[] zoneMask, int minDepth)
            {
                for (ushort blockID = 1; blockID < zoneMask.Length; blockID++)
                {
                    {
                        if (ZoneBlockDetour.GetColumnCount(ref ZoneManager.instance.m_blocks.m_buffer[blockID]) < minDepth 
                            && (zoneMask[blockID] != 0UL))
                        {
                            zoneMask[blockID] = 0UL;
                        }
                    }
                }
            }
        }

    }
}
