using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using LitJson;

namespace AssetX
{
    class AssetBundleBuildPanel : EditorWindow
    {
        const float GAP = 5;
        const float BTN_W = 50;
        const float TAG_W = 50;
        const float TOG_W = 16;
            
        private float _previewPadding = 0;
        private Abb160.AssetBundleManageTab _abPreviewTab;
        private ReorderableList _filterList;
        private Vector2 _scrollPosition = Vector2.zero;

        [MenuItem("Tools/AssetX Bundle Config")]
        static void Open()
        {
            GetWindow<AssetBundleBuildPanel>("AssetX Bundle", true);
        }

        private void OnEnable()
        {
            if (_filterList == null)
            {
                _filterList = new ReorderableList(AssetBundleConfig.Filters, typeof(AssetBundleConfig.PathFilter));
                _filterList.drawElementCallback = OnListElementGUI;
                _filterList.drawHeaderCallback = OnListHeaderGUI;
                _filterList.elementHeight = 22;
                _filterList.draggable = false;
                _filterList.displayAdd = false;
                _filterList.displayRemove = false;
                //_filterList.onAddCallback = (list) => AddFilter();
            }

            if (_abPreviewTab == null)
                _abPreviewTab = new Abb160.AssetBundleManageTab();
            _abPreviewTab.OnEnable(new Rect(0, 0, position.width, position.height), this);
        }

        private void Update()
        {
            _abPreviewTab.Update();
        }

        private void OnGUI()
        {
            float padding = 0;
            //command button bar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                {
                    AssetBundleConfig.SaveConfig();
                }
                if (GUILayout.Button("Apply", EditorStyles.toolbarButton))
                {
                    Apply();
                }
                if (GUILayout.Button("DebugManifest", EditorStyles.toolbarButton))
                {
                    AssetBundleBuilder.GenerateAssetBundle(BuildTarget.StandaloneWindows64, false);
                }
                //GUILayout.FlexibleSpace();
                if (GUILayout.Button("Build", EditorStyles.toolbarButton))
                {
                    Build();
                }
                padding += 18;
            }
            GUILayout.EndHorizontal();

            //filters context
            GUILayout.BeginVertical();
            {
                //Filter item list
                //float lstH = _filterList.count * 22;
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);//, GUILayout.Height(2+position.height/2));
                {
                    _filterList.DoLayoutList();
                }
                GUILayout.EndScrollView();
                padding += 18 + 18 + (_filterList.count == 0 ? 1 : _filterList.count) * _filterList.elementHeight;
            }
            GUILayout.EndVertical();

            //preview
            _abPreviewTab.OnGUI(new Rect(0, padding, position.width, position.height - padding));

            //set dirty
            //if (GUI.changed)
            //    EditorUtility.SetDirty(_config);
        }

        void OnListHeaderGUI(Rect rect)
        {
            float fieldWidth = (rect.xMax - 2*BTN_W - TAG_W - TOG_W) / 2 - GAP * 4;
            Rect r = rect;
            r.height = 18;

            r.width = TOG_W + GAP + TAG_W;

            r.xMin = r.xMax + GAP;
            r.width = fieldWidth;
            EditorGUI.LabelField(r, "Asset Path");

            r.xMin = r.xMax + GAP;
            r.width = BTN_W;
            //EditorGUI.LabelField(r, "Filter");

            r.xMin = r.xMax + GAP;
            r.width = fieldWidth;
            EditorGUI.LabelField(r, "Dest Path");
        }
        void OnListElementGUI(Rect rect, int index, bool isactive, bool isfocused)
        {
            AssetBundleConfig.PathFilter filter = AssetBundleConfig.Filters[index];
            rect.y++;

            float fieldWidth = (rect.xMax - 2 * BTN_W - TAG_W - TOG_W) / 2 - GAP * 4;
            Rect r = rect;
            r.height = 18;

            r.width = TOG_W;
            filter._bValid = GUI.Toggle(r, filter._bValid, GUIContent.none);
            
            r.xMin = r.xMax + GAP;
            r.width = TAG_W;
            GUI.Label(r, filter._tag);

            r.xMin = r.xMax + GAP;
            r.width = fieldWidth;
            GUI.enabled = false;
            GUI.TextField(r, filter.GetPrjRelativeSrcDir());
            GUI.enabled = true;

            r.xMin = r.xMax + GAP;
            r.width = BTN_W;
            if (GUI.Button(r, "Select"))
            {
                var path = SelectFolder(AssetBundleConfig.OriginAssetsRoot);
                if (path != null)
                    filter._srcDir = path.ToLower();
            }

            r.xMin = r.xMax + GAP;
            r.width = fieldWidth;
            GUI.enabled = false;
            GUI.TextField(r, filter.GetPrjRelativeDstDir());
            GUI.enabled = true;

            r.xMin = r.xMax + GAP;
            r.width = BTN_W;
            if (GUI.Button(r, "Select"))
            {
                var path = SelectFolder(AssetBundleConfig.AssetBundlesRoot);
                if (path != null)
                    filter._dstDir = path.ToLower();
            }
        }

        void Apply()
        {
            AssetBundleBuilder.AutoSetAssetBundleNames();
        }
        void Build()
        {
            AssetBundleBuilder.AutoSetAssetBundleNames();
            AssetBundleBuilder.BuildAssetBundle(BuildTarget.StandaloneWindows64);
        }
        string SelectFolder(string rootPath)
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Path", rootPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.StartsWith(rootPath))
                {
                    if(rootPath.Length < selectedPath.Length)
                        return selectedPath.Substring(rootPath.Length + 1);
                    return string.Empty;
                }
                else
                {
                    ShowNotification(new GUIContent("不能在(" + rootPath + ")目录之外!"));
                }
            }
            return null;
        }
    }
}
