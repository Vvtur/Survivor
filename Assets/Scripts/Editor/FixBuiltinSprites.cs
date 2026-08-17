using System.Linq;
using UnityEditor;
using UnityEngine;

namespace QFramework.Gameplay.Editor
{
    /// <summary>
    /// 一键修复 3 个角色 prefab 的 SpriteRenderer 引用：
    /// 当前 prefab 里的 sprite guid 全部是坏的（指向不存在的资源），
    /// 统一替换为 Unity 内置 Sprite（unity_builtin_extra 资源，guid 固定 0000000000000000f000000000000000）。
    /// Player=Square 主角，Enemy=Triangle 敌人，Gem=Circle 宝石。
    /// </summary>
    public static class FixBuiltinSprites
    {
        private const string PlayerPath = "Assets/Res/Art/Prefabs/Player.prefab";
        private const string EnemyPath = "Assets/Res/Art/Prefabs/Enemy.prefab";
        private const string GemPath = "Assets/Res/Art/Prefabs/Gem.prefab";

        [MenuItem("Tools/Fix Builtin Sprites")]
        public static void Fix()
        {
            // 内置 2D 图元 Sprite（Square/Triangle/Circle 等）位于 Library/unity default resources
            // （guid 恒为 0000000000000000e000000000000000），不是 unity_builtin_extra
            var builtinSprites = AssetDatabase.LoadAllAssetsAtPath("Library/unity default resources")
                .OfType<Sprite>()
                .ToDictionary(s => s.name, s => s);

            if (builtinSprites.Count == 0)
            {
                Debug.LogError("未找到任何内置 Sprite，请确认 Unity 版本正常。");
                return;
            }

            Debug.Log($"内置 Sprite 列表: {string.Join(", ", builtinSprites.Keys)}");

            FixPrefab(PlayerPath, "Square", builtinSprites);
            FixPrefab(EnemyPath, "Triangle", builtinSprites);
            FixPrefab(GemPath, "Circle", builtinSprites);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("内置 Sprite 修复完成，请重新打 AB 包！");
        }

        private static void FixPrefab(string prefabPath, string builtinSpriteName,
            System.Collections.Generic.Dictionary<string, Sprite> builtinSprites)
        {
            if (!builtinSprites.TryGetValue(builtinSpriteName, out var sprite))
            {
                Debug.LogError($"{prefabPath} 找不到内置 Sprite: {builtinSpriteName}");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"加载失败: {prefabPath}");
                return;
            }

            var renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"{prefabPath} 没有 SpriteRenderer");
                return;
            }

            foreach (var sr in renderers)
            {
                sr.sprite = sprite;
                EditorUtility.SetDirty(sr);
            }

            PrefabUtility.SavePrefabAsset(prefab);
            Debug.Log($"{prefabPath} 已设置 SpriteRenderer 为内置 {builtinSpriteName}（共 {renderers.Length} 个）");
        }
    }
}
