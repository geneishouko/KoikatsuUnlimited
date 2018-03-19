using ExtensibleSaveFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KoikatsuUnlimited
{
    class Custom
    {
        private class UIItem
        {
            public string label;
            public string name;
            public string value;
            public Rect rect;
            public UIItem(string _label, string _name, Rect _rect)
            {
                label = _label;
                name = _name;
                value = "";
                rect = _rect;
            }
        }

        static readonly IList<UIItem> UIItems = new System.Collections.ObjectModel.ReadOnlyCollection<UIItem>
            (new[]
            {
                new UIItem("Right Eye", "eyeR", new Rect(20, 20, 120, 20)),
                new UIItem("Left Eye", "eyeL", new Rect(160, 20, 120, 20)),
            });

        internal static WeakReference CustomScene = new WeakReference(null);
        internal static WeakReference CustomCharacter = new WeakReference(null);
        internal static PluginData CustomOverrides = null;

        public static void SetOverrideDictionary(PluginData dict, bool face, bool body, bool hair, bool parameter, bool coordinate)
        {
            if (face)
            {
                string[] keys = new string[] { "eyeL", "eyeR" };
                object value;
                foreach (string k in keys)
                {
                    CustomOverrides.data[k] = dict.data.TryGetValue(k, out value) ? value as string : "";
                }
            }

            object v;
            for (int i = 0; i < UIItems.Count; i++)
            {
                UIItems[i].value = CustomOverrides.data.TryGetValue(UIItems[i].name, out v) ? v as string : "";
            }
        }

        private static void SetOverride(string name, string value)
        {
            if (value.IsNullOrEmpty())
                CustomOverrides.data.Remove(name);
            else
                CustomOverrides.data[name] = value;
        }

        internal static void DrawWindow()
        {
            {
                for (int i = 0; i < UIItems.Count; i++)
                {
                    UIItems[i].value = GUI.TextField(UIItems[i].rect, UIItems[i].value);
                }

                if (GUI.changed)
                {
                    for (int i = 0; i < UIItems.Count; i++)
                    {
                        SetOverride(UIItems[i].name, UIItems[i].value);
                    }
                }
            }
        }
    }
}
