using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System;

namespace AssetX
{
    [InitializeOnLoad]
    public static class AssetBundleBuidlerMenuItem
    {
        #region tool
        [MenuItem("打包/Tool/Reset all asset bundle names")]
        public static void ResetAllAssetBundleNames()
        {
            AssetBundleBuilder.ResetAssetBundleNames();
            Debug.Log("Reset all asset bundle name done!");
        }

        [MenuItem("打包/Tool/Show the asset's info")]
        public static void ShowInfo()
        {
            UnityEngine.Object obj = Selection.activeObject;
            string path = AssetDatabase.GetAssetPath(obj);
            string guid = AssetDatabase.AssetPathToGUID(path);
            string[] dependencies = AssetDatabase.GetDependencies(new string[] { path });
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(path + "\n");
            sb.Append("GUID : " + guid + "\n");
            sb.Append("Dependencies : " + "\n");
            foreach (var item in dependencies)
            {
                if (path != item)
                    sb.Append("-" + item + "\n");
            }
            Debug.Log(sb);
        }

        [MenuItem("打包/Tool/Show all asset bundle names")]
        public static void ShowAllAssetBundleNames()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            string[] assetBundleNames = AssetDatabase.GetAllAssetBundleNames();
            foreach (var item in assetBundleNames)
            {
                sb.Append(item + "\n");
            }
            Debug.Log(sb);
        }
        #endregion
    }
}
