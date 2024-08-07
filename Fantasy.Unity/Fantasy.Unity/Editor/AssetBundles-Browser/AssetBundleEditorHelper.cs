#if UNITY_EDITOR || UNITY_EDITOR_64
using System;
using System.IO;
using Fantasy;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBrowser
{
    public static class AssetBundleEditorHelper
    {
        [MenuItem("Fantasy/AssetBundle/SetUIName")]
        public static void SetAllName()
        {
            var dir = new DirectoryInfo("Assets/Bundles/UI");
            var directoryInfos = dir.GetDirectories();

            foreach (var directoryInfo in directoryInfos)
            {
                var subDirInfos = directoryInfo.GetDirectories();
                foreach (var subDirInfo in subDirInfos)
                {
                    var assetPath = subDirInfo.FullName.Replace("\\", "/").Replace(UnityEngine.Application.dataPath, "Assets");
                    var assetImporter = AssetImporter.GetAtPath(assetPath);
                    var bundleName = subDirInfo.FullName.Replace("\\", "/").Replace($"{Application.dataPath}/Bundles/", "");
                    var files = subDirInfo.GetFiles();
                    assetImporter.assetBundleName = files.Length > 0 ? bundleName.ToLower() : default;
                }
            }
            AssetDatabase.RemoveUnusedAssetBundleNames();
            
            AssetDatabase.Refresh();

            Debug.Log($"UI文件夹所有AssetBundleName设置完成 共:{directoryInfos.Length}个");
        }

        [MenuItem("Fantasy/AssetBundle/SetABName")]
        [MenuItem("Assets/SetABName", false, priority = 0)]
        static void SetName(MenuCommand menuCommand)
        {
            foreach (var guid in Selection.assetGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var assetImporter = AssetImporter.GetAtPath(assetPath);
                var replace = assetPath.Replace(".", "", StringComparison.Ordinal);
                assetImporter.assetBundleName = Path.GetFileName(replace).ToLower();
            }
        }

        [MenuItem("Fantasy/AssetBundle/CopyToStreamingAssets")]
        [MenuItem("Assets/CopyToStreamingAssets", false, priority = 1)]
        static void CopyToStreamingAssets(MenuCommand menuCommand)
        {
            var assetGUIDs = Selection.assetGUIDs;

            if (assetGUIDs.Length == 0)
            {
                return;
            }

            var assetBundleRootPath = GetAssetBundleRootPath(assetGUIDs[0]);

            if (string.IsNullOrEmpty(assetBundleRootPath))
            {
                EditorUtility.DisplayDialog("CopyToStreamingAssets", "无法复制AssetBundle到StreamingAssets种、请使用框架内置的打AssetBundle工具打包后再尝试", "OK");
                return;
            }

            foreach (var guid in assetGUIDs)
            {
                // 获取资源的路径
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var substring = assetPath.Substring(assetBundleRootPath.Length);
                var target = $"{UnityEngine.Application.streamingAssetsPath}/{substring}";
                // 拷贝文件夹
                if (Directory.Exists(assetPath))
                {
                    AssetBundleComponent.CreateDirectory(substring, UnityEngine.Application.streamingAssetsPath, false);
                    Fantasy.FileHelper.CopyDirectory(assetPath, target, true);
                    continue;
                }

                // 拷贝文件
                AssetBundleComponent.CreateDirectory(substring, UnityEngine.Application.streamingAssetsPath, true);
                File.Copy(assetPath, target, true);
            }

            // 拷贝AssetBundleManifest
            File.Copy($"{assetBundleRootPath}/Fantasy", $"{UnityEngine.Application.streamingAssetsPath}/Fantasy", true);
            // 重新计算下MD5
            var versionPath = $"{UnityEngine.Application.streamingAssetsPath}/{AppDefine.VersionName}";
            var versionBytes = AssetBundleComponent.CalculateMD5(UnityEngine.Application.streamingAssetsPath, false);
            File.WriteAllBytes(versionPath, versionBytes);
            AssetDatabase.Refresh();
        }

        private static string GetAssetBundleRootPath(string guid)
        {
            var dir = "";
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var directories = assetPath.Split('/');

            foreach (var directory in directories)
            {
                dir = $"{dir}{directory}/";

                if (File.Exists($"{dir}version.bytes"))
                {
                    return dir;
                }
            }

            return null;
        }
    }
}
#endif