using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AssetX
{
    public class AssetBundlePathPaser
    {
        // Key      :item of AssetBundleInfo.allAssetsName; 
        // Value    :AssetBundleInfo.relativePath
        public Dictionary<string, string> Map
        {
            get;
            private set;
        }

        public AssetBundlePathPaser()
        {
            Map = new Dictionary<string, string>();
        }

        public AssetBundlePathPaser(Dictionary<string, AssetBundleInfo> assetInfo)
        {
            Map = new Dictionary<string, string>();
            var itr = assetInfo.GetEnumerator();
            while (itr.MoveNext())
            {
                var item = itr.Current;
                for (int i = 0; i < item.Value.allAssetsName.Length; i++)
                {
                    Map[item.Value.allAssetsName[i]] = item.Key;
                }
            }
        }

        public bool GetAssetPath(string assetPath, out string assetBundlePath)
        {
            if (Map.TryGetValue(assetPath, out assetBundlePath))
            {
                return true;
            }
            string directory = assetPath + "/";
            using (var itr = Map.GetEnumerator())
            {
                while (itr.MoveNext())
                {
                    if (itr.Current.Key.EndsWith(assetPath) && !itr.Current.Key.Contains(directory))
                    {
                        assetBundlePath = itr.Current.Value;
                        return true;
                    }
                }
            }
            assetBundlePath = null;
            return false;
        }

        public string[] GetFiles(string path)
        {
            List<string> subFiles = new List<string>();
            var itr = Map.Keys.GetEnumerator();
            string direcotry = path + "/";
            while (itr.MoveNext())
            {
                if (itr.Current.Contains(path) && !itr.Current.Contains(direcotry))
                {
                    subFiles.Add(itr.Current);
                }
            }
            return subFiles.ToArray();
        }
    }
}