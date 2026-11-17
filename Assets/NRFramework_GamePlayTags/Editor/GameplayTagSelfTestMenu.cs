using System;
using UnityEditor;
using UnityEngine;

namespace NRFramework.GameplayTags
{
    static class GameplayTagSelfTestMenu
    {
        [MenuItem("NRFramework/Gameplay Tags/第一步：检查登记表")]
        static void Run()
        {
            try
            {
                GameplayTagRegistrySelfTest.Run();
                Debug.Log("Gameplay Tags 检查通过：登记表、注释，以及玩家和 Boss 各自的容器。");
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.ToString());
            }
        }
    }
}
