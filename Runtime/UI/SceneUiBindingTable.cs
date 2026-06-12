using System.Collections.Generic;
using UnityEngine;

namespace INLO.Core.UI
{
    [CreateAssetMenu(menuName = "INLO/Core/UI/Scene UI Binding Table")]
    public sealed class SceneUiBindingTable : ScriptableObject
    {
        [SerializeField] private List<SceneUiBindingEntry> entries = new();

        public IReadOnlyList<SceneUiBindingEntry> Entries => entries;

        public void Configure(IEnumerable<SceneUiBindingEntry> bindingEntries)
        {
            entries.Clear();
            if (bindingEntries == null)
            {
                return;
            }

            entries.AddRange(bindingEntries);
        }

        public bool TryFind(string gameSceneName, out SceneUiBindingEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(gameSceneName))
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    SceneUiBindingEntry candidate = entries[i];
                    if (candidate != null && candidate.GameSceneName == gameSceneName)
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }
    }
}
