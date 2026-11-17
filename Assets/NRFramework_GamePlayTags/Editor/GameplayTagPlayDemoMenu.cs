using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NRFramework.GameplayTags
{
    static class GameplayTagPlayDemoMenu
    {
        const string ObjectName = "GameplayTag按Q演示";

        [MenuItem("NRFramework/Gameplay Tags/场景里放按 Q 演示", false, 10)]
        static void Create()
        {
            GameplayTagCatalog catalog = FindCatalog();
            if (catalog == null)
            {
                Debug.LogError("找不到标签表。先打开标签图，它会在 Assets/GameplayTags 里建一份。");
                return;
            }

            GameObject go = GameObject.Find(ObjectName);
            if (go == null)
            {
                go = new GameObject(ObjectName);
                Undo.RegisterCreatedObjectUndo(go, "放按 Q 演示");
            }

            GameplayTagPlayDemo demo = go.GetComponent<GameplayTagPlayDemo>();
            if (demo == null)
                demo = Undo.AddComponent<GameplayTagPlayDemo>(go);

            var serialized = new SerializedObject(demo);
            serialized.FindProperty("_catalog").objectReferenceValue = catalog;
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
            Debug.Log("已经放好。按播放，再按 Q。左上角会显示玩家和 Boss 各自的状态。");
        }

        static GameplayTagCatalog FindCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameplayTagCatalog");
            if (guids == null || guids.Length == 0)
                return null;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.StartsWith("Packages/"))
                    continue;
                return AssetDatabase.LoadAssetAtPath<GameplayTagCatalog>(path);
            }

            return null;
        }
    }
}
