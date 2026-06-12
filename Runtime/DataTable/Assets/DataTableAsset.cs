using System.Collections.Generic;
using UnityEngine;

namespace INLO.Core.DataTable
{
    public abstract class DataTableAsset : ScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField]
        private string editorSourcePath;

        [SerializeField]
        private string editorSheetName;

        [SerializeField]
        private string editorSourceKind;

        [SerializeField]
        private string editorLastImportUtc;

        [SerializeField]
        private string editorLastImportStatus;

        [SerializeField]
        private string editorLastImportMessage;

        public string EditorSourcePath => editorSourcePath;
        public string EditorSheetName => editorSheetName;
        public string EditorSourceKind => editorSourceKind;
        public string EditorLastImportUtc => editorLastImportUtc;
        public string EditorLastImportStatus => editorLastImportStatus;
        public string EditorLastImportMessage => editorLastImportMessage;

        public void Editor_SetSourcePath(string sourcePath)
        {
            editorSourcePath = sourcePath;
        }

        public void Editor_SetSheetName(string sheetName)
        {
            editorSheetName = sheetName;
        }

        public void Editor_SetSourceKind(string sourceKind)
        {
            editorSourceKind = sourceKind;
        }

        public void Editor_SetLastImportResult(
            string utcTime,
            string status,
            string message)
        {
            editorLastImportUtc = utcTime;
            editorLastImportStatus = status;
            editorLastImportMessage = message;
        }
#endif

        public abstract int Count { get; }
        public abstract bool IsCacheBuilt { get; }

        public abstract void BuildCache();
        public abstract void ClearCache();
    }

    public abstract class DataTableAsset<TRow> : DataTableAsset
        where TRow : class, IDataTableRow
    {
        [SerializeField]
        private List<TRow> rows = new();

        private Dictionary<string, TRow> cache;

        public IReadOnlyList<TRow> Rows => rows;

        public IReadOnlyDictionary<string, TRow> Lookup
        {
            get
            {
                BuildCache();
                return cache;
            }
        }

        public override int Count => rows.Count;

        public override bool IsCacheBuilt => cache != null;

        public override void BuildCache()
        {
            if (cache != null)
                return;

            cache = new Dictionary<string, TRow>(rows.Count);

            for (int i = 0; i < rows.Count; i++)
            {
                TRow row = rows[i];

                if (row == null)
                {
                    throw new DataTableException(
                        $"{name}: Row at index {i} is null.");
                }

                if (string.IsNullOrWhiteSpace(row.Id))
                {
                    throw new DataTableException(
                        $"{name}: Row at index {i} has an empty Id.");
                }

                if (cache.ContainsKey(row.Id))
                {
                    throw new DataTableException(
                        $"{name}: Duplicate row Id found: {row.Id}");
                }

                cache.Add(row.Id, row);
            }
        }

        public bool Contains(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            BuildCache();

            return cache.ContainsKey(id);
        }

        public bool TryGet(string id, out TRow row)
        {
            row = null;

            if (string.IsNullOrWhiteSpace(id))
                return false;

            BuildCache();

            return cache.TryGetValue(id, out row);
        }

        public TRow GetOrDefault(string id)
        {
            return TryGet(id, out TRow row)
                ? row
                : null;
        }

        public TRow GetOrThrow(string id)
        {
            if (TryGet(id, out TRow row))
                return row;

            throw new DataTableException(
                $"{name}: Row not found. Id: {id}");
        }

        public IReadOnlyList<TRow> GetAll()
        {
            return rows;
        }

        public List<TRow> CreateListCopy()
        {
            return new List<TRow>(rows);
        }

        public override void ClearCache()
        {
            cache = null;
        }

#if UNITY_EDITOR
        public void Editor_SetRows(List<TRow> newRows)
        {
            rows = newRows ?? new List<TRow>();
            ClearCache();
        }
#endif
    }
}
