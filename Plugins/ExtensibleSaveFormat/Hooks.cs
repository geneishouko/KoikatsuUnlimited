using Harmony;
using MessagePack;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ExtensibleSaveFormat
{
    public static class Hooks
    {
        public static void InstallHooks()
        {
            var harmony = HarmonyInstance.Create("com.bepis.bepinex.extensiblesaveformat");


            MethodInfo original = AccessTools.Method(typeof(ChaFile), "SaveFile", new[] { typeof(BinaryWriter), typeof(bool) });
            HarmonyMethod postfix = new HarmonyMethod(typeof(Hooks).GetMethod("SaveFileHook"));
            harmony.Patch(original, null, postfix);


            original = AccessTools.Method(typeof(ChaFile), "LoadFile", new[] { typeof(BinaryReader), typeof(bool), typeof(bool) });
            postfix = new HarmonyMethod(typeof(Hooks).GetMethod("LoadFileHook"));
            harmony.Patch(original, null, postfix);

        }

        public static void ExamineDictionary(Dictionary<string, object> dict)
        {
            try
            {
                BepInEx.BepInLogger.Log($"ExamineDictionary Keys: {dict.Keys.Count}");
                foreach (KeyValuePair<string, object> kv in dict)
                {
                    BepInEx.BepInLogger.Log($"Got Section called ({kv.Key})");
                    Dictionary<string, object> subdict = kv.Value as Dictionary<string, object>;
                    BepInEx.BepInLogger.Log($"subdict is null? ({subdict == null})");
                    BepInEx.BepInLogger.Log($"Got Section called ({kv.Key}) with ({(kv.Value as Dictionary<string, object>).Values.Count}) keys");
                    foreach (KeyValuePair<string, object> rules in subdict)
                    {
                        BepInEx.BepInLogger.Log($"Got keypair ({rules.Key}) ({rules.Value as string})");
                    }
                }
            }
            catch (System.Exception e)
            {
                BepInEx.BepInLogger.Log($"ExamineDictionary Exception {e.ToString()}");
            }
        }

        public static void SaveFileHook(ChaFile __instance, bool __result, BinaryWriter bw, bool savePng)
        {
            if (!__result)
                return;

            ExtensibleSaveFormat.writeEvent(__instance);

            Dictionary<string, object> extendedData = ExtensibleSaveFormat.GetAllExtendedData(__instance);
            if (extendedData == null )
                return;
            ExamineDictionary(extendedData);
            byte[] bytes = MessagePackSerializer.Serialize(extendedData);

            BepInEx.BepInLogger.Log($"Extended Data Keys: {extendedData.Keys.ToString()}");
            foreach (KeyValuePair<string,object> kv in extendedData)
            {
                Dictionary<string, object> dict = kv.Value as Dictionary<string, object>;
                BepInEx.BepInLogger.Log($"Extended Data: {kv.Key}: {dict.ToString()}");
            }

            bw.Write((int)bytes.Length);
            bw.Write(bytes);
            BepInEx.BepInLogger.Log($"Wrote {bytes.Length} bytes of extended data", true);
        }

        public static void LoadFileHook(ChaFile __instance, bool __result, BinaryReader br, bool noLoadPNG, bool noLoadStatus)
        {
            Dictionary<string, object> dictionary = null;

            if (!__result)
                return;

            try
            {
                int length = br.ReadInt32();

                if (length > 0)
                {
                    byte[] bytes = br.ReadBytes(length);
                    dictionary = MessagePackSerializer.Deserialize<Dictionary<string, object>>(bytes);
                }
            }
            catch (EndOfStreamException) { }

            if (dictionary == null)
            {
                //initialize a new dictionary since it doesn't exist
                dictionary = new Dictionary<string, object>();
            }

            ExamineDictionary(dictionary);

            ExtensibleSaveFormat.internalDictionary.Set(__instance, dictionary);
            ExtensibleSaveFormat.readEvent(__instance);
        }

    }
}
