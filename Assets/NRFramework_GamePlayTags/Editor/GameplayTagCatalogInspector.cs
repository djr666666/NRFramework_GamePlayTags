using UnityEditor;
using UnityEngine;

namespace NRFramework.GameplayTags
{
    [CustomEditor(typeof(GameplayTagCatalog))]
    sealed class GameplayTagCatalogInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var catalog = (GameplayTagCatalog)target;
            if (GUILayout.Button("打开标签编辑器", GUILayout.Height(24f)))
                GameplayTagEditorWindow.Open(catalog);

            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox("在标签图里改名字和注释。从父标签右边的圆点拖到子标签左边，就会连上。点保存配置后，代码里直接写 Tags.State.Control.Frozen。自己加的方法写在 Tags.User.cs，保存时不会覆盖。", MessageType.Info);
            EditorGUILayout.LabelField("已登记", catalog.ExplicitNames.Count.ToString());
            EditorGUILayout.LabelField("已写注释", catalog.NoteCount.ToString());
        }
    }
}
