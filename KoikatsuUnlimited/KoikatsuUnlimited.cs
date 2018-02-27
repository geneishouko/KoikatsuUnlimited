using Harmony;
using BepInEx;
using ChaCustom;
using KoikatsuUnlimited.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace KoikatsuUnlimited
{
    
    public class KoikatsuUnlimited : BaseUnityPlugin
    {
        public override string ID => "aa2g.kant.koikatsuunlimited";
        public override string Name => "Koikatsu Unlimited Startup Pack";
        public override Version Version => new Version("0.0");

        public static string OverridesDir => Path.Combine(BepInEx.Common.Utility.ExecutingDirectory, "UserData\\textures");

        private static WeakReference OverridingCharacterReference = new WeakReference(null);
        public static ChaFile OverridingCharacter
        {
            get { return OverridingCharacterReference.IsAlive ? OverridingCharacterReference.Target as ChaFile : null; }
            set { OverridingCharacterReference.Target = value; }
        }
        public static Dictionary<string, object> OverridingData = null;

        internal static WeakKeyDictionary<ChaFile, Dictionary<String, object>> CharactersData = new WeakKeyDictionary<ChaFile, Dictionary<String, object>>();

        #region MonoBehaviour
        void Awake()
        {
            InstallLoaderHooks();
        }

        void Start()
        {
            ExtensibleSaveFormat.ExtensibleSaveFormat.CardBeingSaved += OnCardSave;
            ExtensibleSaveFormat.ExtensibleSaveFormat.CardBeingLoaded += OnCardLoad;
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F4))
            {
                showingUI = !showingUI;
            }
        }
        #endregion

        #region UI
        private Rect UI = new Rect(20, 20, 400, 200);
        bool showingUI = false;

        void OnGUI()
        {
            if (showingUI)
                UI = GUI.Window(Name.GetHashCode() + 0, UI, WindowFunction, "Koikatsu Unlimited");
        }

        void WindowFunction(int windowID)
        {
            Custom.DrawWindow();
            GUI.DragWindow();
        }
        #endregion

        #region AssetLoader
        static void InstallLoaderHooks()
        {
            var harmony = HarmonyInstance.Create("aa2g.kant.koikatsuunlimited");

            MethodInfo original = AccessTools.Method(typeof(ChaControl), "ReloadAsync");
            var prefix = new HarmonyMethod(typeof(KoikatsuUnlimited).GetMethod("CharacterReloadPrefix"));
            var postfix = new HarmonyMethod(typeof(KoikatsuUnlimited).GetMethod("CharacterReloadPostfix"));
            harmony.Patch(original, prefix, /*postfix*/null);

            original = AccessTools.Method(typeof(ChaControl), "SetCreateTexture");
            prefix = new HarmonyMethod(typeof(KoikatsuUnlimited).GetMethod("SetCreateTexturePrefix"));
            harmony.Patch(original, prefix, /*postfix*/null);

            original = AccessTools.Method(typeof(CustomControl), "Entry");
            postfix  = new HarmonyMethod(typeof(KoikatsuUnlimited).GetMethod("CustomEntryPostfix"));
            harmony.Patch(original, null, postfix);

            original = AccessTools.Method(typeof(ChaFileControl), "LoadFileLimited", new[] { typeof(string), typeof(byte), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) });
            prefix = new HarmonyMethod(typeof(KoikatsuUnlimited).GetMethod("LoadFileLimitedHookPre"));
            postfix = new HarmonyMethod(typeof(KoikatsuUnlimited).GetMethod("LoadFileLimitedHookPost"));
            harmony.Patch(original, prefix, postfix);
        }

        public static void CustomEntryPostfix(ChaControl entryChara)
        {
            BepInEx.BepInLogger.Log($"Custom ChaFile is {entryChara.chaFile.GetHashCode()}", true);
            Custom.CustomCharacter.Target = entryChara.chaFile;
            Custom.CustomOverrides = CharactersData.Get(entryChara.chaFile);
            BepInEx.BepInLogger.Log($"CustomOverrides is {Custom.CustomOverrides.GetHashCode()}", true);
        }

        public static void CharacterReloadPrefix(ChaControl __instance)
        {
            OverridingCharacter = __instance.chaFile;
            OverridingData = CharactersData.Get(__instance.chaFile);
        }

        public static void CharacterReloadPostfix()
        {
            OverridingCharacter = null;
            //OverriddenCardChanged?.Invoke(OverridingCharacter); // Outside of Maker
        }

        static bool GetTextureOverride(string type, string name, out string path)
        {
            path = "";
            if (OverridingData == null)
                return false;
            //BepInEx.BepInLogger.Log($"GetTextureOverride ({type} : {OverridingData.GetHashCode()})", true);
            object tryPath;
            bool okGet = OverridingData.TryGetValue(name, out tryPath);
            if (okGet)
            {
                path = Path.Combine(OverridesDir, type);
                path = Path.Combine(path, tryPath as string);
            }
            
            return okGet;
        }

        public static bool SetCreateTexturePrefix(ChaControl __instance, CustomTextureCreate ctc, bool main, ChaListDefine.CategoryNo type, int id, ChaListDefine.KeyType assetBundleKey, ChaListDefine.KeyType assetKey, int propertyID)
        {
            // we don't support lowpoly
            if (!__instance.hiPoly)
                return true; // discard override and continue original method execution

            if (!__instance.chaFile.Equals(OverridingCharacter))
                CharacterReloadPrefix(__instance);

            bool okGet;

            // typeName is the texture type to override, typeKey is the key name to lookup in card extended data
            string typeName, typeKey;
            okGet = Constants.TextureTypes.TryGetValue(((int)type), out typeName);

            if (!okGet)
                return true;


            if (type == ChaListDefine.CategoryNo.mt_eye || type == ChaListDefine.CategoryNo.mt_eye_gradation)
            {
                typeKey = typeName + (ctc.Equals(__instance.ctCreateEyeL) ? "L" : "R");
            }
            else
            {
                typeKey = typeName;
            }

            string path;
            if (GetTextureOverride(typeName, typeKey, out path))
                try
                {
                    Texture2D texture2D = ResourceRedirector.AssetLoader.LoadTexture(path);
                    if (main)
                    {
                        ctc.SetMainTexture(texture2D);
                    }
                    else
                    {
                        ctc.SetTexture(propertyID, texture2D);
                    }
                    return false; // we have overridden the requested texture, don't execute original method
                }
                catch (System.Exception)
                { }
            //BepInEx.BepInLogger.Log($"NO Overriding type {typeName}/{ChaListDefine.GetCategoryName((int)type)}", true);
            // we failed to override this texture
            return true;
        }
        #endregion

        #region ExtensibleSaveFormat
        private static bool copySaveDataFromLoadedCard = false;

        void OnCardSave(ChaFile file)
        {
            BepInEx.BepInLogger.Log($"Saving data of ChaFile {file.GetHashCode()} {CharactersData.Get(file).GetHashCode()}", true);
            ExtensibleSaveFormat.ExtensibleSaveFormat.SetExtendedDataById(file, ID, CharactersData.Get(file));
        }

        private static Dictionary<string, object> LimitedLoadDictionary;
        void OnCardLoad(ChaFile file)
        {
            ChaFile targetFile = file;
            var data = ExtensibleSaveFormat.ExtensibleSaveFormat.GetExtendedDataById(file, ID);
            if (data == null)
            {
                data = new Dictionary<string, object>();
            }
            CharactersData.Set(file, data);
            BepInEx.BepInLogger.Log($"OnCardLoad {file.GetHashCode()} {data.GetHashCode()}", true);
            if (copySaveDataFromLoadedCard)
            {
                LimitedLoadDictionary = data;
            }
        }

        public static void LoadFileLimitedHookPre(ChaFileControl __instance, string filename, byte sex, bool face, bool body, bool hair, bool parameter, bool coordinate)
        {
            if (Custom.CustomCharacter.IsAlive && __instance.Equals(Custom.CustomCharacter.Target))
                copySaveDataFromLoadedCard = true;
        }

        public static void LoadFileLimitedHookPost(ChaFileControl __instance, string filename, byte sex, bool face, bool body, bool hair, bool parameter, bool coordinate)
        {
            if (copySaveDataFromLoadedCard)
                Custom.SetOverrideDictionary(LimitedLoadDictionary, face, body, hair, parameter, coordinate);
            copySaveDataFromLoadedCard = false;
        }
        #endregion
    }

}
