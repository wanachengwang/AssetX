using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
namespace AssetX
{
    public class NativeAssetManager : IAssetManager
    {
        public class ProgressData : IProgress
        {
            public ProgressState CurrentState
            {
                get { return ProgressState.Done; }
            }

            public float Value
            {
                get { return 1f; }
            }

            public long AllSize
            {
                get { return 1L; }
            }

            public long CurrentSize
            {
                get { return 1L; }
            }

            public int AllStepCount
            {
                get { return 1; }
            }

            public int CurrentStep
            {
                get { return 1; }
            }
        }
        
        NativePathParser PathPaser { get; set; }

 #region IAssetManager
        public NativeAssetManager()
        {
            PrepareSystem(null);
        }
        public void Refresh()
        {
            if (PathPaser == null)
                PathPaser = new NativePathParser();
            PathPaser.Init();
        }

        public IProgress PrepareSystem(System.Action finishCallback)
        {
            PathPaser = new NativePathParser();
            PathPaser.Init();
            if (finishCallback != null)
                finishCallback.Invoke(); 
            return new ProgressData();
        }

        public void UnloadAsset(string asset)
        {
            asset = GeneratePathString(asset);
            string assetPath = null;
            if (PathPaser.GetAssetPath(asset, out assetPath))
            {
            }
        }

        public void CollectionRegister(ref System.Action collection, ref System.Action resetRefrence)
        {
        }

        public T LoadAsset<T>(string asset) where T : UnityEngine.Object
        {
            asset = GeneratePathString(asset);
            string assetPath = null;
            if (PathPaser.GetAssetPath(asset, out assetPath))
            {
                T t = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (t == null)
                {
                    Debug.LogError("failed to load asset:" + assetPath + " whith input asset:" + asset);
                    return null;
                }
                return t;
            }
            else
            {
                Debug.LogWarning("invalid input path:" + asset);
                return null;
            };
        }

        public Object LoadAsset(string asset, System.Type type)
        {
            asset = GeneratePathString(asset);
            string assetPath = null;
            if (PathPaser.GetAssetPath(asset, out assetPath))
            {
                UnityEngine.Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, type);
                if (obj == null)
                {
                    Debug.LogError("failed to load asset:" + assetPath + " whith input asset:" + asset);
                    return null;
                }
                return obj;
            }
            else
            {
                Debug.LogWarning("invalid input path:" + asset);
                return null;
            }
        }

        public IEnumerator LoadAssetAsycn<T>(string asset, System.Action<T> callback) where T : UnityEngine.Object
        {
            yield return null;
            if (callback != null)
            {
                callback.Invoke(LoadAsset<T>(asset));
            }
        }

        public IEnumerator LoadAssetAsycn(string asset, System.Type type, System.Action<UnityEngine.Object> callback)
        {
            yield return null;
            if (callback != null)
            {
                callback.Invoke(LoadAsset(asset, type));
            }
        }

        public string SnapShot()
        {
            return string.Empty;
        }

        public string[] GetFiles(string path)
        {
            path = GeneratePathString(path);
            return PathPaser.GetFiles(path);
        }
#endregion

        public string GeneratePathString(string path)
        {
            string extension = System.IO.Path.GetExtension(path);
            if (extension == string.Empty)
                return path.ToLower().Replace("\\", "/"); 
            else
                return path.ToLower().Replace("\\", "/").Replace(extension, ""); 
        }

        public GameObject LoadGameObject(string prefab)
        {
            return LoadAsset<GameObject>(prefab);
        }
    }
}
#endif