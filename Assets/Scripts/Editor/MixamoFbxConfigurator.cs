#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 一键配置 Mixamo FBX 包：
    ///   - 自动识别 mesh fbx（Y Bot）vs animation fbx
    ///   - Mesh: AnimationType = Humanoid, Create From This Model
    ///   - Animation: Humanoid, Copy From Other Avatar (mesh fbx 的 avatar)
    ///   - 所有 clip 都 Bake Into Pose XZ + Y（位移交给 Rigidbody）
    ///   - idle / walk / run / strafe 类自动 Loop Time = ✓
    ///   - jump / turn / dash / land 类 Loop Time = ✗
    ///
    /// 用法：在 Project 窗口选中**包含 FBX 的文件夹**（比如 Assets/Art/Characters/YBot），
    /// 菜单 Tools/Mixamo/Configure FBX Pack → 选择 mesh fbx → 一键配完。
    /// </summary>
    public static class MixamoFbxConfigurator
    {
        [MenuItem("Tools/Mixamo/Configure FBX Pack (Humanoid + Bake Pose)")]
        public static void Configure()
        {
            string folder = GetSelectedFolder();
            if (string.IsNullOrEmpty(folder))
            {
                EditorUtility.DisplayDialog("Error", "请在 Project 窗口选中一个文件夹再跑这个工具。", "OK");
                return;
            }

            // 找所有 fbx
            var fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { folder });
            if (fbxGuids.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", $"{folder} 里没找到 FBX。", "OK");
                return;
            }

            // 让用户选哪个是 mesh
            var paths = new System.Collections.Generic.List<string>();
            foreach (var g in fbxGuids) paths.Add(AssetDatabase.GUIDToAssetPath(g));
            paths.Sort();

            // mesh 通常就是名字最像 "*Bot.fbx" / "*Character*" / 体积最大的那个，自动猜
            string meshPath = null;
            long biggest = 0;
            foreach (var p in paths)
            {
                var fi = new FileInfo(p);
                if (fi.Length > biggest) { biggest = fi.Length; meshPath = p; }
            }

            if (!EditorUtility.DisplayDialog("确认 mesh 文件",
                $"自动选中（按文件大小）作为带 mesh 的 FBX：\n\n  {meshPath}\n\n其余 {paths.Count - 1} 个 FBX 视为动画文件。\n\n是否继续？",
                "继续", "取消"))
                return;

            // ---- 1. 配 mesh ----
            var meshImporter = AssetImporter.GetAtPath(meshPath) as ModelImporter;
            if (meshImporter == null) { Debug.LogError($"拿不到 ModelImporter: {meshPath}"); return; }

            meshImporter.animationType = ModelImporterAnimationType.Human;
            meshImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            meshImporter.optimizeGameObjects = false;
            meshImporter.SaveAndReimport();

            // 拿到生成的 Avatar
            Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(meshPath);
            if (avatar == null)
            {
                // 有时候 Avatar 是子资产，要遍历找
                var subs = AssetDatabase.LoadAllAssetsAtPath(meshPath);
                foreach (var s in subs) if (s is Avatar a) { avatar = a; break; }
            }
            if (avatar == null)
            {
                Debug.LogError($"[MixamoFbxConfigurator] 配完 mesh 但找不到生成的 Avatar，停止。");
                return;
            }

            // ---- 2. 配每个动画 ----
            int configured = 0;
            foreach (var p in paths)
            {
                if (p == meshPath) continue;

                var imp = AssetImporter.GetAtPath(p) as ModelImporter;
                if (imp == null) continue;

                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                imp.sourceAvatar = avatar;
                imp.optimizeGameObjects = false;

                // clip 配置
                var clips = imp.defaultClipAnimations;
                bool isLoopType = LooksLikeLoopAnim(p);
                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i].loopTime = isLoopType;
                    clips[i].loopPose = isLoopType;
                    clips[i].lockRootRotation = false;
                    clips[i].lockRootHeightY = true;     // Bake Into Pose Y
                    clips[i].lockRootPositionXZ = true;  // Bake Into Pose XZ
                    clips[i].keepOriginalOrientation = true;
                    clips[i].keepOriginalPositionXZ = false;
                    clips[i].keepOriginalPositionY = false;
                }
                imp.clipAnimations = clips;
                imp.SaveAndReimport();
                configured++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MixamoFbxConfigurator] 完成。Mesh：{Path.GetFileName(meshPath)}，动画 {configured} 个。");
            EditorUtility.DisplayDialog("Done",
                $"Mesh: {Path.GetFileName(meshPath)}\n动画: {configured} 个\n\n所有 clip 都 Bake Into Pose XZ+Y，位移交给 Rigidbody。",
                "OK");
        }

        private static bool LooksLikeLoopAnim(string path)
        {
            string n = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            // 一次性动作
            if (n.Contains("jump")) return false;
            if (n.Contains("turn")) return false;
            if (n.Contains("dash")) return false;
            if (n.Contains("land")) return false;
            if (n.Contains("attack")) return false;
            if (n.Contains("hit")) return false;
            if (n.Contains("die") || n.Contains("death")) return false;
            // 默认循环（idle/walk/run/strafe/...）
            return true;
        }

        private static string GetSelectedFolder()
        {
            foreach (var obj in Selection.objects)
            {
                string p = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(p)) continue;
                if (AssetDatabase.IsValidFolder(p)) return p;
            }
            return null;
        }
    }
}
#endif
