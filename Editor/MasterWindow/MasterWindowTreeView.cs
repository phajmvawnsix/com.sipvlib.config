using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SiPVLib.Config.Configs;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SiPVLib.Config.Editor.MasterWindow
{
    /// <summary>
    /// One node in <see cref="MasterWindowTreeView"/>: either a folder (<see cref="Config"/> null)
    /// or a single <see cref="GameConfig"/> asset (leaf).
    /// </summary>
    internal class MasterWindowTreeItem : TreeViewItem
    {
        public GameConfig Config;
        public string FolderPath;
        public string SearchText;
    }

    /// <summary>
    /// Browses every <see cref="GameConfig"/> under the four configured root folders, standing in
    /// for Odin's <c>OdinMenuTree</c>. Supports both view modes MasterWindow previously offered:
    /// <see cref="MenuTreeViewType.Hierarchical"/> mirrors the real folder structure; <see
    /// cref="MenuTreeViewType.FlatWithFolders"/> groups by one folder level, flattening everything
    /// nested inside each.
    /// </summary>
    internal class MasterWindowTreeView : TreeView
    {
        private readonly (string Label, string Path)[] _roots;
        private readonly MenuTreeViewType _viewType;

        public event Action<GameConfig> ConfigDoubleClicked;
        public event Action<MasterWindowTreeItem> FolderContextMenuRequested;
        public event Action<GameConfig[]> ConfigsContextMenuRequested;

        public MasterWindowTreeView(TreeViewState state, (string, string)[] roots, MenuTreeViewType viewType)
            : base(state)
        {
            _roots = roots;
            _viewType = viewType;
            showAlternatingRowBackgrounds = true;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            var allItems = new List<TreeViewItem>();
            var nextId = 1;

            foreach (var (label, path) in _roots)
            {
                var rootItem = new MasterWindowTreeItem { id = nextId++, depth = 0, displayName = label, FolderPath = path };
                allItems.Add(rootItem);

                if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path)) continue;

                switch (_viewType)
                {
                    case MenuTreeViewType.Hierarchical:
                        BuildHierarchical(rootItem, path, 1, allItems, ref nextId);
                        break;
                    case MenuTreeViewType.FlatWithFolders:
                        BuildFlatWithFolders(rootItem, path, allItems, ref nextId);
                        break;
                }
            }

            SetupParentsAndChildrenFromDepths(root, allItems);
            ApplyIconsAndSearch(root);
            return root;
        }

        private void BuildHierarchical(MasterWindowTreeItem parent, string folder, int depth, List<TreeViewItem> allItems, ref int nextId)
        {
            foreach (var config in LoadConfigsDirectlyIn(folder))
            {
                allItems.Add(new MasterWindowTreeItem { id = nextId++, depth = depth, displayName = ConfigDisplayName(config), Config = config });
            }

            foreach (var subFolder in AssetDatabase.GetSubFolders(folder).OrderBy(p => p, StringComparer.Ordinal))
            {
                var subItem = new MasterWindowTreeItem
                {
                    id = nextId++, depth = depth, displayName = Path.GetFileName(subFolder), FolderPath = subFolder,
                };
                allItems.Add(subItem);
                BuildHierarchical(subItem, subFolder, depth + 1, allItems, ref nextId);
            }
        }

        private void BuildFlatWithFolders(MasterWindowTreeItem parent, string rootFolder, List<TreeViewItem> allItems, ref int nextId)
        {
            foreach (var config in LoadConfigsDirectlyIn(rootFolder))
            {
                allItems.Add(new MasterWindowTreeItem { id = nextId++, depth = 1, displayName = ConfigDisplayName(config), Config = config });
            }

            foreach (var subFolder in AssetDatabase.GetSubFolders(rootFolder).OrderBy(p => p, StringComparer.Ordinal))
            {
                var subItem = new MasterWindowTreeItem
                {
                    id = nextId++, depth = 1, displayName = Path.GetFileName(subFolder), FolderPath = subFolder,
                };
                allItems.Add(subItem);

                foreach (var config in LoadConfigsRecursivelyIn(subFolder))
                {
                    allItems.Add(new MasterWindowTreeItem { id = nextId++, depth = 2, displayName = ConfigDisplayName(config), Config = config });
                }
            }
        }

        private static IEnumerable<GameConfig> LoadConfigsDirectlyIn(string folder)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:GameConfig", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(path)?.Replace('\\', '/') != folder) continue;

                var config = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
                if (config != null) yield return config;
            }
        }

        private static IEnumerable<GameConfig> LoadConfigsRecursivelyIn(string folder)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:GameConfig", new[] { folder }))
            {
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (config != null) yield return config;
            }
        }

        private static string ConfigDisplayName(GameConfig config) => config.name;

        // ── Icons / search ──────────────────────────────────────────────

        private void ApplyIconsAndSearch(TreeViewItem root)
        {
            foreach (var raw in root.children ?? Enumerable.Empty<TreeViewItem>())
            {
                ApplyIconsAndSearchRecursive(raw);
            }
        }

        private void ApplyIconsAndSearchRecursive(TreeViewItem raw)
        {
            if (raw is MasterWindowTreeItem item)
            {
                if (item.Config != null)
                {
                    item.icon = ResolveConfigIcon(item.Config);
                    item.SearchText = BuildSearchText(item.Config);
                }
                else
                {
                    var configs = CollectConfigs(item);
                    if (configs.Any(c => !c.IsValid()))
                    {
                        item.icon = EditorGUIUtility.IconContent("console.erroricon").image as Texture2D;
                    }
                }
            }

            if (raw.children == null) return;
            foreach (var child in raw.children)
            {
                ApplyIconsAndSearchRecursive(child);
            }
        }

        private static Texture2D ResolveConfigIcon(GameConfig config)
        {
            if (config == null || !config.IsValid())
            {
                return EditorGUIUtility.IconContent("console.erroricon").image as Texture2D;
            }

            if (config.IgnoreInBuild)
            {
                return EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
            }

            return config.GetEditorIcon() as Texture2D;
        }

        private static string BuildSearchText(GameConfig config)
        {
            var sb = new StringBuilder();
            sb.Append(config.name).Append(' ');

            if (!string.IsNullOrEmpty(config.Id)) sb.Append(config.Id).Append(' ');

            for (var type = config.GetType(); type != null && type != typeof(ScriptableObject); type = type.BaseType)
            {
                sb.Append(type.Name).Append(' ');
            }

            return sb.ToString();
        }

        public static List<GameConfig> CollectConfigs(TreeViewItem item)
        {
            var result = new List<GameConfig>();
            CollectConfigsRecursive(item, result);
            return result;
        }

        private static void CollectConfigsRecursive(TreeViewItem item, List<GameConfig> result)
        {
            if (item is MasterWindowTreeItem { Config: not null } configItem)
            {
                result.Add(configItem.Config);
                return;
            }

            if (item.children == null) return;
            foreach (var child in item.children)
            {
                CollectConfigsRecursive(child, result);
            }
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            if (item is not MasterWindowTreeItem { Config: not null } configItem) return false;

            return configItem.SearchText?.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ── Drag out (so a config can be dropped onto a [ConfigRef] field) ──

        protected override bool CanStartDrag(CanStartDragArgs args) =>
            args.draggedItem is MasterWindowTreeItem { Config: not null };

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            var configs = args.draggedItemIDs
                .Select(id => FindItem(id, rootItem) as MasterWindowTreeItem)
                .Where(item => item?.Config != null)
                .Select(item => (Object) item.Config)
                .ToArray();

            if (configs.Length == 0) return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = configs;
            DragAndDrop.StartDrag(configs.Length == 1 ? configs[0].name : "Configs");
        }

        // ── Interactions ─────────────────────────────────────────────────

        protected override void DoubleClickedItem(int id)
        {
            if (FindItem(id, rootItem) is MasterWindowTreeItem { Config: not null } item)
            {
                ConfigDoubleClicked?.Invoke(item.Config);
            }
        }

        protected override void ContextClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as MasterWindowTreeItem;
            if (item == null) return;

            if (item.Config != null)
            {
                var selectedConfigs = GetSelection()
                    .Select(selId => FindItem(selId, rootItem) as MasterWindowTreeItem)
                    .Where(i => i?.Config != null)
                    .Select(i => i.Config)
                    .ToArray();

                ConfigsContextMenuRequested?.Invoke(selectedConfigs.Length > 0 ? selectedConfigs : new[] { item.Config });
            }
            else
            {
                FolderContextMenuRequested?.Invoke(item);
            }
        }

        public GameConfig GetSelectedConfig()
        {
            var selection = GetSelection();
            if (selection.Count == 0) return null;

            return (FindItem(selection[0], rootItem) as MasterWindowTreeItem)?.Config;
        }

        public void SelectConfig(GameConfig config)
        {
            var match = FindItemRecursive(rootItem, i => i is MasterWindowTreeItem { Config: not null } c && c.Config == config);
            if (match == null) return;

            SetSelection(new List<int> { match.id }, TreeViewSelectionOptions.RevealAndFrame);
        }

        private static TreeViewItem FindItemRecursive(TreeViewItem item, Func<TreeViewItem, bool> predicate)
        {
            if (predicate(item)) return item;
            if (item.children == null) return null;

            foreach (var child in item.children)
            {
                var found = FindItemRecursive(child, predicate);
                if (found != null) return found;
            }

            return null;
        }
    }
}
