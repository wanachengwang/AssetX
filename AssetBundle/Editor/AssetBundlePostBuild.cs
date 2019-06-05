using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace AssetX
{
    public class AssetBundlePostBuild : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool dirtyTag = false;
            for (int i = 0; i < importedAssets.Length; i++)
            {
                var item = importedAssets[i];
                if (item.ToLower().Contains(AssetBundleConfig.SrcAssetsDir))
                {
                    dirtyTag = true;
                    break;
                }
            }
            if (dirtyTag)
                AssetManager.Instance.Refresh();
        }
    }
}