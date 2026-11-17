using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NRFramework.GameplayTags
{
    /// <summary>
    /// 标签树画成从左到右的节点图。线从父标签连到子标签。
    /// </summary>
    sealed class GameplayTagGraphView : GraphView
    {
        const float CardWidth = 236f;
        const float CardHeight = 108f;
        const float SlotHeight = 124f;
        const float GapX = 88f;
        const float GapY = 28f;

        readonly Dictionary<string, GameplayTagNode> _byName = new Dictionary<string, GameplayTagNode>(StringComparer.Ordinal);
        readonly MiniMap _map;
        readonly VisualElement _overlay;
        readonly Label _empty;
        string _search = string.Empty;
        string _published;
        bool _silent;
        bool _rightDown;
        bool _rightDragged;
        float _rightDistance;

        public event Action<string> SelectionChanged;
        public event Action AddRequested;
        public event Action<string> AddChildRequested;
        public event Action<string> RenameRequested;
        public event Action<string> DeleteRequested;
        public event Action RelayoutRequested;
        public event Action CreateRequested;
        public event Action<string, string> ReparentRequested;
        public event Action<IList<string>> DetachRequested;

        readonly List<KeyValuePair<string, string>> _renames = new List<KeyValuePair<string, string>>();
        int _changeSerial;

        public GameplayTagGraphView()
        {
            style.flexGrow = 1f;
            style.backgroundColor = new Color(0.11f, 0.118f, 0.141f);

            GridBackground grid = new GridBackground();
            grid.AddToClassList("tag-grid");
            StyleSheet sheet = FindSheet();
            if (sheet != null)
                grid.styleSheets.Add(sheet);
            Insert(0, grid);
            grid.StretchToParentSize();

            SetupZoom(0.18f, 1.6f);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ClickSelector());

            deleteSelection = (operationName, askUser) =>
            {
                List<string> wires = SelectedEdgeChildren();
                if (wires.Count > 0)
                {
                    _changeSerial++;
                    Raise(DetachRequested, wires);
                    return;
                }

                string name = CurrentSelectionName();
                if (!string.IsNullOrEmpty(name))
                    Raise(DeleteRequested, name);
            };
            graphViewChanged = OnGraphViewChanged;
            serializeGraphElements = elements => string.Empty;
            canPasteSerializedData = data => false;
            unserializeAndPaste = (operationName, data) => { };

            RegisterCallback<MouseDownEvent>(OnRightDown);
            RegisterCallback<MouseMoveEvent>(OnRightMove);
            RegisterCallback<MouseUpEvent>(OnRightUp, TrickleDown.TrickleDown);

            _map = new MiniMap();
            _map.anchored = true;
            _map.windowed = false;
            _map.maxWidth = 200f;
            _map.maxHeight = 130f;
            Add(_map);

            _overlay = BuildOverlay();
            Add(_overlay);

            _empty = new Label("这张表还是空的。点「添加」，写下第一个标签，例如 State.Control.Frozen。");
            _empty.style.position = Position.Absolute;
            _empty.style.left = 24f;
            _empty.style.top = 24f;
            _empty.style.right = 24f;
            _empty.style.color = new Color(0.75f, 0.76f, 0.8f);
            _empty.style.fontSize = 13;
            _empty.style.whiteSpace = WhiteSpace.Normal;
            _empty.style.display = DisplayStyle.None;
            _empty.pickingMode = PickingMode.Ignore;
            Add(_empty);
        }

        public void SetOverlay(bool visible)
        {
            _overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetSearch(string search)
        {
            _search = search ?? string.Empty;
            ApplySearch();
        }

        public void Select(string fullName)
        {
            GameplayTagNode node;
            if (string.IsNullOrEmpty(fullName) || !_byName.TryGetValue(fullName, out node))
                return;
            _silent = true;
            ClearSelection();
            AddToSelection(node);
            _silent = false;
            _published = fullName;
        }

        public void RememberRename(string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName) || oldName == newName)
                return;
            _renames.Add(new KeyValuePair<string, string>(oldName, newName));
        }

        public void ShowTags(GameplayTagNameTable table, string keep, bool frameAll)
        {
            Dictionary<string, Rect> kept = frameAll ? null : CapturePositions();
            if (frameAll)
                _renames.Clear();

            _silent = true;
            try
            {
                ClearContents();
                if (table != null)
                    Build(table);
                RestoreKept(kept);
                RestoreSelection(keep);
            }
            catch
            {
                ClearContents();
                throw;
            }
            finally
            {
                _silent = false;
            }

            _empty.style.display = table != null && _byName.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            ApplySearch();
            Publish();
            FrameWhenReady(frameAll, keep, 0);
        }

        public void SetNote(string fullName, string note)
        {
            GameplayTagNode node;
            if (string.IsNullOrEmpty(fullName) || !_byName.TryGetValue(fullName, out node))
                return;
            node.SetNote(note);
            ApplySearch();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (IsInsideMiniMap(evt.target as VisualElement))
                return;

            Edge edge = FindEdge(evt.target as VisualElement);
            if (edge != null)
            {
                string childName;
                if (TryChildName(edge, out childName))
                    evt.menu.AppendAction("取消连线", _ => Raise(DetachRequested, new List<string> { childName }));
                return;
            }

            GameplayTagNode node = FindNode(evt.target as VisualElement);
            if (node != null)
            {
                if (evt.menu.MenuItems().Count == 0)
                    FillNodeMenu(evt.menu, node.FullName);
                return;
            }

            evt.menu.AppendAction("添加标签", _ => Raise(AddRequested));
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("整理布局", _ => Raise(RelayoutRequested));
            evt.menu.AppendAction("看全图", _ => FrameAll());
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> result = new List<Port>();
            GameplayTagNode from = startPort == null ? null : startPort.node as GameplayTagNode;
            if (from == null)
                return result;

            foreach (KeyValuePair<string, GameplayTagNode> pair in _byName)
            {
                GameplayTagNode other = pair.Value;
                if (other == from)
                    continue;
                if (IsSameOrUnder(from.FullName, other.FullName) || IsSameOrUnder(other.FullName, from.FullName))
                    continue;

                Port candidate = startPort.direction == Direction.Output ? other.InPort : other.OutPort;
                if (candidate == null)
                    continue;
                result.Add(candidate);
            }
            return result;
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            Publish();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            Publish();
        }

        public override void ClearSelection()
        {
            base.ClearSelection();
            Publish();
        }

        public static string SegmentOf(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return string.Empty;
            int dot = fullName.LastIndexOf('.');
            if (dot < 0)
                return fullName;
            return fullName.Substring(dot + 1);
        }

        public static string ParentOf(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return string.Empty;
            int dot = fullName.LastIndexOf('.');
            if (dot < 0)
                return string.Empty;
            return fullName.Substring(0, dot);
        }

        public static string BranchOf(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return string.Empty;
            int first = fullName.IndexOf('.');
            if (first < 0)
                return fullName;
            int second = fullName.IndexOf('.', first + 1);
            if (second < 0)
                return fullName.Substring(first + 1);
            return fullName.Substring(first + 1, second - first - 1);
        }

        public static Color AccentFor(string fullName)
        {
            switch (BranchOf(fullName))
            {
                case "Ailment": return new Color(0.70f, 0.28f, 0.32f);
                case "Burning": return new Color(0.78f, 0.40f, 0.16f);
                case "Combat": return new Color(0.70f, 0.50f, 0.18f);
                case "Control": return new Color(0.24f, 0.48f, 0.78f);
                case "Guard": return new Color(0.22f, 0.55f, 0.40f);
                case "Move": return new Color(0.46f, 0.36f, 0.74f);
                case "Stealth": return new Color(0.36f, 0.40f, 0.62f);
                case "State": return new Color(0.32f, 0.38f, 0.48f);
            }

            string branch = BranchOf(fullName);
            int hash = 17;
            for (int i = 0; i < branch.Length; i++)
                hash = hash * 31 + branch[i];
            float hue = ((hash & 0x7fffffff) % 360) / 360f;
            return Color.HSVToRGB(hue, 0.48f, 0.62f);
        }

        void Build(GameplayTagNameTable table)
        {
            GameplayTagRegistry registry = table.CreateRegistry();
            List<GameplayTag> tags = new List<GameplayTag>();
            registry.CopyTags(tags);

            Dictionary<string, LayoutNode> map = new Dictionary<string, LayoutNode>(StringComparer.Ordinal);
            List<LayoutNode> roots = new List<LayoutNode>();
            for (int i = 0; i < tags.Count; i++)
            {
                GameplayTag tag = tags[i];
                bool explicitTag = ContainsExplicit(table, tag.Name);
                LayoutNode node = new LayoutNode();
                node.FullName = tag.Name;
                node.Segment = SegmentOf(tag.Name);
                node.Note = table.GetNote(tag.Name);
                node.Explicit = explicitTag;
                node.Accent = AccentFor(tag.Name);
                map.Add(tag.Name, node);
            }

            for (int i = 0; i < tags.Count; i++)
            {
                LayoutNode node = map[tags[i].Name];
                string parentName = ParentOf(node.FullName);
                LayoutNode parent;
                if (parentName.Length == 0)
                    roots.Add(node);
                else if (map.TryGetValue(parentName, out parent))
                    parent.Children.Add(node);
            }

            float y = 36f;
            for (int i = 0; i < roots.Count; i++)
            {
                Measure(roots[i]);
                Place(roots[i], 56f, y);
                y += roots[i].SubtreeHeight + 48f;
            }

            for (int i = 0; i < roots.Count; i++)
                Emit(roots[i]);
            for (int i = 0; i < roots.Count; i++)
                Link(roots[i]);
        }

        void Emit(LayoutNode node)
        {
            GameplayTagNode visual = new GameplayTagNode(
                node.FullName,
                node.Segment,
                node.Note,
                node.Explicit,
                node.Accent);
            visual.SetPosition(new Rect(node.X, node.Y, CardWidth, CardHeight));
            visual.FillMenu = menu => FillNodeMenu(menu, node.FullName);
            visual.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount != 2 || evt.button != (int)MouseButton.LeftMouse)
                    return;
                Raise(RenameRequested, node.FullName);
                evt.StopPropagation();
            });
            _byName.Add(node.FullName, visual);
            AddElement(visual);
            for (int i = 0; i < node.Children.Count; i++)
                Emit(node.Children[i]);
        }

        void Link(LayoutNode node)
        {
            GameplayTagNode parent = _byName[node.FullName];
            for (int i = 0; i < node.Children.Count; i++)
            {
                LayoutNode child = node.Children[i];
                GameplayTagNode other = _byName[child.FullName];
                if (parent.OutPort != null && other.InPort != null)
                {
                    Edge edge = parent.OutPort.ConnectTo(other.InPort);
                    edge.capabilities = Capabilities.Selectable | Capabilities.Deletable;
                    edge.pickingMode = PickingMode.Position;
                    Color wire = Color.Lerp(child.Accent, Color.white, 0.2f);
                    edge.edgeControl.inputColor = wire;
                    edge.edgeControl.outputColor = wire;
                    edge.edgeControl.fromCapColor = wire;
                    edge.edgeControl.toCapColor = wire;
                    AddElement(edge);
                }
                Link(child);
            }
        }

        static void Measure(LayoutNode node)
        {
            if (node.Children.Count == 0)
            {
                node.SubtreeHeight = SlotHeight;
                return;
            }

            float total = 0f;
            for (int i = 0; i < node.Children.Count; i++)
            {
                Measure(node.Children[i]);
                total += node.Children[i].SubtreeHeight;
            }
            total += GapY * (node.Children.Count - 1);
            node.SubtreeHeight = Mathf.Max(SlotHeight, total);
        }

        static void Place(LayoutNode node, float left, float top)
        {
            node.X = left;
            node.Y = top + (node.SubtreeHeight - CardHeight) * 0.5f;
            if (node.Children.Count == 0)
                return;

            float block = 0f;
            for (int i = 0; i < node.Children.Count; i++)
                block += node.Children[i].SubtreeHeight;
            block += GapY * (node.Children.Count - 1);

            float cursor = top + (node.SubtreeHeight - block) * 0.5f;
            float childLeft = left + CardWidth + GapX;
            for (int i = 0; i < node.Children.Count; i++)
            {
                Place(node.Children[i], childLeft, cursor);
                cursor += node.Children[i].SubtreeHeight + GapY;
            }
        }

        void RestoreSelection(string keep)
        {
            ClearSelection();
            GameplayTagNode node;
            if (!string.IsNullOrEmpty(keep) && _byName.TryGetValue(keep, out node))
                AddToSelection(node);
        }

        void ClearContents()
        {
            List<GraphElement> all = new List<GraphElement>();
            foreach (GraphElement element in graphElements)
                all.Add(element);
            if (all.Count > 0)
                DeleteElements(all);
            _byName.Clear();
        }

        void ApplySearch()
        {
            foreach (KeyValuePair<string, GameplayTagNode> pair in _byName)
            {
                bool hit = pair.Value.IsHit(_search);
                pair.Value.style.opacity = hit ? 1f : 0.14f;
            }

            foreach (Edge edge in edges)
            {
                GameplayTagNode from = edge.output == null ? null : edge.output.node as GameplayTagNode;
                GameplayTagNode to = edge.input == null ? null : edge.input.node as GameplayTagNode;
                bool hot = from != null && to != null && from.IsHit(_search) && to.IsHit(_search);
                edge.style.opacity = hot ? 1f : 0.05f;
            }
        }

        void FrameWhenReady(bool frameAll, string reveal, int attempt)
        {
            schedule.Execute(() =>
            {
                if ((float.IsNaN(layout.width) || layout.width < 20f) && attempt < 8)
                {
                    FrameWhenReady(frameAll, reveal, attempt + 1);
                    return;
                }
                if (_byName.Count == 0)
                    return;
                if (frameAll)
                    FrameAll();
                else if (!string.IsNullOrEmpty(reveal))
                    CenterOn(reveal);
            }).ExecuteLater(50);
        }

        void CenterOn(string fullName)
        {
            GameplayTagNode node;
            if (string.IsNullOrEmpty(fullName) || !_byName.TryGetValue(fullName, out node))
                return;
            Rect rect = ReadPosition(node);
            float scale = viewTransform.scale.x;
            if (scale < 0.35f || float.IsNaN(scale))
                scale = 1f;
            float width = layout.width;
            float height = layout.height;
            if (width < 20f || height < 20f)
                return;
            float x = width * 0.5f - (rect.x + rect.width * 0.5f) * scale;
            float y = height * 0.5f - (rect.y + rect.height * 0.5f) * scale;
            UpdateViewTransform(new Vector3(x, y, 0f), new Vector3(scale, scale, 1f));
        }

        void Publish()
        {
            if (_silent)
                return;
            string name = CurrentSelectionName();
            if (name == _published)
                return;
            _published = name;
            Action<string> handler = SelectionChanged;
            if (handler != null)
                handler(name);
        }

        string CurrentSelectionName()
        {
            foreach (ISelectable selected in selection)
            {
                GameplayTagNode node = selected as GameplayTagNode;
                if (node != null)
                    return node.FullName;
            }
            return null;
        }

        GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_silent)
                return change;

            bool creating = change.edgesToCreate != null && change.edgesToCreate.Count > 0;
            if (creating)
            {
                _changeSerial++;
                List<Edge> created = new List<Edge>(change.edgesToCreate);
                change.edgesToCreate.Clear();
                schedule.Execute(() => ApplyDroppedEdges(created));
                return change;
            }

            List<string> detached = TakeRemovedEdges(change.elementsToRemove);
            if (detached.Count > 0)
            {
                int serial = ++_changeSerial;
                schedule.Execute(() =>
                {
                    if (serial != _changeSerial)
                        return;
                    Raise(DetachRequested, detached);
                });
            }
            return change;
        }

        static List<string> TakeRemovedEdges(List<GraphElement> elements)
        {
            List<string> names = new List<string>();
            if (elements == null)
                return names;
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                Edge edge = elements[i] as Edge;
                string childName;
                if (!TryChildName(edge, out childName))
                    continue;
                elements.RemoveAt(i);
                if (!names.Contains(childName))
                    names.Add(childName);
            }
            return names;
        }

        void ApplyDroppedEdges(List<Edge> created)
        {
            for (int i = 0; i < created.Count; i++)
            {
                GameplayTagNode parentNode;
                GameplayTagNode childNode;
                if (!TryReadConnection(created[i], out parentNode, out childNode))
                    continue;
                if (parentNode.FullName == childNode.FullName)
                    continue;
                Raise(ReparentRequested, childNode.FullName, parentNode.FullName);
                return;
            }
        }

        static bool TryReadConnection(Edge edge, out GameplayTagNode parentNode, out GameplayTagNode childNode)
        {
            parentNode = null;
            childNode = null;
            if (edge == null)
                return false;
            ReadPort(edge.output, ref parentNode, ref childNode);
            ReadPort(edge.input, ref parentNode, ref childNode);
            return parentNode != null && childNode != null && parentNode != childNode;
        }

        static void ReadPort(Port port, ref GameplayTagNode parentNode, ref GameplayTagNode childNode)
        {
            if (port == null)
                return;
            GameplayTagNode node = port.node as GameplayTagNode;
            if (node == null)
                return;
            if (port.direction == Direction.Output)
                parentNode = node;
            else
                childNode = node;
        }

        static bool IsSameOrUnder(string name, string ancestor)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(ancestor))
                return false;
            return name == ancestor || name.StartsWith(ancestor + ".", StringComparison.Ordinal);
        }

        static bool TryChildName(Edge edge, out string childName)
        {
            childName = null;
            GameplayTagNode parentNode;
            GameplayTagNode childNode;
            if (!TryReadConnection(edge, out parentNode, out childNode))
                return false;
            childName = childNode.FullName;
            return !string.IsNullOrEmpty(childName);
        }

        List<string> SelectedEdgeChildren()
        {
            List<string> names = new List<string>();
            foreach (ISelectable selected in selection)
            {
                string childName;
                if (!TryChildName(selected as Edge, out childName))
                    continue;
                if (!names.Contains(childName))
                    names.Add(childName);
            }
            return names;
        }

        static Rect ReadPosition(Node node)
        {
            Rect rect = node.GetPosition();
            if (!float.IsNaN(rect.x) && !float.IsNaN(rect.y) && rect.width > 1f)
                return rect;

            Rect laid = node.layout;
            if (!float.IsNaN(laid.x) && !float.IsNaN(laid.y))
                return laid;
            return rect;
        }

        Dictionary<string, Rect> CapturePositions()
        {
            Dictionary<string, Rect> kept = new Dictionary<string, Rect>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, GameplayTagNode> pair in _byName)
                kept[MapRenamed(pair.Key)] = ReadPosition(pair.Value);
            _renames.Clear();
            return kept;
        }

        string MapRenamed(string name)
        {
            string bestOld = null;
            string bestNew = null;
            for (int i = 0; i < _renames.Count; i++)
            {
                string oldPrefix = _renames[i].Key;
                if (name != oldPrefix && !name.StartsWith(oldPrefix + ".", StringComparison.Ordinal))
                    continue;
                if (bestOld != null && oldPrefix.Length <= bestOld.Length)
                    continue;
                bestOld = oldPrefix;
                bestNew = _renames[i].Value;
            }
            if (bestOld == null)
                return name;
            return bestNew + name.Substring(bestOld.Length);
        }

        void RestoreKept(Dictionary<string, Rect> kept)
        {
            if (kept == null)
                return;
            foreach (KeyValuePair<string, GameplayTagNode> pair in _byName)
            {
                Rect rect;
                if (!kept.TryGetValue(pair.Key, out rect))
                    continue;
                pair.Value.SetPosition(rect);
            }
        }

        static Edge FindEdge(VisualElement element)
        {
            while (element != null)
            {
                Edge edge = element as Edge;
                if (edge != null)
                    return edge;
                element = element.parent;
            }
            return null;
        }

        void OnRightDown(MouseDownEvent evt)
        {
            if (evt.button != (int)MouseButton.RightMouse)
                return;
            _rightDown = true;
            _rightDragged = false;
            _rightDistance = 0f;
        }

        void FillNodeMenu(DropdownMenu menu, string fullName)
        {
            menu.AppendAction("添加子标签", _ => Raise(AddChildRequested, fullName));
            menu.AppendAction("重命名", _ => Raise(RenameRequested, fullName));
            menu.AppendAction("删除", _ => Raise(DeleteRequested, fullName));
            menu.AppendSeparator();
            menu.AppendAction("看这个标签", _ => CenterOn(fullName));
        }

        void OnRightMove(MouseMoveEvent evt)
        {
            if (!_rightDown)
                return;
            _rightDistance += evt.mouseDelta.magnitude;
            if (_rightDistance < 4f)
                return;
            if (!_rightDragged)
            {
                _rightDragged = true;
                this.CaptureMouse();
            }
            Vector3 scale = viewTransform.scale;
            Vector3 pos = viewTransform.position;
            UpdateViewTransform(new Vector3(pos.x + evt.mouseDelta.x, pos.y + evt.mouseDelta.y, pos.z), scale);
            evt.StopPropagation();
        }

        void OnRightUp(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.RightMouse)
                return;
            _rightDown = false;
            if (this.HasMouseCapture())
                this.ReleaseMouse();
            if (!_rightDragged)
                return;
            _rightDragged = false;
            evt.StopImmediatePropagation();
        }

        VisualElement BuildOverlay()
        {
            VisualElement overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0f;
            overlay.style.right = 0f;
            overlay.style.top = 0f;
            overlay.style.bottom = 0f;
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;
            overlay.style.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 0.78f);
            overlay.style.display = DisplayStyle.None;

            VisualElement card = new VisualElement();
            card.style.width = 360f;
            card.style.backgroundColor = new Color(0.16f, 0.17f, 0.2f);
            card.style.paddingLeft = 18f;
            card.style.paddingRight = 18f;
            card.style.paddingTop = 16f;
            card.style.paddingBottom = 16f;
            card.style.borderTopLeftRadius = 8f;
            card.style.borderTopRightRadius = 8f;
            card.style.borderBottomLeftRadius = 8f;
            card.style.borderBottomRightRadius = 8f;

            Label title = new Label("还没有标签表");
            title.style.fontSize = 16;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.Add(title);

            Label body = new Label("创建后会放进 NRFramework_GamePlayTags，并带上冰冻、沉默、燃烧这些例子。");
            body.style.whiteSpace = WhiteSpace.Normal;
            body.style.color = new Color(0.74f, 0.75f, 0.78f);
            body.style.marginTop = 6f;
            body.style.marginBottom = 12f;
            card.Add(body);

            Button create = new Button(() => Raise(CreateRequested)) { text = "创建标签表" };
            create.style.height = 26f;
            card.Add(create);
            overlay.Add(card);
            return overlay;
        }

        static void Raise(Action handler)
        {
            if (handler != null)
                handler();
        }

        static void Raise(Action<string> handler, string value)
        {
            if (handler != null)
                handler(value);
        }

        static void Raise(Action<string, string> handler, string first, string second)
        {
            if (handler != null)
                handler(first, second);
        }

        static void Raise(Action<IList<string>> handler, IList<string> values)
        {
            if (handler != null)
                handler(values);
        }

        static GameplayTagNode FindNode(VisualElement element)
        {
            while (element != null)
            {
                GameplayTagNode node = element as GameplayTagNode;
                if (node != null)
                    return node;
                element = element.parent;
            }
            return null;
        }

        static bool IsInsideMiniMap(VisualElement element)
        {
            while (element != null)
            {
                if (element is MiniMap)
                    return true;
                element = element.parent;
            }
            return false;
        }

        static bool ContainsExplicit(GameplayTagNameTable table, string fullName)
        {
            IReadOnlyList<string> names = table.ExplicitNames;
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i] == fullName)
                    return true;
            }
            return false;
        }

        static StyleSheet FindSheet()
        {
            string[] guids = AssetDatabase.FindAssets("GameplayTagGraph t:StyleSheet");
            if (guids == null || guids.Length == 0)
                return null;
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        sealed class LayoutNode
        {
            public string FullName;
            public string Segment;
            public string Note;
            public bool Explicit;
            public Color Accent;
            public float X;
            public float Y;
            public float SubtreeHeight;
            public readonly List<LayoutNode> Children = new List<LayoutNode>();
        }
    }

    sealed class GameplayTagNode : Node
    {
        public readonly string FullName;
        public readonly Port InPort;
        public readonly Port OutPort;
        readonly Label _note;
        string _noteText = string.Empty;

        public GameplayTagNode(string fullName, string segment, string note, bool explicitTag, Color accent)
        {
            FullName = fullName;
            title = segment;
            capabilities = Capabilities.Selectable | Capabilities.Movable | Capabilities.Ascendable | Capabilities.Snappable;

            Color titleColor = explicitTag ? accent : Color.Lerp(accent, new Color(0.12f, 0.12f, 0.14f), 0.42f);
            VisualElement titleBar = titleContainer != null ? titleContainer : this.Q("title");
            if (titleBar != null)
                titleBar.style.backgroundColor = titleColor;

            Label titleLabel = this.Q<Label>("title-label");
            if (titleLabel != null)
            {
                titleLabel.style.color = Color.white;
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLabel.style.fontSize = 13;
            }

            if (titleButtonContainer != null)
                titleButtonContainer.style.display = DisplayStyle.None;
            else
            {
                VisualElement collapse = this.Q("collapse-button");
                if (collapse != null)
                    collapse.style.display = DisplayStyle.None;
            }

            Color body = new Color(0.15f, 0.16f, 0.18f);
            VisualElement border = this.Q("node-border");
            if (border != null)
            {
                border.style.backgroundColor = body;
                border.style.borderLeftWidth = 3f;
                border.style.borderLeftColor = accent;
            }
            VisualElement contents = this.Q("contents");
            if (contents != null)
                contents.style.backgroundColor = body;

            style.width = 236f;
            style.maxWidth = 236f;

            InPort = AddPort(Direction.Input, accent);
            OutPort = AddPort(Direction.Output, accent);

            Label path = MakeLine(fullName, new Color(0.72f, 0.75f, 0.8f));
            extensionContainer.Add(path);

            if (!explicitTag)
            {
                Label kind = MakeLine("自动补上", new Color(0.62f, 0.64f, 0.68f));
                extensionContainer.Add(kind);
            }

            _note = MakeLine(string.Empty, new Color(0.90f, 0.91f, 0.82f));
            extensionContainer.Add(_note);
            extensionContainer.style.backgroundColor = body;
            extensionContainer.style.paddingLeft = 8f;
            extensionContainer.style.paddingRight = 8f;
            extensionContainer.style.paddingTop = 4f;
            extensionContainer.style.paddingBottom = 8f;
            SetNote(note);

            expanded = true;
            RefreshExpandedState();
            RefreshPorts();
        }

        public Action<DropdownMenu> FillMenu;

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (FillMenu != null)
                FillMenu(evt.menu);
        }

        public void SetNote(string note)
        {
            _noteText = string.IsNullOrWhiteSpace(note) ? string.Empty : note.Trim();
            _note.text = Short(_noteText);
            _note.style.display = _noteText.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            tooltip = _noteText.Length == 0 ? FullName : FullName + "\n" + _noteText;
        }

        public bool IsHit(string search)
        {
            if (string.IsNullOrEmpty(search))
                return true;
            if (FullName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return _noteText.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        Port AddPort(Direction direction, Color accent)
        {
            Port port = InstantiatePort(Orientation.Horizontal, direction, direction == Direction.Input ? Port.Capacity.Single : Port.Capacity.Multi, typeof(bool));
            port.portName = string.Empty;
            port.portColor = Color.Lerp(accent, Color.white, 0.45f);
            port.tooltip = direction == Direction.Output
                ? "拖到另一个标签左边的圆点，它就会挂到这个下面"
                : "把父标签右边的圆点拖到这里";
            if (direction == Direction.Input)
                inputContainer.Add(port);
            else
                outputContainer.Add(port);

            Label caption = port.Q<Label>();
            if (caption != null)
                caption.style.display = DisplayStyle.None;

            VisualElement connector = port.Q("connector");
            if (connector != null)
            {
                connector.style.width = 14f;
                connector.style.height = 14f;
                connector.style.backgroundColor = port.portColor;
                connector.style.unityBackgroundImageTintColor = port.portColor;
            }
            return port;
        }

        static Label MakeLine(string text, Color color)
        {
            Label label = new Label(text);
            label.style.fontSize = 11;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.marginTop = 1f;
            label.style.marginBottom = 1f;
            return label;
        }

        static string Short(string note)
        {
            if (string.IsNullOrEmpty(note))
                return string.Empty;
            string line = note.Replace("\r", string.Empty).Replace('\n', ' ');
            if (line.Length <= 28)
                return line;
            return line.Substring(0, 28) + "…";
        }
    }
}
