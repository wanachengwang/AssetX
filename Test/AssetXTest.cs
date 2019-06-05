using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AssetX;
//using Sirenix.OdinInspector;

#if UNITY_EDITOR
public class AssetXTest : MonoBehaviour
{
    IAssetManager _nativeMgr;
    IAssetManager _assetBundleMgr;

    // Start is called before the first frame update
    void Start()
    {
        _nativeMgr = new NativeAssetManager();
        _nativeMgr.PrepareSystem(null);

        _assetBundleMgr = new AssetBundleManager();
        _assetBundleMgr.PrepareSystem(null);
    }

    //[Button("Load Native")]
    void LoadNative()
    {
        //MapData mapData = AssetX.ResourceManager.Instance.LoadAsset<MapData>("BattleLand");
        //MapData mapData0 = _nativeMgr.LoadAsset<MapData>("BattleLand");
        //Debug.Log(mapData0.MapInfo);
    }

    //[Button("Load AssetBundle")]
    void LoadAssetBundle()
    {
        //MapData mapData0 = _assetBundleMgr.LoadAsset<MapData>("BattleLand");
        //Debug.Log(mapData0.MapInfo);
    }
}
#endif