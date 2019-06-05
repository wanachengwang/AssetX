using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using System.Linq;
using System;

namespace AssetX.Abb160
{
    internal class AssetBundleTreeNew : AssetBundleTree
    {
        internal AssetBundleTreeNew(TreeViewState state, AssetBundleManageTab ctrl) : base(state, ctrl)
        {
            CreateDepsFolder();
            AssetBundleModel.Model.OnBundleChanged = UpdateDepsManifest;
        }

        private void AddDepsItemIntoManifest(AssetBundleModel.BundleTreeItem item, Action<string, string> doAddManifest) 
        {
            if(item != null)
            {
                var bundle = item.bundle as AssetBundleModel.BundleDataInfo;
                if (bundle != null)
                {
                    string bundleName = bundle.displayName;
                    foreach(var asset in bundle.Assets)
                    {
                        doAddManifest(asset.fullAssetName, asset.realBundleName);
                    }
                }
                else
                {
                    foreach(var child in item.children)
                    {
                        AddDepsItemIntoManifest(child as AssetBundleModel.BundleTreeItem, doAddManifest);
                    }
                }
            }
        }
        private void UpdateDepsManifest()
        {
            if (rootItem == null)
                return;

            DependencyManifest.Clear();
            var item = GetDepsFolderItem();
            if (item != null)
            {
                AddDepsItemIntoManifest(item, DependencyManifest.Add);
            }
            DependencyManifest.SaveData();
        }
		private bool CheckInDepsFolder(AssetBundleModel.BundleTreeItem item)
        {
			return item.bundle.m_Name.bundleName.StartsWith(AssetBundleConfig.AssetBundlesDepsDir + "/");
        }
		private bool CheckInDepsFolder(List<AssetBundleModel.BundleTreeItem> items)
        {
			foreach(var item in items)
			{
				if(!CheckInDepsFolder(item))
					return false;
			}
            return true;
        }
		private bool CheckDropInDepsFolder(DragAndDropArgs args)
		{
			DragAndDropData data = new DragAndDropData(args);
			var parent = (data.args.parentItem as AssetBundleModel.BundleTreeItem);
			if (parent != null)
			{
				if(parent.bundle.m_Name.bundleName.StartsWith(AssetBundleConfig.AssetBundlesDepsDir))
					return true;
			}
			return false;
		}
        private AssetBundleModel.BundleFolderInfo CreateDepsFolder()
        {
			AssetBundleModel.BundleFolderConcreteInfo depsFolder = new AssetBundleModel.BundleFolderConcreteInfo(AssetBundleConfig.AssetBundlesDepsDir, null);
			return AssetBundleModel.Model.CreateNamedBundleFolder(depsFolder);
        }
        private AssetBundleModel.BundleFolderInfo ValidateFolder(AssetBundleModel.BundleFolderInfo folder)
        {
            if(folder == null)
            {
                // Check and Create deps folder
                var item = GetDepsFolderItem();
                if(item != null)
                    return item.bundle as AssetBundleModel.BundleFolderInfo;
                    
				return CreateDepsFolder();
            }
			else if(folder.m_Name.bundleName.StartsWith(AssetBundleConfig.AssetBundlesDepsDir))
			{
				return folder;
			}
            m_Controller.ShowNotification(new GUIContent("Invalid Operation -- not in deps folder!"));
            return null;
        }
        private AssetBundleModel.BundleTreeItem GetDepsFolderItem()
        {
            string[] folderNames = AssetBundleConfig.Filters[0]._dstDir.Split('/');
            var abRoot = rootItem;
            if (folderNames.Length > 0)
            {
                foreach(string name in folderNames)
                {
                    abRoot = abRoot.children.Find(it => it.displayName == name);
                    if (abRoot == null)
                        return null;
                }
            }
            return abRoot.children.Find(it => it.displayName == AssetBundleConfig.AssetBundlesDepsDirName) as AssetBundleModel.BundleTreeItem;
        }

        //Check if item is under deps folder
		protected override bool CanMultiSelect(TreeViewItem item) {     return CheckInDepsFolder(item as AssetBundleModel.BundleTreeItem); }
		protected override bool CanRename(TreeViewItem item) {          return CheckInDepsFolder(item as AssetBundleModel.BundleTreeItem); }
        protected override bool CanStartDrag(CanStartDragArgs args) {   return false; }

		protected override void ForceReloadData(object context)
		{
			AssetBundleModel.Model.ForceReloadData(this);
			CreateDepsFolder();
		}

        protected override void CreateFolderUnderParent(AssetBundleModel.BundleFolderConcreteInfo folder)
        {
            folder = ValidateFolder(folder) as AssetBundleModel.BundleFolderConcreteInfo;
            if (folder == null)
                return;

            var newBundle = AssetBundleModel.Model.CreateEmptyBundleFolder(folder);
            ReloadAndSelect(newBundle.nameHashCode, true);
        }
        protected override void CreateBundleUnderParent(AssetBundleModel.BundleFolderInfo folder)
        {
            folder = ValidateFolder(folder);
            if (folder == null)
                return;

            var newBundle = AssetBundleModel.Model.CreateEmptyBundle(folder);
            ReloadAndSelect(newBundle.nameHashCode, true);
        }
        protected override void CreateVariantUnderParent(AssetBundleModel.BundleVariantFolderInfo folder)
        {
            if (folder != null)
            {
                folder = ValidateFolder(folder) as AssetBundleModel.BundleVariantFolderInfo;
                if (folder == null)
                    return;

                var newBundle = AssetBundleModel.Model.CreateEmptyVariant(folder);
                ReloadAndSelect(newBundle.nameHashCode, true);
            }
        }

        protected override void ConvertToVariant(object context)
        {
			base.ConvertToVariant(context);
        }
        protected override void DedupeBundles(object context, bool onlyOverlappedAssets)
        {
			base.DedupeBundles(context, onlyOverlappedAssets);
        }
        protected override void DeleteBundles(object b)
        {
            var selectedNodes = b as List<AssetBundleModel.BundleTreeItem>;
			if(!CheckInDepsFolder(selectedNodes))
				return;
            AssetBundleModel.Model.HandleBundleDelete(selectedNodes.Select(item => item.bundle));
            ReloadAndSelect(new List<int>());
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
			if(!CheckDropInDepsFolder(args))
				return DragAndDropVisualMode.Rejected;

			return base.HandleDragAndDrop(args);
        }
    }
}
