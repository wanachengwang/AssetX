using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using System.Linq;
//using System;


namespace AssetX.Abb160
{
    internal class AssetListTreeNew : AssetListTree
    {
        private AssetBundleManageTab _parentTab;

        internal AssetListTreeNew(TreeViewState state, MultiColumnHeaderState mchs, AssetBundleManageTab ctrl) :base(state, mchs, ctrl)
        {
            _parentTab = ctrl;
        }

        private bool IsDepsBundle()
        {
            foreach (var bundle in m_SourceBundles)
            {
                if (!bundle.m_Name.bundleName.StartsWith(AssetBundleConfig.AssetBundlesDepsDir))
                    return false;
            }
            return true;
        }

        protected override bool CanBeParent(TreeViewItem item)
        {
            return false;
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            IList<int> selectedNodes = GetSelection();
            if (!IsDepsBundle())
            {
                foreach (var nodeID in selectedNodes)
                {
                    AssetBundleModel.AssetTreeItem item = FindItem(nodeID, rootItem) as AssetBundleModel.AssetTreeItem;
                    if (!string.IsNullOrEmpty(item.asset.realBundleName))
                        return false;
                }
            }
            args.draggedItemIDs = selectedNodes;
            return true;
        }

        protected override void ContextClickedItem(int id)
        {
            if (!IsDepsBundle())
                return;
            base.ContextClickedItem(id);
        }

        protected override void KeyEvent()
        {
            if (!IsDepsBundle())
                return;
            base.KeyEvent();
        }
    }
}
