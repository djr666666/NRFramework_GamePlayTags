using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NRFramework.GameplayTags
{
    /// <summary>
    /// 标签图。中间是节点，右边改名字和注释。
    /// </summary>
    public sealed class GameplayTagEditorWindow : EditorWindow
    {
        enum EditMode
        {
            None,
            Add,
            AddChild,
            Rename,
        }

        const string CatalogPath = "Assets/GameplayTags/GameplayTagCatalog.asset";

        [SerializeField] GameplayTagCatalog _catalog;

        GameplayTagGraphView _graph;
        ObjectField _catalogField;
        TextField _searchField;
        TextField _draftField;
        TextField _pathField;
        TextField _noteField;
        Label _countLabel;
        Label _kindLabel;
        Label _registeredLabel;
        Label _parentLabel;
        Label _underLabel;
        Label _promptLabel;
        Label _previewLabel;
        Label _placeholder;
        VisualElement _detail;
        VisualElement _editBox;
        VisualElement _accent;
        VisualElement _legend;
        HelpBox _error;
        Label _syncLabel;
        Button _addButton;
        Button _addChildButton;
        Button _renameButton;
        Button _deleteButton;
        Button _reloadButton;
        Button _layoutButton;
        Button _frameButton;
        EditMode _mode;
        string _draft = string.Empty;
        string _selected;
        bool _uiReady;
        bool _suppressDraft;
        bool _suppressNote;

        [MenuItem("NRFramework/Gameplay Tags/标签编辑器", false, 0)]
        public static void Open()
        {
            Open(FindCatalog());
        }

        public static void Open(GameplayTagCatalog catalog)
        {
            GameplayTagEditorWindow window = GetWindow<GameplayTagEditorWindow>();
            window.titleContent = new GUIContent("标签图");
            window.minSize = new Vector2(980f, 640f);
            window._catalog = catalog;
            window.Show();
            window.BindCatalog();
        }

        void OnEnable()
        {
            titleContent = new GUIContent("标签图");
            minSize = new Vector2(980f, 640f);
            if (_catalog == null)
                _catalog = FindCatalog();
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();
            root.style.backgroundColor = new Color(0.11f, 0.118f, 0.141f);
            root.Add(BuildToolbar());

            VisualElement body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1f;
            body.style.flexShrink = 1f;
            body.style.minHeight = 0f;
            root.Add(body);

            _graph = new GameplayTagGraphView();
            _graph.style.flexGrow = 1f;
            _graph.style.flexShrink = 1f;
            _graph.style.flexBasis = 0f;
            _graph.style.minWidth = 0f;
            _graph.SelectionChanged += OnGraphSelection;
            _graph.AddRequested += () => BeginEdit(EditMode.Add, null, string.Empty);
            _graph.AddChildRequested += fullName => BeginEdit(EditMode.AddChild, fullName, string.Empty);
            _graph.RenameRequested += fullName => BeginEdit(EditMode.Rename, fullName, GameplayTagGraphView.SegmentOf(fullName));
            _graph.DeleteRequested += DeleteByName;
            _graph.RelayoutRequested += () => ReloadGraph(true);
            _graph.CreateRequested += CreateCatalog;
            _graph.ReparentRequested += Reparent;
            _graph.DetachRequested += Detach;
            body.Add(_graph);
            body.Add(BuildSide());

            _uiReady = true;
            BindCatalog();
        }

        void BindCatalog()
        {
            if (!_uiReady)
                return;
            if (_catalogField != null)
                _catalogField.SetValueWithoutNotify(_catalog);
            CancelDraft();
            ReloadGraph(true);
        }

        VisualElement BuildToolbar()
        {
            VisualElement bar = new VisualElement();
            bar.style.flexShrink = 0f;
            bar.style.backgroundColor = new Color(0.16f, 0.165f, 0.19f);
            bar.style.borderBottomWidth = 1f;
            bar.style.borderBottomColor = new Color(0.08f, 0.08f, 0.09f);
            bar.style.paddingLeft = 8f;
            bar.style.paddingRight = 8f;
            bar.style.paddingTop = 6f;
            bar.style.paddingBottom = 6f;

            VisualElement top = Row();
            _catalogField = new ObjectField("标签表");
            _catalogField.objectType = typeof(GameplayTagCatalog);
            _catalogField.allowSceneObjects = false;
            _catalogField.SetValueWithoutNotify(_catalog);
            _catalogField.style.flexGrow = 1f;
            _catalogField.style.marginRight = 8f;
            _catalogField.RegisterValueChangedCallback(evt =>
            {
                _catalog = evt.newValue as GameplayTagCatalog;
                CancelDraft();
                ReloadGraph(true);
            });
            top.Add(_catalogField);

            Label searchLabel = MutedLabel("搜索");
            searchLabel.style.marginRight = 4f;
            searchLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            top.Add(searchLabel);
            _searchField = new TextField();
            _searchField.style.width = 180f;
            _searchField.RegisterValueChangedCallback(evt =>
            {
                if (_graph != null)
                    _graph.SetSearch(evt.newValue);
            });
            top.Add(_searchField);
            bar.Add(top);

            VisualElement actions = Row();
            actions.style.marginTop = 4f;
            actions.style.flexWrap = Wrap.Wrap;
            _addButton = MakeButton("添加", "写一个完整名字，例如 State.Control.Frozen", () => BeginEdit(EditMode.Add, null, string.Empty));
            _addChildButton = MakeButton("添加子标签", "加在当前选中的标签下面", () => BeginEdit(EditMode.AddChild, _selected, string.Empty));
            _renameButton = MakeButton("重命名", "只改这一段，下面的名字会跟着换", () => BeginEdit(EditMode.Rename, _selected, GameplayTagGraphView.SegmentOf(_selected)));
            _deleteButton = MakeButton("删除", "去掉这一支，以及下面登记过的标签", () => DeleteByName(_selected));
            _reloadButton = MakeButton("保存配置", "导出代码。之后直接写 Tags.State.Control.Frozen，调用时不再查表。", ReloadCode);
            _reloadButton.style.backgroundColor = new Color(0.16f, 0.55f, 0.30f);
            _reloadButton.style.unityBackgroundImageTintColor = new Color(0.16f, 0.55f, 0.30f);
            _reloadButton.style.color = Color.white;
            _reloadButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            _layoutButton = MakeButton("整理布局", "把拖开的节点按父子关系重新排好", () => ReloadGraph(true));
            _frameButton = MakeButton("看全图", "缩放到能看见全部标签", () =>
            {
                if (_graph != null)
                    _graph.FrameAll();
            });
            actions.Add(_addButton);
            actions.Add(_addChildButton);
            actions.Add(_renameButton);
            actions.Add(_deleteButton);
            actions.Add(_reloadButton);
            actions.Add(FlexibleSpace());
            _countLabel = MutedLabel(string.Empty);
            _countLabel.style.marginRight = 8f;
            _countLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            actions.Add(_countLabel);
            _syncLabel = MutedLabel("改完再点保存配置");
            _syncLabel.style.marginRight = 8f;
            actions.Add(_syncLabel);
            actions.Add(_layoutButton);
            actions.Add(_frameButton);
            bar.Add(actions);
            return bar;
        }

        VisualElement BuildSide()
        {
            VisualElement side = new VisualElement();
            side.style.width = 320f;
            side.style.flexShrink = 0f;
            side.style.backgroundColor = new Color(0.145f, 0.145f, 0.16f);
            side.style.borderLeftWidth = 1f;
            side.style.borderLeftColor = new Color(0.08f, 0.08f, 0.09f);
            side.style.paddingLeft = 10f;
            side.style.paddingRight = 10f;
            side.style.paddingTop = 8f;
            side.style.paddingBottom = 8f;

            _editBox = BuildEditBox();
            side.Add(_editBox);

            ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            scroll.style.minHeight = 0f;
            _placeholder = new Label("在图里点一个标签。\n从右边的圆点拖到另一个标签左边的圆点，它就会变成子标签。");
            _placeholder.style.whiteSpace = WhiteSpace.Normal;
            _placeholder.style.color = new Color(0.62f, 0.63f, 0.66f);
            _placeholder.style.fontSize = 12;
            scroll.Add(_placeholder);

            _detail = new VisualElement();
            _detail.style.display = DisplayStyle.None;
            _accent = new VisualElement();
            _accent.style.height = 4f;
            _accent.style.marginBottom = 8f;
            _accent.style.borderTopLeftRadius = 2f;
            _accent.style.borderTopRightRadius = 2f;
            _detail.Add(_accent);

            _kindLabel = new Label();
            _kindLabel.style.fontSize = 18;
            _kindLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _kindLabel.style.color = Color.white;
            _detail.Add(_kindLabel);

            _registeredLabel = MutedLabel(string.Empty);
            _registeredLabel.style.marginBottom = 8f;
            _detail.Add(_registeredLabel);

            _detail.Add(MutedLabel("完整名字"));
            _pathField = new TextField();
            _pathField.isReadOnly = true;
            _pathField.style.marginBottom = 6f;
            _detail.Add(_pathField);

            _parentLabel = new Label();
            _parentLabel.style.marginBottom = 2f;
            _parentLabel.style.whiteSpace = WhiteSpace.Normal;
            _detail.Add(_parentLabel);

            _underLabel = new Label();
            _underLabel.style.marginBottom = 8f;
            _detail.Add(_underLabel);

            _detail.Add(MutedLabel("注释"));
            _noteField = new TextField();
            _noteField.multiline = true;
            _noteField.style.minHeight = 96f;
            _noteField.style.whiteSpace = WhiteSpace.Normal;
            _noteField.RegisterValueChangedCallback(evt =>
            {
                if (_suppressNote)
                    return;
                SaveNote(evt.newValue);
            });
            _detail.Add(_noteField);
            scroll.Add(_detail);
            side.Add(scroll);

            _error = new HelpBox(string.Empty, HelpBoxMessageType.Error);
            _error.style.display = DisplayStyle.None;
            _error.style.marginTop = 6f;
            side.Add(_error);

            _legend = new VisualElement();
            _legend.style.flexDirection = FlexDirection.Row;
            _legend.style.flexWrap = Wrap.Wrap;
            _legend.style.marginTop = 8f;
            side.Add(_legend);

            Label hint = MutedLabel("点一条线，按 Delete 或右键可以取消。点「保存配置」后，代码里直接写 Tags.State.Control.Frozen。");
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.marginTop = 8f;
            hint.style.fontSize = 11;
            side.Add(hint);
            return side;
        }

        VisualElement BuildEditBox()
        {
            VisualElement box = new VisualElement();
            box.style.display = DisplayStyle.None;
            box.style.backgroundColor = new Color(0.18f, 0.19f, 0.22f);
            box.style.paddingLeft = 8f;
            box.style.paddingRight = 8f;
            box.style.paddingTop = 8f;
            box.style.paddingBottom = 8f;
            box.style.marginBottom = 8f;
            box.style.borderTopLeftRadius = 6f;
            box.style.borderTopRightRadius = 6f;
            box.style.borderBottomLeftRadius = 6f;
            box.style.borderBottomRightRadius = 6f;

            _promptLabel = new Label();
            _promptLabel.style.whiteSpace = WhiteSpace.Normal;
            _promptLabel.style.color = new Color(0.86f, 0.87f, 0.9f);
            _promptLabel.style.marginBottom = 4f;
            box.Add(_promptLabel);

            _draftField = new TextField();
            _draftField.RegisterValueChangedCallback(evt =>
            {
                if (_suppressDraft)
                    return;
                _draft = evt.newValue;
                RefreshPreview();
            });
            _draftField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    CommitDraft();
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    CancelDraft();
                    evt.StopPropagation();
                }
            });
            box.Add(_draftField);

            _previewLabel = new Label();
            _previewLabel.style.marginTop = 4f;
            _previewLabel.style.marginBottom = 6f;
            _previewLabel.style.whiteSpace = WhiteSpace.Normal;
            _previewLabel.style.color = new Color(0.72f, 0.78f, 0.88f);
            box.Add(_previewLabel);

            VisualElement row = Row();
            Button ok = MakeButton("确定", null, CommitDraft);
            Button cancel = MakeButton("取消", null, CancelDraft);
            row.Add(ok);
            row.Add(cancel);
            box.Add(row);
            return box;
        }

        void OnGraphSelection(string fullName)
        {
            bool changed = _selected != fullName;
            _selected = fullName;
            if (changed && _mode != EditMode.None)
                CancelDraft();
            RefreshDetail();
            RefreshButtons();
        }

        void ReloadGraph(bool frameAll)
        {
            RefreshCount();
            RefreshLegend();
            if (_graph == null)
                return;
            _graph.SetOverlay(_catalog == null);
            if (_catalog == null)
            {
                _graph.ShowTags(null, null, false);
                RefreshDetail();
                RefreshButtons();
                return;
            }

            try
            {
                _graph.ShowTags(_catalog.CopyTable(), _selected, frameAll);
                ClearError();
            }
            catch (Exception exception)
            {
                ShowError(exception.Message);
            }
            RefreshDetail();
            RefreshButtons();
        }

        void RefreshDetail()
        {
            bool has = _catalog != null && !string.IsNullOrEmpty(_selected);
            _placeholder.style.display = has ? DisplayStyle.None : DisplayStyle.Flex;
            _detail.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            if (!has)
                return;

            Color accent = GameplayTagGraphView.AccentFor(_selected);
            _accent.style.backgroundColor = accent;
            _kindLabel.text = GameplayTagGraphView.SegmentOf(_selected);
            _kindLabel.style.color = Color.white;
            bool explicitTag = IsExplicit(_selected);
            _registeredLabel.text = explicitTag ? "你登记的标签" : "自动补上的父标签";
            _pathField.SetValueWithoutNotify(_selected);
            string parent = GameplayTagGraphView.ParentOf(_selected);
            _parentLabel.text = "父标签    " + (string.IsNullOrEmpty(parent) ? "无" : parent);
            _underLabel.text = "下面登记了 " + CountUnder(_selected) + " 个";

            _suppressNote = true;
            _noteField.SetValueWithoutNotify(_catalog.CopyTable().GetNote(_selected));
            _suppressNote = false;
        }

        void RefreshCount()
        {
            if (_countLabel == null)
                return;
            _countLabel.text = _catalog == null ? "未指定标签表" : _catalog.ExplicitNames.Count + " 个已登记";
        }

        void RefreshLegend()
        {
            if (_legend == null)
                return;
            _legend.Clear();
            if (_catalog == null)
                return;

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<string> names = _catalog.ExplicitNames;
            for (int i = 0; i < names.Count; i++)
            {
                string branch = GameplayTagGraphView.BranchOf(names[i]);
                if (string.IsNullOrEmpty(branch) || !seen.Add(branch))
                    continue;

                VisualElement chip = new VisualElement();
                chip.style.flexDirection = FlexDirection.Row;
                chip.style.alignItems = Align.Center;
                chip.style.marginRight = 10f;
                chip.style.marginBottom = 4f;

                VisualElement swatch = new VisualElement();
                swatch.style.width = 8f;
                swatch.style.height = 8f;
                swatch.style.marginRight = 4f;
                swatch.style.backgroundColor = GameplayTagGraphView.AccentFor(branch);
                swatch.style.borderTopLeftRadius = 2f;
                swatch.style.borderTopRightRadius = 2f;
                swatch.style.borderBottomLeftRadius = 2f;
                swatch.style.borderBottomRightRadius = 2f;

                Label label = MutedLabel(branch);
                chip.Add(swatch);
                chip.Add(label);
                _legend.Add(chip);
            }
        }

        void RefreshButtons()
        {
            if (_addButton == null)
                return;
            bool hasCatalog = _catalog != null;
            bool hasSelection = hasCatalog && !string.IsNullOrEmpty(_selected);
            bool editing = _mode != EditMode.None;
            _addButton.SetEnabled(hasCatalog && !editing);
            _addChildButton.SetEnabled(hasSelection && !editing);
            _renameButton.SetEnabled(hasSelection && !editing);
            _deleteButton.SetEnabled(hasSelection && !editing);
            _layoutButton.SetEnabled(hasCatalog && !editing);
            _frameButton.SetEnabled(hasCatalog);
            _reloadButton.SetEnabled(hasCatalog && !editing);
        }

        void BeginEdit(EditMode mode, string anchor, string draft)
        {
            if (_catalog == null)
                return;
            if (_mode != EditMode.None)
                CancelDraft();
            if (mode != EditMode.Add && string.IsNullOrEmpty(anchor))
                return;
            if (!string.IsNullOrEmpty(anchor))
            {
                _selected = anchor;
                if (_graph != null)
                    _graph.Select(anchor);
                RefreshDetail();
            }

            _mode = mode;
            _draft = draft ?? string.Empty;
            if (_mode == EditMode.Add)
                _promptLabel.text = "完整名字，例如 State.Control.Frozen";
            else if (_mode == EditMode.AddChild)
                _promptLabel.text = "加在「" + _selected + "」下面，只写这一段";
            else
                _promptLabel.text = "新的这一段名字。下面的标签会跟着改";

            _suppressDraft = true;
            _draftField.value = _draft;
            _suppressDraft = false;
            _editBox.style.display = DisplayStyle.Flex;
            ClearError();
            RefreshPreview();
            RefreshButtons();
            _draftField.schedule.Execute(() => _draftField.Focus()).ExecuteLater(30);
        }

        void CancelDraft()
        {
            _mode = EditMode.None;
            _draft = string.Empty;
            if (_editBox != null)
                _editBox.style.display = DisplayStyle.None;
            RefreshButtons();
        }

        void RefreshPreview()
        {
            if (_previewLabel == null)
                return;
            string preview = Preview();
            _previewLabel.text = string.IsNullOrEmpty(preview) ? "结果    …" : "结果    " + preview;
        }

        void CommitDraft()
        {
            if (_catalog == null || _mode == EditMode.None)
                return;

            string fullName = Preview();
            GameplayTagNameTable table = _catalog.CopyTable();
            string error;
            bool ok;
            string undoName;
            if (_mode == EditMode.Rename)
            {
                ok = table.TryRename(_selected, fullName, out error);
                undoName = "重命名标签";
            }
            else
            {
                ok = table.TryAdd(fullName, out error);
                undoName = "添加标签";
            }

            if (!ok)
            {
                ShowError(error);
                return;
            }

            Apply(table, undoName);
            bool renamed = _mode == EditMode.Rename;
            string previous = _selected;
            _selected = fullName;
            if (renamed && _graph != null)
                _graph.RememberRename(previous, fullName);
            CancelDraft();
            ReloadGraph(false);
        }

        void DeleteByName(string fullName)
        {
            if (_catalog == null || string.IsNullOrEmpty(fullName))
                return;

            _selected = fullName;
            if (_graph != null)
                _graph.Select(fullName);
            RefreshDetail();

            bool agreed = EditorUtility.DisplayDialog(
                "删除标签",
                "去掉「" + fullName + "」以及它下面登记过的标签？",
                "删除",
                "取消");
            if (!agreed)
                return;

            GameplayTagNameTable table = _catalog.CopyTable();
            string error;
            if (!table.TryRemove(fullName, out error))
            {
                ShowError(error);
                return;
            }

            Apply(table, "删除标签");
            _selected = null;
            CancelDraft();
            ReloadGraph(false);
        }

        void Reparent(string childName, string parentName)
        {
            if (_catalog == null || string.IsNullOrEmpty(childName) || string.IsNullOrEmpty(parentName))
                return;
            if (childName == parentName || parentName.StartsWith(childName + ".", StringComparison.Ordinal))
            {
                ShowError("不能把标签连到自己或自己的子标签上。");
                ReloadGraph(false);
                return;
            }

            string newName = parentName + "." + GameplayTagGraphView.SegmentOf(childName);
            if (newName == childName)
            {
                ReloadGraph(false);
                return;
            }

            GameplayTagNameTable table = _catalog.CopyTable();
            string error;
            if (!table.TryRename(childName, newName, out error))
            {
                ShowError(error);
                ReloadGraph(false);
                return;
            }

            Apply(table, "连到父标签");
            if (_graph != null)
                _graph.RememberRename(childName, newName);
            _selected = newName;
            ClearError();
            ReloadGraph(false);
        }

        void Detach(IList<string> childNames)
        {
            if (_catalog == null || childNames == null || childNames.Count == 0)
                return;

            GameplayTagNameTable table = _catalog.CopyTable();
            List<KeyValuePair<string, string>> planned = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < childNames.Count; i++)
            {
                string childName = childNames[i];
                string segment = GameplayTagGraphView.SegmentOf(childName);
                if (string.IsNullOrEmpty(segment) || segment == childName)
                    continue;

                string error;
                if (!table.TryRename(childName, segment, out error))
                {
                    ShowError(error);
                    ReloadGraph(false);
                    return;
                }
                planned.Add(new KeyValuePair<string, string>(childName, segment));
            }

            if (planned.Count == 0)
            {
                ReloadGraph(false);
                return;
            }

            Apply(table, "取消连线");
            if (_graph != null)
            {
                for (int i = 0; i < planned.Count; i++)
                    _graph.RememberRename(planned[i].Key, planned[i].Value);
            }
            _selected = planned[planned.Count - 1].Value;
            ClearError();
            ReloadGraph(false);
        }

        void ReloadCode()
        {
            if (_catalog == null)
                return;

            try
            {
                GameplayTagScriptGenerator.Generate(_catalog);
                if (_syncLabel != null)
                    _syncLabel.text = "已导出，可以直接调用";
                ClearError();
            }
            catch (Exception exception)
            {
                ShowError(exception.Message);
            }
        }

        void SaveNote(string text)
        {
            if (_catalog == null || string.IsNullOrEmpty(_selected))
                return;

            GameplayTagNameTable table = _catalog.CopyTable();
            string error;
            if (!table.SetNote(_selected, text, out error))
            {
                ShowError(error);
                return;
            }

            ClearError();
            Apply(table, "标签注释");
            if (_graph != null)
                _graph.SetNote(_selected, text);
        }

        string Preview()
        {
            string segment = (_draft ?? string.Empty).Trim();
            if (_mode == EditMode.Add)
                return segment;
            string selected = _selected ?? string.Empty;
            if (_mode == EditMode.AddChild)
                return string.IsNullOrEmpty(selected) ? segment : selected + "." + segment;
            string parent = GameplayTagGraphView.ParentOf(selected);
            return string.IsNullOrEmpty(parent) ? segment : parent + "." + segment;
        }

        int CountUnder(string fullName)
        {
            if (_catalog == null || string.IsNullOrEmpty(fullName))
                return 0;
            IReadOnlyList<string> names = _catalog.ExplicitNames;
            int count = 0;
            string head = fullName + ".";
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i] == fullName || names[i].StartsWith(head, StringComparison.Ordinal))
                    count++;
            }
            return count;
        }

        bool IsExplicit(string fullName)
        {
            IReadOnlyList<string> names = _catalog.ExplicitNames;
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i] == fullName)
                    return true;
            }
            return false;
        }

        void Apply(GameplayTagNameTable table, string undoName)
        {
            Undo.RecordObject(_catalog, undoName);
            _catalog.ReplaceWith(table);
            EditorUtility.SetDirty(_catalog);
            if (_syncLabel != null)
                _syncLabel.text = "有改动，还没保存";
        }

        void ShowError(string message)
        {
            if (_error == null)
                return;
            _error.text = message ?? string.Empty;
            _error.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void ClearError()
        {
            ShowError(null);
        }

        static VisualElement Row()
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            return row;
        }

        static VisualElement FlexibleSpace()
        {
            VisualElement space = new VisualElement();
            space.style.flexGrow = 1f;
            return space;
        }

        static Label MutedLabel(string text)
        {
            Label label = new Label(text);
            label.style.color = new Color(0.62f, 0.63f, 0.66f);
            label.style.fontSize = 11;
            return label;
        }

        static Button MakeButton(string text, string tip, Action click)
        {
            Button button = new Button(click) { text = text };
            button.tooltip = tip;
            button.style.height = 32f;
            button.style.fontSize = 14;
            button.style.marginRight = 6f;
            button.style.marginBottom = 2f;
            button.style.paddingLeft = 14f;
            button.style.paddingRight = 14f;
            return button;
        }

        static void SeedTemplate(GameplayTagNameTable table)
        {
            string[] names =
            {
                "State.Ailment.Bleed",
                "State.Ailment.Poison",
                "State.Burning",
                "State.Combat.Airborne",
                "State.Combat.Casting",
                "State.Combat.Dead",
                "State.Combat.Stagger",
                "State.Control.Blind",
                "State.Control.Fear",
                "State.Control.Frozen",
                "State.Control.Rooted",
                "State.Control.Silenced",
                "State.Control.Sleep",
                "State.Control.Stun",
                "State.Control.Taunt",
                "State.Guard.Invulnerable",
                "State.Guard.Shield",
                "State.Guard.SuperArmor",
                "State.Move.Haste",
                "State.Move.Slow",
                "State.Stealth.Invisible",
                "State.Stealth.Untargetable",
            };
            string error;
            for (int i = 0; i < names.Length; i++)
            {
                if (!table.TryAdd(names[i], out error))
                    throw new InvalidOperationException(error);
            }

            string[] notes =
            {
                "State", "全游戏共用的状态名。玩家、怪物、装备都用这一份。",
                "State.Ailment", "持续伤害。中毒、流血都算。",
                "State.Ailment.Bleed", "流血。持续掉血。",
                "State.Ailment.Poison", "中毒。持续掉血。",
                "State.Burning", "燃烧。谁身上有，谁就在烧。",
                "State.Combat", "战斗过程里的动作状态。",
                "State.Combat.Airborne", "浮空。人在空中。",
                "State.Combat.Casting", "施法中。这一下还没放完。",
                "State.Combat.Dead", "死亡。策划约定：不能行动，也不能被选为目标。",
                "State.Combat.Stagger", "硬直。这一下被打断。",
                "State.Control", "控制。下面任何一条在，问控制都算有。",
                "State.Control.Blind", "致盲。攻击更容易落空。",
                "State.Control.Fear", "恐惧。不受控制地远离来源。",
                "State.Control.Frozen", "冰冻，不能行动。",
                "State.Control.Rooted", "定身。不能移动，可以攻击和放技能。",
                "State.Control.Silenced", "沉默，不能放技能。",
                "State.Control.Sleep", "睡眠。不能行动，受到伤害会醒来。",
                "State.Control.Stun", "击晕，不能行动。",
                "State.Control.Taunt", "嘲讽。只能攻击嘲讽自己的那个目标。",
                "State.Guard", "防护。护盾、无敌、霸体都算。",
                "State.Guard.Invulnerable", "无敌。不受伤害。",
                "State.Guard.Shield", "护盾。先扣护盾，再扣生命。",
                "State.Guard.SuperArmor", "霸体。策划约定：这段时间不吃硬直和控制。",
                "State.Move", "移动速度。加速和减速都算。",
                "State.Move.Haste", "加速。移动和攻击更快。",
                "State.Move.Slow", "减速。移动变慢。",
                "State.Stealth", "难以被看到或被选中。",
                "State.Stealth.Invisible", "隐身。敌人默认看不见。",
                "State.Stealth.Untargetable", "不可选中。技能不能拿它当目标。",
            };
            for (int i = 0; i < notes.Length; i += 2)
            {
                if (!table.SetNote(notes[i], notes[i + 1], out error))
                    throw new InvalidOperationException(error);
            }
        }

        void CreateCatalog()
        {
            GameplayTagCatalog existing = FindCatalog();
            if (existing != null)
            {
                _catalog = existing;
                if (_catalogField != null)
                    _catalogField.SetValueWithoutNotify(_catalog);
                ReloadGraph(true);
                return;
            }

            GameplayTagNameTable table = new GameplayTagNameTable();
            SeedTemplate(table);

            if (!AssetDatabase.IsValidFolder("Assets/GameplayTags"))
                AssetDatabase.CreateFolder("Assets", "GameplayTags");
            GameplayTagCatalog catalog = CreateInstance<GameplayTagCatalog>();
            catalog.ReplaceWith(table);
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            _catalog = catalog;
            if (_catalogField != null)
                _catalogField.SetValueWithoutNotify(_catalog);
            ReloadGraph(true);
        }

        static GameplayTagCatalog FindCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameplayTagCatalog");
            if (guids == null || guids.Length == 0)
                return null;

            GameplayTagCatalog packaged = null;
            string packagedPath = null;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.StartsWith("Packages/"))
                {
                    packaged = AssetDatabase.LoadAssetAtPath<GameplayTagCatalog>(path);
                    packagedPath = path;
                    continue;
                }
                return AssetDatabase.LoadAssetAtPath<GameplayTagCatalog>(path);
            }

            if (packaged == null)
                return null;

            if (!AssetDatabase.IsValidFolder("Assets/GameplayTags"))
                AssetDatabase.CreateFolder("Assets", "GameplayTags");
            const string localPath = "Assets/GameplayTags/GameplayTagCatalog.asset";
            if (AssetDatabase.LoadAssetAtPath<GameplayTagCatalog>(localPath) == null)
                AssetDatabase.CopyAsset(packagedPath, localPath);
            return AssetDatabase.LoadAssetAtPath<GameplayTagCatalog>(localPath);
        }
    }
}
