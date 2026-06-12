using INLO.Core.DataTable;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace INLO.Core.DataTable.Editor
{
    public sealed class DataTableDatabaseCandidate
    {
        public DataTableDatabaseCandidate(DataTableAsset table)
        {
            Table = table;
            AssetPath = AssetDatabase.GetAssetPath(table);
            TableType = table?.GetType();
            LastImportStatus = table?.EditorLastImportStatus ?? string.Empty;
        }

        public DataTableAsset Table { get; }
        public string AssetPath { get; }
        public Type TableType { get; }
        public string LastImportStatus { get; }
        public bool Selected { get; set; } = true;
    }

    public sealed class DataTableDatabasePlan
    {
        public readonly List<DataTableDatabaseCandidate> Candidates = new();
        public readonly List<string> Conflicts = new();

        public DataTableDatabase Target { get; set; }
        public int Added { get; set; }
        public int Removed { get; set; }
        public int Unchanged { get; set; }
        public bool CanApply => Conflicts.Count == 0;

        public void Recalculate(DataTableDatabase database)
        {
            Added = 0;
            Removed = 0;
            Unchanged = 0;

            HashSet<DataTableAsset> existing = new();
            if (database != null)
            {
                for (int i = 0; i < database.Tables.Count; i++)
                {
                    if (database.Tables[i] != null)
                        existing.Add(database.Tables[i]);
                }
            }

            HashSet<DataTableAsset> selected = new();
            for (int i = 0; i < Candidates.Count; i++)
            {
                if (!Candidates[i].Selected)
                    continue;

                selected.Add(Candidates[i].Table);

                if (existing.Contains(Candidates[i].Table))
                    Unchanged++;
                else
                    Added++;
            }

            foreach (DataTableAsset table in existing)
            {
                if (!selected.Contains(table))
                    Removed++;
            }
        }
    }
}
