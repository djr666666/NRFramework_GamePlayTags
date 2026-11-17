using System.Collections.Generic;
using UnityEngine;

namespace NRFramework.GameplayTags
{
    /// <summary>
    /// 标签编辑器存盘用的资源。里面只有亲手登记的名字。
    /// </summary>
    [CreateAssetMenu(fileName = "GameplayTagCatalog", menuName = "NRFramework/Gameplay Tag Catalog")]
    public sealed class GameplayTagCatalog : ScriptableObject
    {
        [SerializeField] List<string> _explicitNames = new List<string>();
        [SerializeField] List<GameplayTagNote> _notes = new List<GameplayTagNote>();

        public IReadOnlyList<string> ExplicitNames
        {
            get { return _explicitNames; }
        }

        public int NoteCount
        {
            get { return _notes == null ? 0 : _notes.Count; }
        }

        public GameplayTagNameTable CopyTable()
        {
            return new GameplayTagNameTable(_explicitNames, _notes);
        }

        public void ReplaceWith(GameplayTagNameTable table)
        {
            _explicitNames.Clear();
            if (_notes == null)
                _notes = new List<GameplayTagNote>();
            _notes.Clear();
            if (table == null)
                return;

            IReadOnlyList<string> names = table.ExplicitNames;
            for (int i = 0; i < names.Count; i++)
                _explicitNames.Add(names[i]);

            IReadOnlyList<GameplayTagNote> notes = table.Notes;
            for (int i = 0; i < notes.Count; i++)
            {
                GameplayTagNote note = notes[i];
                if (note == null || string.IsNullOrEmpty(note.Name) || string.IsNullOrWhiteSpace(note.Description))
                    continue;
                _notes.Add(new GameplayTagNote { Name = note.Name, Description = note.Description });
            }
        }
    }
}
