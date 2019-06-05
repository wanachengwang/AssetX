using System;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

namespace AssetX
{
    #region AssetBundleManifest
    [Serializable]
    public class AssetBundleInfo
    {
        public string relativePath;
        public string MD5;
        public string[] allAssetsName;
        public string[] dependencies;
        #region serialize
        public void JsonSerialize(ref JsonWriter writter)
        {
            writter.WriteObjectStart();
			writter.WritePropertyName("relativePath");writter.Write(relativePath);
			writter.WritePropertyName("MD5");writter.Write(MD5);
            writter.WritePropertyName("allAssetsName");
            writter.WriteArrayStart();
            for (int i = 0; i < allAssetsName.Length; i++)
            {
                writter.Write(allAssetsName[i]);
            }
            writter.WriteArrayEnd();
            writter.WritePropertyName("dependencies");
            writter.WriteArrayStart();
            for (int i = 0; i < dependencies.Length; i++)
            {
                writter.Write(dependencies[i]);
            }
            writter.WriteArrayEnd();
            writter.WriteObjectEnd();
        }
        public void JsonDeserialize(JsonData jd)
        {
            relativePath = GetString(jd["relativePath"]);
            MD5 = GetString(jd["MD5"]);
            List<string> tmp = new List<string>();
            var jdAllAssetsName = jd["allAssetsName"];
            if (jdAllAssetsName != null)
            {
                for (int i = 0; i < jdAllAssetsName.Count; i++)
                {
                    tmp.Add(GetString(jdAllAssetsName[i]));
                }
            }
            allAssetsName = tmp.ToArray();

            tmp.Clear();
            var jdDependencies = jd["dependencies"];
            if (jdDependencies != null)
            {
                for (int i = 0; i < jdDependencies.Count; i++)
                {
                    tmp.Add(GetString(jdDependencies[i]));
                }
            }
            dependencies = tmp.ToArray();
        }
        string GetString(JsonData jd)
        {
            if (jd != null) return jd.ToString();
            else return null;
        }
        #endregion
    }
    public class CustomAssetBundleManifest
    {
        public bool IsCompressed { get; private set; }
        public Dictionary<string, AssetBundleInfo> AssetMap { get; private set; }

        #region static serialize
        public static void JsonSerialize(List<AssetBundleInfo> assetBundleInfos, string path, bool isCompress)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            JsonWriter writer = new JsonWriter(sb);
            writer.PrettyPrint = true;
            writer.WriteObjectStart();

            writer.WritePropertyName("compress");
            writer.Write(isCompress);

            writer.WritePropertyName("manifest");
            writer.WriteArrayStart();
            for (int i = 0; i < assetBundleInfos.Count; i++)
            {
                assetBundleInfos[i].JsonSerialize(ref writer);
            }
            writer.WriteArrayEnd();
            writer.WriteObjectEnd();

            System.IO.File.WriteAllText(path, sb.ToString());
        }
        public static CustomAssetBundleManifest CreateInstance(byte[] bytes)
        {
            string text = System.Text.Encoding.Default.GetString(bytes);
            return CreateInstance(text);
        }
        public static CustomAssetBundleManifest CreateInstance(string text)
        {
            var manifest = new CustomAssetBundleManifest();
            manifest.AssetMap = new Dictionary<string, AssetBundleInfo>();
            JsonData jd = JsonMapper.ToObject(text.Trim());
            manifest.IsCompressed = System.Convert.ToBoolean(jd["compress"].ToString());
            JsonData jdManifest = jd["manifest"];
            for (int i = 0; i < jdManifest.Count; i++)
            {
                var assetBundleInfo = new AssetBundleInfo();
                assetBundleInfo.JsonDeserialize(jdManifest[i]);
                manifest.AssetMap[assetBundleInfo.relativePath] = assetBundleInfo;
            }
            return manifest;
        }
        #endregion

        public static string PlaformPath
        {
            get
            {
                return System.IO.Path.Combine(AssetBundleConfig.PersistentDataPath, AssetBundleConfig.ManifestRelativePath).Replace("\\", "/");
            }
        }

        /// <param name="lh"></param>
        /// <param name="rh"></param>
        /// <returns>defference between lh an rh, rh have but lh not have.</returns>
        public static List<string> GetDifference(CustomAssetBundleManifest lh, CustomAssetBundleManifest rh)
        {
            List<string> list = new List<string>();
            if (rh == null) { return list; }
            using (var itr = rh.AssetMap.GetEnumerator())
            {
                while (itr.MoveNext())
                {
                    string key = itr.Current.Key;
                    if (lh != null && lh.Exists(key) && lh.GetMD5(key) == rh.GetMD5(key))
                        continue;
                    list.Add(key);
                }
            }
            return list;
        }
        public static List<string> GetSame(CustomAssetBundleManifest lh, CustomAssetBundleManifest rh)
        {
            List<string> list = new List<string>();
            if (lh == null || rh == null) { return list; }
            using (var itr = rh.AssetMap.GetEnumerator())
            {
                while (itr.MoveNext())
                {
                    string key = itr.Current.Key;
                    if (lh.Exists(key) && lh.GetMD5(key) == rh.GetMD5(key))
                    {
                        list.Add(key);
                    }
                }
            }
            return list;
        }
        public string[] GetDirectDependencies(string assetBundlePath)
        {
            if (!Exists(assetBundlePath))
            {
                Debug.LogError("assetbundle : " + assetBundlePath + " is not existed!");
                return new string[] { };
            }
            return AssetMap[assetBundlePath].dependencies;
        }
        public string GetMD5(string assetBundlePath)
        {
            if (!Exists(assetBundlePath))
            {
                return string.Empty;
            }
            return AssetMap[assetBundlePath].MD5;
        }
        public bool Exists(string assetBundleRelativePath)
        {
            return AssetMap.ContainsKey(assetBundleRelativePath);
        }
        public bool TryGetValue(string assetBundleRelativePath, out AssetBundleInfo info)
        {
            return AssetMap.TryGetValue(assetBundleRelativePath, out info);
        }
    }
    #endregion

#if UNITY_EDITOR
    #region DependencyManifest
    public class DependencyManifest
    {
        private class ManifestData
        {
            //key:asset path
            //val:bundleName
            public Dictionary<string, string> _assetsMap = new Dictionary<string, string>();
        }
        private static ManifestData _manifestData;

        public static void LoadData()
        {
            string strData = System.IO.File.ReadAllText(AssetBundleConfig.PrjRelativeDepsPath);
            _manifestData = JsonMapper.ToObject<ManifestData>(strData.Trim());
            foreach (var data in _manifestData._assetsMap)
            {
                UnityEditor.AssetImporter.GetAtPath(data.Key).assetBundleName = data.Value;
            }
        }
        public static void SaveData()
        {
            string jsonStr = JsonMapper.ToJson(_manifestData);
            System.IO.File.WriteAllText(AssetBundleConfig.PrjRelativeDepsPath, jsonStr);
        }
        public static void Clear()
        {
            if(_manifestData == null)
            {
                _manifestData = new ManifestData();
            }
            else
            {
                _manifestData._assetsMap.Clear();
            }
        }
        public static void Add(string assetPath, string bundleName)
        {
            _manifestData._assetsMap[assetPath] = bundleName;
        }
    }
    #endregion
#endif

    #region SceneBundleManifest
    public class SceneVersionInfo
    {
        public string scenename;//场景名字
        public string version;//场景版本

        #region serialize
        public void JsonSerialize(ref JsonWriter writter)
        {
            writter.WriteObjectStart();
            writter.WritePropertyName("sceneName");writter.Write(scenename);
            writter.WritePropertyName("version");writter.Write(version);
            writter.WriteObjectEnd();
        }
        public void JsonDeserialize(JsonData jd)
        {
            scenename = GetString(jd["sceneName"]);
            version = GetString(jd["version"]);
        }

        string GetString(JsonData jd)
        {
            if (jd != null) return jd.ToString();
            else return null;
        }
        #endregion

        public static SceneVersionInfo GetIntance(byte[] bytes)
        {
            string text = System.Text.Encoding.Default.GetString(bytes);
            JsonData jd = JsonMapper.ToObject(text.Trim());

            var sceneinfo = new SceneVersionInfo();
            sceneinfo.JsonDeserialize(jd);
            return sceneinfo;
        }
    }
    public class CustomSceneVersionManifest//记录场景版本信息
    {
        public Dictionary<string, SceneVersionInfo> SceneMap { get; private set; }
        public static void JsonSerialize(List<SceneVersionInfo> sceneversioninfo, string path)//path为保存路径
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            JsonWriter writer = new JsonWriter(sb);
            writer.PrettyPrint = true;
            writer.WriteObjectStart();
            writer.WritePropertyName("manifest");
            writer.WriteArrayStart();
            for (int i = 0; i < sceneversioninfo.Count; i++)
            {
                sceneversioninfo[i].JsonSerialize(ref writer);

            }
            writer.WriteArrayEnd();
            writer.WriteObjectEnd();
            System.IO.File.WriteAllText(path, sb.ToString());

        }
        public static CustomSceneVersionManifest CreateInstance(byte[] bytes)
        {
            string text = System.Text.Encoding.Default.GetString(bytes);
            return CreateInstance(text);
        }

        public static CustomSceneVersionManifest CreateInstance(string text)
        {
            var manifest = new CustomSceneVersionManifest();
            manifest.SceneMap = new Dictionary<string, SceneVersionInfo>();
            JsonData jd = JsonMapper.ToObject(text.Trim());

            JsonData jdManifest = jd["manifest"];
            for (int i = 0; i < jdManifest.Count; i++)
            {
                var assetBundleInfo = new SceneVersionInfo();
                assetBundleInfo.JsonDeserialize(jdManifest[i]);
                manifest.SceneMap[assetBundleInfo.scenename] = assetBundleInfo;
            }
            return manifest;
        }
    }
    #endregion
}
