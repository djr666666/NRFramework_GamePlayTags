using System;
using System.Collections.Generic;

namespace NRFramework.GameplayTags
{
    /// <summary>
    /// 策划或开发写在某个标签上的注释。名字是完整标签名。
    /// </summary>
    [Serializable]
    public sealed class GameplayTagNote
    {
        public string Name;
        public string Description;
    }

    /// <summary>
    /// 编辑器里保存的那张名字表。只存亲手登记的名字，父节点仍由登记表自动补上。
    /// 注释可以写在亲手登记的标签上，也可以写在自动补上的父标签上。
    /// </summary>
    public sealed class GameplayTagNameTable
    {
        readonly List<string> _explicitNames = new List<string>();
        readonly List<GameplayTagNote> _notes = new List<GameplayTagNote>();

        public GameplayTagNameTable()
        {
        }

        public GameplayTagNameTable(IEnumerable<string> explicitNames, IEnumerable<GameplayTagNote> notes)
        {
            if (explicitNames != null)
            {
                foreach (string name in explicitNames)
                {
                    if (string.IsNullOrEmpty(name) || Contains(name))
                        continue;
                    _explicitNames.Add(name);
                }
                _explicitNames.Sort(StringComparer.Ordinal);
            }

            if (notes == null)
                return;

            foreach (GameplayTagNote note in notes)
            {
                if (note == null || string.IsNullOrEmpty(note.Name) || string.IsNullOrWhiteSpace(note.Description))
                    continue;
                SetStoredNote(note.Name, note.Description);
            }
        }

        public IReadOnlyList<string> ExplicitNames
        {
            get { return _explicitNames; }
        }

        public IReadOnlyList<GameplayTagNote> Notes
        {
            get { return _notes; }
        }

        public string GetNote(string fullName)
        {
            for (int i = 0; i < _notes.Count; i++)
            {
                if (_notes[i].Name == fullName)
                    return _notes[i].Description ?? string.Empty;
            }
            return string.Empty;
        }

        public bool SetNote(string fullName, string description, out string error)
        {
            if (!TryValidate(fullName, out error))
                return false;

            GameplayTagRegistry registry = CreateRegistry();
            if (!registry.Contains(fullName))
            {
                error = "没有这个标签：" + fullName;
                return false;
            }

            if (string.IsNullOrWhiteSpace(description))
                RemoveStoredNote(fullName);
            else
                SetStoredNote(fullName, description);
            error = null;
            return true;
        }

        public bool TryAdd(string tagName, out string error)
        {
            if (!TryValidate(tagName, out error))
                return false;
            if (Contains(tagName))
            {
                error = "已经登记过：" + tagName;
                return false;
            }

            _explicitNames.Add(tagName);
            _explicitNames.Sort(StringComparer.Ordinal);
            return true;
        }

        /// <summary>
        /// 去掉这个名字，以及挂在它下面、已经登记过的名字。
        /// </summary>
        public bool TryRemove(string fullName, out string error)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                error = "标签名是空的。";
                return false;
            }

            int removed = _explicitNames.RemoveAll(name => IsSameOrUnder(name, fullName));
            if (removed == 0)
            {
                error = "这个名字下面没有登记过的标签：" + fullName;
                return false;
            }

            _notes.RemoveAll(note => note == null || string.IsNullOrEmpty(note.Name) || IsSameOrUnder(note.Name, fullName));
            error = null;
            return true;
        }

        /// <summary>
        /// 改一个节点的全名。挂在它下面的登记名会一起换前缀。
        /// </summary>
        public bool TryRename(string oldFullName, string newFullName, out string error)
        {
            if (string.IsNullOrEmpty(oldFullName))
            {
                error = "标签名是空的。";
                return false;
            }
            if (oldFullName == newFullName)
            {
                error = null;
                return true;
            }
            if (!TryValidate(newFullName, out error))
                return false;

            var next = new List<string>(_explicitNames.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            bool touched = false;
            for (int i = 0; i < _explicitNames.Count; i++)
            {
                string updated = Rewrite(_explicitNames[i], oldFullName, newFullName);
                if (updated != _explicitNames[i])
                    touched = true;
                if (!TryValidate(updated, out error))
                    return false;
                if (!seen.Add(updated))
                {
                    error = "改完会和已有标签重名：" + updated;
                    return false;
                }
                next.Add(updated);
            }

            if (!touched)
            {
                error = "这个名字下面没有登记过的标签：" + oldFullName;
                return false;
            }

            var rewrittenNotes = new List<GameplayTagNote>(_notes.Count);
            var noteNames = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _notes.Count; i++)
            {
                GameplayTagNote note = _notes[i];
                if (note == null || string.IsNullOrEmpty(note.Name) || string.IsNullOrWhiteSpace(note.Description))
                    continue;

                string updatedName = Rewrite(note.Name, oldFullName, newFullName);
                if (!noteNames.Add(updatedName))
                {
                    error = "改完注释会撞在同一个标签上：" + updatedName;
                    return false;
                }
                rewrittenNotes.Add(new GameplayTagNote { Name = updatedName, Description = note.Description });
            }

            next.Sort(StringComparer.Ordinal);
            rewrittenNotes.Sort(CompareNoteName);
            _explicitNames.Clear();
            _explicitNames.AddRange(next);
            _notes.Clear();
            _notes.AddRange(rewrittenNotes);
            error = null;
            return true;
        }

        public GameplayTagRegistry CreateRegistry()
        {
            var registry = new GameplayTagRegistry();
            for (int i = 0; i < _explicitNames.Count; i++)
                registry.Register(_explicitNames[i]);
            registry.Build();
            return registry;
        }

        public static bool TryValidate(string tagName, out string error)
        {
            try
            {
                var registry = new GameplayTagRegistry();
                registry.Register(tagName);
                error = null;
                return true;
            }
            catch (ArgumentException exception)
            {
                error = exception.Message;
                return false;
            }
        }

        void SetStoredNote(string fullName, string description)
        {
            for (int i = 0; i < _notes.Count; i++)
            {
                if (_notes[i].Name != fullName)
                    continue;
                _notes[i].Description = description;
                return;
            }

            _notes.Add(new GameplayTagNote { Name = fullName, Description = description });
            _notes.Sort(CompareNoteName);
        }

        void RemoveStoredNote(string fullName)
        {
            for (int i = _notes.Count - 1; i >= 0; i--)
            {
                if (_notes[i].Name == fullName)
                    _notes.RemoveAt(i);
            }
        }

        static int CompareNoteName(GameplayTagNote left, GameplayTagNote right)
        {
            return string.CompareOrdinal(left.Name, right.Name);
        }

        bool Contains(string tagName)
        {
            for (int i = 0; i < _explicitNames.Count; i++)
            {
                if (_explicitNames[i] == tagName)
                    return true;
            }
            return false;
        }

        static bool IsSameOrUnder(string name, string ancestor)
        {
            if (name == ancestor)
                return true;
            return name.StartsWith(ancestor + ".", StringComparison.Ordinal);
        }

        static string Rewrite(string name, string oldPrefix, string newPrefix)
        {
            if (name == oldPrefix)
                return newPrefix;
            string head = oldPrefix + ".";
            if (name.StartsWith(head, StringComparison.Ordinal))
                return newPrefix + name.Substring(oldPrefix.Length);
            return name;
        }
    }
}
