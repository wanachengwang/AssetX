using UnityEngine;
using System.Collections;

namespace AssetX
{
    public enum ProgressState
    {
        Init,
        Load,
        Download,
        Copy,
        Decode,
        Done
    }
    public interface IProgress
    {
        ProgressState CurrentState { get; }
        float Value { get; }
        long AllSize { get; }
        long CurrentSize { get; }
        int AllStepCount { get; }
        int CurrentStep { get; }
    }

    public interface IAssetManager
    {
        void Refresh();
        IProgress PrepareSystem(System.Action finishCallback);
        void CollectionRegister(ref System.Action collection, ref System.Action resetRefrence);
        void UnloadAsset(string assetPath);
        T LoadAsset<T>(string assetPath) where T : UnityEngine.Object;
        GameObject LoadGameObject(string prefab);
        UnityEngine.Object LoadAsset(string assetPath, System.Type type);
        IEnumerator LoadAssetAsycn<T>(string assetPath, System.Action<T> callback) where T : UnityEngine.Object;
        IEnumerator LoadAssetAsycn(string assetPath, System.Type type, System.Action<UnityEngine.Object> callback);
        string SnapShot();
        string[] GetFiles(string path);
    }
}
