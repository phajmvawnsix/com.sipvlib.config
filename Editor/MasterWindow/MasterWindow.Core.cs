using System.IO;
using SiPVLib.Config.Configs;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SiPVLib.Config.Editor.MasterWindow
{
    /// <summary>
    /// Browses every <see cref="GameConfig"/> under the four configured root folders (left pane,
    /// <see cref="MasterWindowTreeView"/>) with the selected config's inspector alongside it (right
    /// pane). Replaces Odin's <c>OdinMenuEditorWindow</c> with a plain <c>EditorWindow</c> hosting a
    /// standard <c>UnityEditor.IMGUI.Controls.TreeView</c>, so this compiles and works without Odin.
    /// </summary>
    public partial class MasterWindow : EditorWindow
    {
        private string _rootFolderLocal;
        private string _rootFolderResources;
        private string _rootFolderAddressable;
        private string _rootFolderRemoteConfig;

        private MenuTreeViewType _treeViewType = MenuTreeViewType.Hierarchical;

        [SerializeField] private TreeViewState _treeViewState;
        private MasterWindowTreeView _treeView;
        private SearchField _searchField;
        private UnityEditor.Editor _selectedEditor;
        private Object _selectedObject;

        private float _treeWidth = 280f;
        private Vector2 _inspectorScroll;

        [MenuItem("SiPV/Master Window")]
        private static void Open()
        {
            var window = GetWindow<MasterWindow>();
            window.titleContent = new GUIContent("Master Window");
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettingsFields();

            Selection.selectionChanged -= OnSelection;
            Selection.selectionChanged += OnSelection;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelection;

            if (_selectedEditor != null)
            {
                DestroyImmediate(_selectedEditor);
                _selectedEditor = null;
            }
        }

        private void LoadSettingsFields()
        {
            var settings = MasterWindowSettings.instance;
            _rootFolderLocal = settings.rootFolderLocal;
            _rootFolderResources = settings.rootFolderResources;
            _rootFolderAddressable = settings.rootFolderAddressable;
            _rootFolderRemoteConfig = settings.rootFolderRemoteConfig;
            _treeViewType = settings.menuTreeViewType;
        }

        private void EnsureTreeView()
        {
            if (_treeView != null) return;

            _treeViewState ??= new TreeViewState();
            _searchField ??= new SearchField();

            _treeView = new MasterWindowTreeView(_treeViewState, BuildRootFolders(), _treeViewType);
            _treeView.ConfigDoubleClicked += config =>
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            };
            _treeView.FolderContextMenuRequested += ShowFolderContextMenu;
            _treeView.ConfigsContextMenuRequested += configs =>
            {
                if (configs.Length > 1) ShowMultiSelectionContextMenu(configs);
                else if (configs.Length == 1) ShowConfigContextMenu(configs[0]);
            };

            _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;
        }

        private (string, string)[] BuildRootFolders() => new[]
        {
            ("Local", _rootFolderLocal),
            ("Addressable", _rootFolderAddressable),
            ("Resources", _rootFolderResources),
            ("RemoteConfig", _rootFolderRemoteConfig),
        };

        /// <summary>Rebuilds the tree from scratch — call after settings, view type, or on-disk assets change.</summary>
        private void ForceMenuTreeRebuild()
        {
            LoadSettingsFields();
            _treeView = null;
            EnsureTreeView();
            Repaint();
        }

        private void OnSelection()
        {
            if (Selection.activeObject is not GameConfig config) return;

            EnsureTreeView();
            if (_treeView.GetSelectedConfig() == config) return;

            _treeView.SelectConfig(config);
        }

        private void OnGUI()
        {
            EnsureTreeView();

            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            DrawTreePane();
            DrawInspectorPane();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTreePane()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_treeWidth));

            var searchRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            _treeView.searchString = _searchField.OnGUI(searchRect, _treeView.searchString);

            var treeRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _treeView.OnGUI(treeRect);

            EditorGUILayout.EndVertical();

            var dragRect = GUILayoutUtility.GetLastRect();
            HandleTreeWidthDrag(new Rect(dragRect.xMax, dragRect.y, 4f, dragRect.height));
        }

        private void HandleTreeWidthDrag(Rect handleRect)
        {
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);

            var evt = Event.current;
            if (evt.type == EventType.MouseDown && handleRect.Contains(evt.mousePosition))
            {
                GUIUtility.hotControl = handleRect.GetHashCode();
                evt.Use();
            }
            else if (evt.type == EventType.MouseDrag && GUIUtility.hotControl == handleRect.GetHashCode())
            {
                _treeWidth = Mathf.Clamp(_treeWidth + evt.delta.x, 160f, position.width - 200f);
                evt.Use();
                Repaint();
            }
            else if (evt.type == EventType.MouseUp && GUIUtility.hotControl == handleRect.GetHashCode())
            {
                GUIUtility.hotControl = 0;
                evt.Use();
            }
        }

        private void DrawInspectorPane()
        {
            EditorGUILayout.BeginVertical();

            var selected = _treeView.GetSelectedConfig();
            if (selected != _selectedObject)
            {
                _selectedObject = selected;
                if (_selectedEditor != null) DestroyImmediate(_selectedEditor);
                _selectedEditor = selected != null ? UnityEditor.Editor.CreateEditor(selected) : null;
            }

            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
            if (_selectedEditor != null)
            {
                _selectedEditor.OnInspectorGUI();
            }
            else
            {
                EditorGUILayout.HelpBox("Select a config on the left to view/edit it here.", MessageType.Info);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private string GetFolderPath(MasterWindowTreeItem folderItem)
        {
            return folderItem?.FolderPath ?? _rootFolderLocal;
        }

        private static ConfigRoot GetConfigs(ConfigLocation location)
        {
            return ConfigRootEditorSync.GetOrCreateRoot(location);
        }

        private static void UpdateConfigRoot()
        {
            foreach (var location in new[]
                     {
                         ConfigLocation.Resources,
                         ConfigLocation.Addressable,
                         ConfigLocation.RemoteConfig,
                         ConfigLocation.Local
                     })
            {
                ConfigRootEditorSync.RefreshLocation(location);
            }

            ConfigRootRefsEditor.UpdateCache();
        }
    }
}
