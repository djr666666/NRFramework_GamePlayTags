using System.IO;
using System.Text;
using UnityEditor;

namespace NRFramework.GameplayTags
{
    static class GameplayTagScriptGenerator
    {
        public const string OutputDirectory = "Assets/GameplayTags";
        public const string OutputPath = "Assets/GameplayTags/Tags.cs";
        public const string UserPath = "Assets/GameplayTags/Tags.User.cs";
        public const string AsmdefPath = "Assets/GameplayTags/NRFramework.GameplayTags.Generated.asmdef";

        const string UserFile =
            "namespace NRFramework.GameplayTags\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// 项目自己的包装写在这里。保存配置不会覆盖这个文件。\r\n" +
            "    /// 标签在 Tags.cs，例如 Tags.State.Control.Frozen。\r\n" +
            "    /// </summary>\r\n" +
            "    public static partial class Tags\r\n" +
            "    {\r\n" +
            "    }\r\n" +
            "}\r\n";

        const string AsmdefFile =
            "{\r\n" +
            "    \"name\": \"NRFramework.GameplayTags.Generated\",\r\n" +
            "    \"rootNamespace\": \"NRFramework.GameplayTags\",\r\n" +
            "    \"references\": [\r\n" +
            "        \"NRFramework.GameplayTags\",\r\n" +
            "        \"NRFramework.GameplayTags.Unity\"\r\n" +
            "    ],\r\n" +
            "    \"includePlatforms\": [],\r\n" +
            "    \"excludePlatforms\": [],\r\n" +
            "    \"allowUnsafeCode\": false,\r\n" +
            "    \"overrideReferences\": false,\r\n" +
            "    \"precompiledReferences\": [],\r\n" +
            "    \"autoReferenced\": true,\r\n" +
            "    \"defineConstraints\": [],\r\n" +
            "    \"versionDefines\": [],\r\n" +
            "    \"noEngineReferences\": false\r\n" +
            "}\r\n";

        public static void Generate(GameplayTagCatalog catalog)
        {
            if (catalog == null)
                return;

            EnsureFolder();
            EnsureUserFile();
            EnsureAsmdef();
            string text = Build(catalog);
            string fullPath = Path.GetFullPath(OutputPath);
            if (File.Exists(fullPath) && File.ReadAllText(fullPath) == text)
                return;

            File.WriteAllText(fullPath, text, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(OutputPath);
        }

        public static string Build(GameplayTagCatalog catalog)
        {
            return GameplayTagScriptText.Build(catalog.CopyTable());
        }

        static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(OutputDirectory))
                return;
            AssetDatabase.CreateFolder("Assets", "GameplayTags");
        }

        static void EnsureUserFile()
        {
            string fullPath = Path.GetFullPath(UserPath);
            if (File.Exists(fullPath))
                return;

            File.WriteAllText(fullPath, UserFile, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(UserPath);
        }

        static void EnsureAsmdef()
        {
            string fullPath = Path.GetFullPath(AsmdefPath);
            if (File.Exists(fullPath))
                return;

            File.WriteAllText(fullPath, AsmdefFile, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(AsmdefPath);
        }
    }
}
