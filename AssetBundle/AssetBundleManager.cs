using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AssetX
{
    public class AssetBundleManager : IAssetManager
    {
        public class LoadedAssetBundle
        {
            public AssetBundle m_AssetBundle;
            public int m_ReferencedCount;

            public LoadedAssetBundle(AssetBundle assetBundle)
            {
                m_AssetBundle = assetBundle;
                m_ReferencedCount = 1;
            }
        }

        Dictionary<string, LoadedAssetBundle> LoadedAssetBundles { get; set; }
        Dictionary<string, string[]> BundleDependencies { get; set; }
        CustomAssetBundleManifest Manifest { get; set; }
        AssetBundlePathPaser PathPaser { get; set; }

        public AssetBundleManager()
        {
            LoadedAssetBundles = new Dictionary<string, LoadedAssetBundle>();
            BundleDependencies = new Dictionary<string, string[]>();
        }

        void Init()
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(CustomAssetBundleManifest.PlaformPath);
            if (assetBundle == null)
                throw new System.Exception("failed to create manifest assetBundle");
            var text = assetBundle.LoadAsset<TextAsset>(AssetBundleConfig.ManifestName);
            if (text == null)
                throw new System.Exception("failed to load manifest");
            Manifest = CustomAssetBundleManifest.CreateInstance(text.bytes);
            PathPaser = new AssetBundlePathPaser(Manifest.AssetMap);
        }

#region IAssetManager
        public void Refresh()
        {
            throw new System.NotImplementedException("only used in EditorMode.");
        }

        public IProgress PrepareSystem(System.Action finishCallback)
        {
            return
                AssetBundlePreparer.Instance.Prepare(
                () => {
                    Init();
                    if (finishCallback != null)
                        finishCallback();
                }, false);

        }

        public void CollectionRegister(ref System.Action collection, ref System.Action resetRefrence)
        {
            resetRefrence += ResetAllRefrenceCount;
            collection += CollectAssetBundle;
        }

        public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            assetPath = GeneratePathString(assetPath);
            string assetBundlePath = null;
            if (PathPaser.GetAssetPath(assetPath, out assetBundlePath))
            {
                Debug.LogWarning(assetBundlePath + ":" + assetPath);
                return LoadAsset<T>(assetBundlePath, assetPath);
            }
            else
            {
                Debug.LogWarning("invalid input path:" + assetPath);
                return null;
            }
        }

        public Object LoadAsset(string assetPath, System.Type type)
        {
            assetPath = GeneratePathString(assetPath);
            string assetBundlePath = null;
            if (PathPaser.GetAssetPath(assetPath, out assetBundlePath))
            {
                return LoadAsset(assetBundlePath, assetPath, type);
            }
            else
            {
                Debug.LogWarning("invalid input path:" + assetPath);
                return null; ;
            }
        }

        public IEnumerator LoadAssetAsycn<T>(string assetPath, System.Action<T> callback) where T : UnityEngine.Object
        {
            assetPath = GeneratePathString(assetPath);
            string assetBundlePath = null;
            if (PathPaser.GetAssetPath(assetPath, out assetBundlePath))
            {
                return LoadAssetAsycn<T>(assetBundlePath, assetPath, callback);
            }
            else
            {
                throw new System.ArgumentException("invalid asset:" + assetPath);
            }
        }

        public IEnumerator LoadAssetAsycn(string assetPath, System.Type type, System.Action<Object> callback)
        {
            assetPath = GeneratePathString(assetPath);
            string assetBundlePath = null;
            if (PathPaser.GetAssetPath(assetPath, out assetBundlePath))
            {
                return LoadAssetAsycn(assetBundlePath, assetPath, type, callback);
            }
            else
            {
                throw new System.ArgumentException("invalid asset:" + assetPath);
            }
        }

        public string SnapShot()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var pair in LoadedAssetBundles)
            {
                sb.Append(pair.Key);
                sb.Append(string.Format("  ReferencedCount : {0}", pair.Value.m_ReferencedCount));
                sb.Append("\n");
            }
            return sb.ToString();
        }

        public string[] GetFiles(string path)
        {
            path = GeneratePathString(path);
            return PathPaser.GetFiles(path);
        }

        public void UnloadAsset(string assetPath)
        {
            assetPath = GeneratePathString(assetPath);
            string assetBundlePath = null;
            if (PathPaser.GetAssetPath(assetPath, out assetBundlePath))
            {
                DecreaseRefrenceCount(assetBundlePath);
            }
        }
#endregion

        T LoadAsset<T>(string assetBundlePath, string assetPath) where T : UnityEngine.Object
        {
            string assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            return LoadAssetBundle(assetBundlePath).LoadAsset<T>(assetName);
        }

        UnityEngine.Object LoadAsset(string assetBundlePath, string assetPath, System.Type type)
        {
            string assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            return LoadAssetBundle(assetBundlePath).LoadAsset(assetName, type);
        }

        IEnumerator LoadAssetAsycn<T>(string assetBundlePath, string assetPath, System.Action<T> callback) where T : UnityEngine.Object
        {
            string assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            AssetBundleRequest request = LoadAssetBundle(assetBundlePath).LoadAssetAsync<T>(assetName);
            yield return request;
            if (callback != null)
            {
                T asset = request.asset as T;
                callback.Invoke(asset);
            }
        }

        IEnumerator LoadAssetAsycn(string assetBundlePath, string assetPath, System.Type type, System.Action<UnityEngine.Object> callback)
        {
            string assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            AssetBundleRequest request = LoadAssetBundle(assetBundlePath).LoadAssetAsync(assetName, type);
            yield return request;
            if (callback != null)
            {
                callback(request.asset);
            }
        }


        AssetBundle LoadAssetBundle(string path)
        {
            path = GeneratePathString(path);
            LoadedAssetBundle loadedAssetBundel = null;
            AssetBundle assetBundle;
            if (LoadedAssetBundles.TryGetValue(path, out loadedAssetBundel))
            {
                loadedAssetBundel.m_ReferencedCount++;
                assetBundle = loadedAssetBundel.m_AssetBundle;
            }
            else
            {
                Debug.Log("LoadAssetBundleFromDisk" + path);
                assetBundle = LoadAssetBundleFromDisk(path);
            }

            if (assetBundle != null)
            {
                Debug.Log(assetBundle.name);
                var dependencies = BundleDependencies[path];
                if (dependencies != null)
                {
                    var itr = dependencies.GetEnumerator();
                    for (int i = 0; i < dependencies.Length; i++)
                    {
                        LoadAssetBundle(dependencies[i]);
                    }
                }
            }
            else
            {
                Debug.LogWarning("failed to load " + path);
            }
            return assetBundle;
        }

        AssetBundle LoadAssetBundleFromDisk(string relativePath)
        {
            if (Manifest.Exists(relativePath))
            {
                string obsolutePath = System.IO.Path.Combine(AssetBundleConfig.PersistentDataPath, relativePath).Replace("\\", "/");
                try
                {
                    Debug.LogWarning("AssetBundle.LoadFromFile " + obsolutePath);
                    var assetBundle = AssetBundle.LoadFromFile(obsolutePath);

                    if (assetBundle != null)
                    {
                        BundleDependencies[relativePath] = Manifest.GetDirectDependencies(relativePath);
                        LoadedAssetBundles[relativePath] = new LoadedAssetBundle(assetBundle);
                    }
                    return assetBundle;
                }
                catch (System.Exception)
                {
                    Debug.LogWarning("create from disk failed : " + obsolutePath);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        void ResetAllRefrenceCount()
        {
            using (var itr = LoadedAssetBundles.Values.GetEnumerator())
            {
                while (itr.MoveNext())
                {
                    itr.Current.m_ReferencedCount = 0;
                }
            }
        }

        void DecreaseRefrenceCount(string assetBundlePath)
        {
            if (LoadedAssetBundles.ContainsKey(assetBundlePath))
            {
                LoadedAssetBundles[assetBundlePath].m_ReferencedCount -= 1;
                var dependencies = BundleDependencies[assetBundlePath];
                for (int index = 0; index < dependencies.Length; index++)
                {
                    DecreaseRefrenceCount(dependencies[index]);
                }

                if (LoadedAssetBundles[assetBundlePath].m_ReferencedCount == 0)
                {
                    LoadedAssetBundles[assetBundlePath].m_AssetBundle.Unload(false);
                    LoadedAssetBundles.Remove(assetBundlePath);
                    BundleDependencies.Remove(assetBundlePath);
                }
            }
        }

        void CollectAssetBundle()
        {
            List<string> names = new List<string>(LoadedAssetBundles.Keys);
            for (int i = 0; i < names.Count; i++)
            {
                if (LoadedAssetBundles[names[i]].m_ReferencedCount == 0)
                {
                    LoadedAssetBundles[names[i]].m_AssetBundle.Unload(false);
                    LoadedAssetBundles.Remove(names[i]);
                    BundleDependencies.Remove(names[i]);
                }
            }
            Resources.UnloadUnusedAssets();
        }

        public string GeneratePathString(string path)
        {
            string extension = System.IO.Path.GetExtension(path);
            if (extension == string.Empty)
            { return path.ToLower().Replace("\\", "/"); }
            else
            { return path.ToLower().Replace("\\", "/").Replace(extension, ""); }
        }

        public GameObject LoadGameObject(string prefab)
        {
            return LoadAsset<GameObject>(prefab);
        }
    }
}