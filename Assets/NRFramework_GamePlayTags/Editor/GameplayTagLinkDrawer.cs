using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NRFramework.GameplayTags
{
    [CustomPropertyDrawer(typeof(GameplayTagLink))]
    sealed class GameplayTagLinkDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty nameProperty = property.FindPropertyRelative("_name");
            string current = nameProperty.stringValue;
            string shown = string.IsNullOrEmpty(current) ? "点这里选择" : current;
            if (!GUI.Button(position, new GUIContent(label.text + "：" + shown, "资源里存的是名字。播放时换成编号。"), EditorStyles.popup))
                return;

            GenericMenu menu = BuildMenu(nameProperty);
            menu.DropDown(position);
        }

        static GenericMenu BuildMenu(SerializedProperty nameProperty)
        {
            var menu = new GenericMenu();
            string current = nameProperty.stringValue;
            string propertyPath = nameProperty.propertyPath;
            Object target = nameProperty.serializedObject.targetObject;
            GameplayTagCatalog catalog = FindCatalog();
            if (catalog == null)
            {
                menu.AddDisabledItem(new GUIContent("还没有标签表"));
                return menu;
            }

            GameplayTagRegistry registry = catalog.CopyTable().CreateRegistry();
            var tags = new List<GameplayTag>();
            registry.CopyTags(tags);
            if (tags.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("标签表是空的"));
                return menu;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                string tagName = tags[i].Name;
                menu.AddItem(new GUIContent(tagName.Replace('.', '/')), tagName == current, Pick, new PickArgs
                {
                    Target = target,
                    PropertyPath = propertyPath,
                    TagName = tagName,
                });
            }
            return menu;
        }

        static void Pick(object userData)
        {
            var args = (PickArgs)userData;
            var serialized = new SerializedObject(args.Target);
            SerializedProperty nameProperty = serialized.FindProperty(args.PropertyPath);
            nameProperty.stringValue = args.TagName;
            serialized.ApplyModifiedProperties();
        }

        static GameplayTagCatalog FindCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameplayTagCatalog");
            if (guids == null || guids.Length == 0)
                return null;
            string fallback = null;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.StartsWith("Packages/"))
                {
                    fallback = path;
                    continue;
                }
                return AssetDatabase.LoadAssetAtPath<GameplayTagCatalog>(path);
            }
            if (fallback == null)
                return null;
            return AssetDatabase.LoadAssetAtPath<GameplayTagCatalog>(fallback);
        }

        sealed class PickArgs
        {
            public Object Target;
            public string PropertyPath;
            public string TagName;
        }
    }
}
