// Modifications Copyright (c) 2026 TerminalJack
// Licensed under the MIT License. See the LICENSE.TXT file in the project root for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Stui
{
    /// <summary>
    /// 批量修复 Spriter 导入后 Transform Z 旋转曲线中多出的整圈角度。
    /// </summary>
    public static class ImportedRotationCurveRepair
    {
        private const float AnglePeriod = 360f;
        private const float HalfAnglePeriod = 180f;
        private const float ComparisonEpsilon = 0.001f;

        /// <summary>
        /// 修复当前选中预制体中的导入旋转曲线。
        /// </summary>
        [MenuItem("Tools/Spriter/Repair Imported Rotation Curves", false, 200)]
        private static void RepairSelectedPrefab()
        {
            string assetPath = GetSelectedPrefabPath();
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog(
                    "Spriter Rotation Repair",
                    "请先在 Project 窗口中选择一个预制体。",
                    "确定");
                return;
            }

            List<AnimationClip> clips = GetAnimationClips(assetPath);
            int repairCount = CountRepairableKeys(clips);

            if (repairCount == 0)
            {
                EditorUtility.DisplayDialog(
                    "Spriter Rotation Repair",
                    $"未在 {Path.GetFileName(assetPath)} 中找到需要修复的旋转关键帧。",
                    "确定");
                return;
            }

            string message =
                $"将在 {Path.GetFileName(assetPath)} 中修复 {repairCount} 个旋转关键帧。\n\n" +
                "执行前会在项目根目录的 AnimationRotationBackups 文件夹中创建原始预制体备份。\n" +
                "是否继续？";

            if (!EditorUtility.DisplayDialog("Spriter Rotation Repair", message, "修复", "取消"))
            {
                return;
            }

            string backupPath = CreateBackup(assetPath);
            if (string.IsNullOrEmpty(backupPath))
            {
                EditorUtility.DisplayDialog(
                    "Spriter Rotation Repair",
                    "无法创建原始预制体备份，已取消修复。请查看 Console 中的错误信息。",
                    "确定");
                return;
            }

            ApplyRepairs(assetPath, clips, backupPath);
        }

        /// <summary>
        /// 控制修复菜单是否只在选中预制体资源时可用。
        /// </summary>
        [MenuItem("Tools/Spriter/Repair Imported Rotation Curves", true)]
        private static bool ValidateRepairSelectedPrefab()
        {
            return !string.IsNullOrEmpty(GetSelectedPrefabPath());
        }

        /// <summary>
        /// 获取当前选中的预制体资源路径，也支持选中预制体内的动画子资源。
        /// </summary>
        private static string GetSelectedPrefabPath()
        {
            if (Selection.activeObject == null)
            {
                return null;
            }

            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            return assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
                ? assetPath
                : null;
        }

        /// <summary>
        /// 收集预制体文件中所有可编辑的 AnimationClip，并去除重复引用。
        /// </summary>
        private static List<AnimationClip> GetAnimationClips(string assetPath)
        {
            var clips = new HashSet<AnimationClip>();

            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is AnimationClip clip)
                {
                    clips.Add(clip);
                }
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                return clips.ToList();
            }

            foreach (Animator animator in prefab.GetComponentsInChildren<Animator>(true))
            {
                if (animator.runtimeAnimatorController == null)
                {
                    continue;
                }

                foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                {
                    if (clip != null && IsClipOwnedByAsset(clip, assetPath))
                    {
                        clips.Add(clip);
                    }
                }
            }

            foreach (Animation animation in prefab.GetComponentsInChildren<Animation>(true))
            {
                foreach (AnimationState state in animation)
                {
                    if (state.clip != null && IsClipOwnedByAsset(state.clip, assetPath))
                    {
                        clips.Add(state.clip);
                    }
                }
            }

            return clips.ToList();
        }

        /// <summary>
        /// 判断动画片段是否实际嵌入当前预制体资源。
        /// </summary>
        private static bool IsClipOwnedByAsset(AnimationClip clip, string assetPath)
        {
            return string.Equals(
                AssetDatabase.GetAssetPath(clip),
                assetPath,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 统计所有旋转曲线中可以自动修复的关键帧数量。
        /// </summary>
        private static int CountRepairableKeys(IEnumerable<AnimationClip> clips)
        {
            int count = 0;

            foreach (AnimationClip clip in clips)
            {
                foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (!IsRotationZBinding(binding))
                    {
                        continue;
                    }

                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    count += CountRepairableKeys(curve);
                }
            }

            return count;
        }

        /// <summary>
        /// 统计单条旋转曲线中需要进行连续角度展开的关键帧数量。
        /// </summary>
        private static int CountRepairableKeys(AnimationCurve curve)
        {
            if (curve == null || curve.length < 2)
            {
                return 0;
            }

            Keyframe[] keys = curve.keys;
            float previousAngle = keys[0].value;
            int count = 0;

            for (int i = 1; i < keys.Length; i++)
            {
                float correctedAngle;
                if (TryGetCorrectedAngle(previousAngle, keys[i].value, out correctedAngle))
                {
                    count++;
                    previousAngle = correctedAngle;
                }
                else
                {
                    previousAngle = keys[i].value;
                }
            }

            return count;
        }

        /// <summary>
        /// 对预制体中的所有旋转曲线执行修复并保存资源。
        /// </summary>
        private static void ApplyRepairs(string assetPath, IEnumerable<AnimationClip> clips, string backupPath)
        {
            int repairedKeys = 0;
            int repairedCurves = 0;
            var details = new StringBuilder();

            foreach (AnimationClip clip in clips)
            {
                foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (!IsRotationZBinding(binding))
                    {
                        continue;
                    }

                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    int curveRepairCount = RepairCurve(curve, details, clip.name, binding.path);
                    if (curveRepairCount == 0)
                    {
                        continue;
                    }

                    AnimationUtility.SetEditorCurve(clip, binding, curve);
                    EditorUtility.SetDirty(clip);
                    repairedKeys += curveRepairCount;
                    repairedCurves++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            Debug.Log(
                $"[SpriterRotationRepair] 已修复 {repairedKeys} 个关键帧，涉及 {repairedCurves} 条曲线。" +
                $"\n预制体：{assetPath}\n备份：{backupPath}\n{details}");

            EditorUtility.DisplayDialog(
                "Spriter Rotation Repair",
                $"修复完成。\n\n关键帧：{repairedKeys}\n曲线：{repairedCurves}\n备份：{backupPath}",
                "确定");
        }

        /// <summary>
        /// 修复单条曲线中跨越 360 度的角度，并更新受影响线段的线性切线。
        /// </summary>
        private static int RepairCurve(AnimationCurve curve, StringBuilder details, string clipName, string bindingPath)
        {
            if (curve == null || curve.length < 2)
            {
                return 0;
            }

            Keyframe[] keys = curve.keys;
            bool[] changed = new bool[keys.Length];
            float previousAngle = keys[0].value;
            int repairCount = 0;

            for (int i = 1; i < keys.Length; i++)
            {
                float originalAngle = keys[i].value;
                float correctedAngle;

                if (TryGetCorrectedAngle(previousAngle, originalAngle, out correctedAngle))
                {
                    Keyframe key = keys[i];
                    key.value = correctedAngle;
                    keys[i] = key;
                    changed[i] = true;
                    repairCount++;

                    details.AppendLine(
                        $"  {clipName} / {bindingPath} / t={key.time:0.####}: " +
                        $"{originalAngle:0.###} -> {correctedAngle:0.###}");

                    previousAngle = correctedAngle;
                }
                else
                {
                    previousAngle = originalAngle;
                }
            }

            if (repairCount == 0)
            {
                return 0;
            }

            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (!changed[i] && !changed[i + 1])
                {
                    continue;
                }

                float duration = keys[i + 1].time - keys[i].time;
                if (duration <= 0f)
                {
                    continue;
                }

                float slope = (keys[i + 1].value - keys[i].value) / duration;
                if (float.IsNaN(slope) || float.IsInfinity(slope))
                {
                    continue;
                }

                Keyframe startKey = keys[i];
                startKey.outTangent = slope;
                keys[i] = startKey;

                Keyframe endKey = keys[i + 1];
                endKey.inTangent = slope;
                keys[i + 1] = endKey;
            }

            curve.keys = keys;
            return repairCount;
        }

        /// <summary>
        /// 判断绑定是否为 Spriter 导入器使用的 Transform Z 欧拉角曲线。
        /// </summary>
        private static bool IsRotationZBinding(EditorCurveBinding binding)
        {
            return binding.type == typeof(Transform) &&
                (binding.propertyName == "localEulerAnglesRaw.z" ||
                 binding.propertyName == "localEulerAngles.z");
        }

        /// <summary>
        /// 计算当前角度的等价值中与前一关键帧最接近的连续角度。
        /// </summary>
        private static bool TryGetCorrectedAngle(float previousAngle, float currentAngle, out float correctedAngle)
        {
            correctedAngle = currentAngle;

            float candidate = currentAngle;
            while (candidate - previousAngle > HalfAnglePeriod)
            {
                candidate -= AnglePeriod;
            }

            while (previousAngle - candidate > HalfAnglePeriod)
            {
                candidate += AnglePeriod;
            }

            if (Mathf.Abs(candidate - currentAngle) <= ComparisonEpsilon)
            {
                return false;
            }

            correctedAngle = candidate;
            return true;
        }

        /// <summary>
        /// 将原始预制体复制到项目根目录外的备份文件夹。
        /// </summary>
        private static string CreateBackup(string assetPath)
        {
            try
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string backupDirectory = Path.Combine(projectRoot, "AnimationRotationBackups");
                Directory.CreateDirectory(backupDirectory);

                string fileName = Path.GetFileNameWithoutExtension(assetPath) +
                    "_BeforeRotationRepair_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".prefab";
                string backupPath = Path.Combine(backupDirectory, fileName);
                File.Copy(Path.GetFullPath(assetPath), backupPath, false);
                return backupPath;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SpriterRotationRepair] 创建备份失败：{exception}");
                return null;
            }
        }
    }
}
