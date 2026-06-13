using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace INLO.Core.EditorUI.Editor
{
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    public struct DiagnosticEntry
    {
        public DiagnosticSeverity Severity;
        public string Module;
        public string Message;
        public string FilePath;
    }

    [InloControlCenterPanel("Control & Audit", "d_Settings", "panel-control", 5)]
    public sealed class ControlAuditPanel : IControlCenterPanel
    {
        private InloControlCenterWindow _window;
        private Button _utilBtnAudit;
        private Button _utilBtnUiBinding;
        private Button _utilBtnEventCi;
        private Button _utilBtnPoolKey;

        // Interactive Console Elements
        private ScrollView _consoleScroll;
        private Button _btnFilterAll;
        private Button _btnFilterErrors;
        private Button _btnFilterWarnings;
        private Button _btnFilterInfo;

        private readonly List<DiagnosticEntry> _allEntries = new();
        private DiagnosticSeverity? _currentFilter = null; // null means "All"

        public void Initialize(InloControlCenterWindow window, VisualElement root)
        {
            _window = window;

            _utilBtnAudit = root.Q<Button>("util-btn-audit");
            _utilBtnUiBinding = root.Q<Button>("util-btn-uibinding");
            _utilBtnEventCi = root.Q<Button>("util-btn-eventci");
            _utilBtnPoolKey = root.Q<Button>("util-btn-poolkey");

            _consoleScroll = root.Q<ScrollView>("cc-console-scroll");
            _btnFilterAll = root.Q<Button>("btn-filter-all");
            _btnFilterErrors = root.Q<Button>("btn-filter-errors");
            _btnFilterWarnings = root.Q<Button>("btn-filter-warnings");
            _btnFilterInfo = root.Q<Button>("btn-filter-info");

            if (_utilBtnAudit != null) _utilBtnAudit.clicked += RunCodeConventionAudit;
            if (_utilBtnUiBinding != null) _utilBtnUiBinding.clicked += RunUiBindingValidation;
            if (_utilBtnEventCi != null) _utilBtnEventCi.clicked += RunEventReleaseCheck;
            if (_utilBtnPoolKey != null) _utilBtnPoolKey.clicked += RunAutoFillPoolKeys;

            if (_btnFilterAll != null) _btnFilterAll.clicked += () => SetFilter(null);
            if (_btnFilterErrors != null) _btnFilterErrors.clicked += () => SetFilter(DiagnosticSeverity.Error);
            if (_btnFilterWarnings != null) _btnFilterWarnings.clicked += () => SetFilter(DiagnosticSeverity.Warning);
            if (_btnFilterInfo != null) _btnFilterInfo.clicked += () => SetFilter(DiagnosticSeverity.Info);

            _allEntries.Clear();
            RefreshConsole();
        }

        public void OnPanelEnabled() { }
        public void OnPanelDisabled() { }
        public void UpdateUI() { }

        private void SetFilter(DiagnosticSeverity? severity)
        {
            _currentFilter = severity;
            UpdateFilterButtonStyles();
            RefreshConsole();
        }

        private void UpdateFilterButtonStyles()
        {
            SetFilterButtonStyle(_btnFilterAll, _currentFilter == null);
            SetFilterButtonStyle(_btnFilterErrors, _currentFilter == DiagnosticSeverity.Error);
            SetFilterButtonStyle(_btnFilterWarnings, _currentFilter == DiagnosticSeverity.Warning);
            SetFilterButtonStyle(_btnFilterInfo, _currentFilter == DiagnosticSeverity.Info);
        }

        private static void SetFilterButtonStyle(Button button, bool active)
        {
            if (button == null) return;
            button.EnableInClassList("cc-console-filter-btn--active", active);
            button.EnableInClassList("cc-console-filter-btn--idle", !active);
        }

        public void RunCodeConventionAudit()
        {
            _allEntries.Clear();

            _allEntries.Add(new DiagnosticEntry
            {
                Severity = DiagnosticSeverity.Info,
                Module = "Audit",
                Message = "Starting project-wide code convention check..."
            });

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
            {
                _allEntries.Add(new DiagnosticEntry
                {
                    Severity = DiagnosticSeverity.Error,
                    Module = "Audit",
                    Message = "Failed to resolve project root path."
                });
                RefreshConsole();
                return;
            }

            int scannedFiles = 0;
            int encodingViolations = 0;
            int legacyUiViolations = 0;

            ScanAndReport(Path.Combine(projectRoot, "Packages"), ref scannedFiles, ref encodingViolations, ref legacyUiViolations);
            ScanAndReport(Path.Combine(projectRoot, "Assets"), ref scannedFiles, ref encodingViolations, ref legacyUiViolations);

            if (encodingViolations == 0 && legacyUiViolations == 0)
            {
                _allEntries.Add(new DiagnosticEntry
                {
                    Severity = DiagnosticSeverity.Info,
                    Module = "Audit",
                    Message = $"SUCCESS: Audit completed with ZERO violations! Scanned {scannedFiles} code files."
                });
            }
            else
            {
                _allEntries.Add(new DiagnosticEntry
                {
                    Severity = DiagnosticSeverity.Warning,
                    Module = "Audit",
                    Message = $"COMPLETED: Found {encodingViolations} encoding violations and {legacyUiViolations} legacy UI violations across {scannedFiles} files."
                });
            }

            // Fallback to "All" view after scan
            SetFilter(null);
        }

        private void ScanAndReport(string dirPath, ref int scanned, ref int encViolations, ref int uiViolations)
        {
            if (!Directory.Exists(dirPath)) return;

            string[] excludeDirs = new[] { "Library", "Temp", "Logs", "UserSettings", "obj", "bin", ".git" };
            string dirName = Path.GetFileName(dirPath);
            foreach (string ex in excludeDirs)
            {
                if (string.Equals(dirName, ex, StringComparison.OrdinalIgnoreCase)) return;
            }

            foreach (string file in Directory.GetFiles(dirPath, "*.*"))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == ".cs" || ext == ".md")
                {
                    scanned++;
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(file);
                        bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                        bool hasCrlf = false;
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            if (bytes[i] == 13) { hasCrlf = true; break; }
                        }

                        string relPath = file.Replace("\\", "/");
                        int assetsIdx = relPath.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
                        int pkgsIdx = relPath.IndexOf("/Packages/", StringComparison.OrdinalIgnoreCase);
                        if (assetsIdx != -1) relPath = relPath.Substring(assetsIdx + 1);
                        else if (pkgsIdx != -1) relPath = relPath.Substring(pkgsIdx + 1);

                        if (hasBom || hasCrlf)
                        {
                            encViolations++;
                            _allEntries.Add(new DiagnosticEntry
                            {
                                Severity = DiagnosticSeverity.Warning,
                                Module = "Encoding",
                                Message = $"BOM={hasBom}, CRLF={hasCrlf} (Must use UTF-8 No BOM & LF)",
                                FilePath = relPath
                            });
                        }

                        if (ext == ".cs")
                        {
                            string content = File.ReadAllText(file, Encoding.UTF8);
                            if (content.Contains("UnityEngine.UI.Text") || content.Contains("UnityEngine.UI.InputField"))
                            {
                                uiViolations++;
                                _allEntries.Add(new DiagnosticEntry
                                {
                                    Severity = DiagnosticSeverity.Error,
                                    Module = "Legacy UI",
                                    Message = "Uses legacy UnityEngine.UI components instead of TextMeshPro.",
                                    FilePath = relPath
                                });
                            }
                        }
                    }
                    catch (Exception exx)
                    {
                        _allEntries.Add(new DiagnosticEntry
                        {
                            Severity = DiagnosticSeverity.Error,
                            Module = "System",
                            Message = $"Failed to read file: {exx.Message}",
                            FilePath = file
                        });
                    }
                }
            }

            foreach (string sub in Directory.GetDirectories(dirPath))
            {
                ScanAndReport(sub, ref scanned, ref encViolations, ref uiViolations);
            }
        }

        private void RefreshConsole()
        {
            if (_consoleScroll == null) return;
            _consoleScroll.Clear();

            // Compute Filter Button Counts
            int totalAll = _allEntries.Count;
            int totalErrors = _allEntries.FindAll(e => e.Severity == DiagnosticSeverity.Error).Count;
            int totalWarnings = _allEntries.FindAll(e => e.Severity == DiagnosticSeverity.Warning).Count;
            int totalInfo = _allEntries.FindAll(e => e.Severity == DiagnosticSeverity.Info).Count;

            if (_btnFilterAll != null) _btnFilterAll.text = $"All ({totalAll})";
            if (_btnFilterErrors != null) _btnFilterErrors.text = $"Errors ({totalErrors})";
            if (_btnFilterWarnings != null) _btnFilterWarnings.text = $"Warnings ({totalWarnings})";
            if (_btnFilterInfo != null) _btnFilterInfo.text = $"Info ({totalInfo})";

            List<DiagnosticEntry> filtered = _currentFilter == null
                ? _allEntries
                : _allEntries.FindAll(e => e.Severity == _currentFilter.Value);

            if (filtered.Count == 0)
            {
                Label emptyLabel = new Label("No diagnostic entries to display under the current filter.");
                emptyLabel.AddToClassList("cc-log-text");
                _consoleScroll.Add(emptyLabel);
                return;
            }

            foreach (var entry in filtered)
            {
                VisualElement row = new VisualElement();
                row.AddToClassList("cc-diag-row");

                // Icon selection depending on severity
                string iconName = "d_console.infoicon";
                string badgeClass = "cc-diag-badge--info";
                string badgeText = "INFO";

                switch (entry.Severity)
                {
                    case DiagnosticSeverity.Warning:
                        iconName = "d_console.warnicon";
                        badgeClass = "cc-diag-badge--warning";
                        badgeText = "WARN";
                        break;
                    case DiagnosticSeverity.Error:
                        iconName = "d_console.erroricon";
                        badgeClass = "cc-diag-badge--error";
                        badgeText = "ERROR";
                        break;
                }

                Texture2D iconTex = EditorGUIUtility.IconContent(iconName)?.image as Texture2D;
                if (iconTex != null)
                {
                    VisualElement iconEl = new VisualElement();
                    iconEl.AddToClassList("cc-diag-icon");
                    iconEl.style.backgroundImage = new StyleBackground(iconTex);
                    row.Add(iconEl);
                }

                Label badge = new Label(badgeText);
                badge.AddToClassList("cc-diag-badge");
                badge.AddToClassList(badgeClass);
                row.Add(badge);

                Label module = new Label($"[{entry.Module}]");
                module.AddToClassList("cc-diag-badge");
                module.AddToClassList("cc-diag-badge--info");
                row.Add(module);

                Label msg = new Label(entry.Message);
                msg.AddToClassList("cc-diag-msg");
                row.Add(msg);

                if (!string.IsNullOrEmpty(entry.FilePath))
                {
                    Label pathLabel = new Label(entry.FilePath);
                    pathLabel.AddToClassList("cc-diag-path");
                    row.Add(pathLabel);

                    // Add high-fidelity project-relative double-click/single-click selection callback
                    row.RegisterCallback<PointerUpEvent>(evt =>
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(entry.FilePath);
                        if (obj != null)
                        {
                            Selection.activeObject = obj;
                            EditorGUIUtility.PingObject(obj);
                        }
                    });
                }

                _consoleScroll.Add(row);
            }
        }

        private void RunUiBindingValidation()
        {
            _allEntries.Clear();
            _allEntries.Add(new DiagnosticEntry
            {
                Severity = DiagnosticSeverity.Info,
                Module = "UI Binding",
                Message = "Running table scene bindings validation..."
            });

            try
            {
                EditorApplication.ExecuteMenuItem("Tools/INLO/UI Scene Binding/Validate Tables");
                _allEntries.Add(new DiagnosticEntry
                {
                    Severity = DiagnosticSeverity.Info,
                    Module = "UI Binding",
                    Message = "UI Table scenes validation successfully completed. Check Unity Console for individual table errors."
                });
            }
            catch (Exception ex)
            {
                _allEntries.Add(new DiagnosticEntry
                {
                    Severity = DiagnosticSeverity.Error,
                    Module = "UI Binding",
                    Message = $"Validation exception triggered: {ex.Message}"
                });
            }
            SetFilter(null);
        }

        private void RunEventReleaseCheck()
        {
            _allEntries.Clear();
            _allEntries.Add(new DiagnosticEntry
            {
                Severity = DiagnosticSeverity.Info,
                Module = "Events",
                Message = "Executing Event Release Rules Build Check..."
            });

            try
            {
                EditorApplication.ExecuteMenuItem("Tools/INLO/Events/Validation/Validate Release Build Rules");
                _allEntries.Add(new DiagnosticEntry
                {
                    Severity = DiagnosticSeverity.Info,
                    Module = "Events",
                    Message = "Release Build rules scanning completed. Check Unity console for full integrity checklist."
                });
            }
            catch (Exception ex)
            {
                _allEntries.Add(new DiagnosticEntry
                {
                    Severity = DiagnosticSeverity.Error,
                    Module = "Events",
                    Message = $"Check failed: {ex.Message}"
                });
            }
            SetFilter(null);
        }

        private void RunAutoFillPoolKeys()
        {
            _allEntries.Clear();
            _allEntries.Add(new DiagnosticEntry
            {
                Severity = DiagnosticSeverity.Info,
                Module = "Pooling",
                Message = "Auto-filling missing PoolKeys from Prefab names..."
            });

            try
            {
                EditorApplication.ExecuteMenuItem("Tools/INLO/Pooling/Utilities/Fill Missing PoolKeys From Prefab Names");
                _allEntries.Add(new DiagnosticEntry
                {
                    Severity = DiagnosticSeverity.Info,
                    Module = "Pooling",
                    Message = "Prefabs scan complete. Normalization applied to all registered Database entries."
                });
            }
            catch (Exception ex)
            {
                _allEntries.Add(new DiagnosticEntry
                {
                    Severity = DiagnosticSeverity.Error,
                    Module = "Pooling",
                    Message = $"Auto-fill failed: {ex.Message}"
                });
            }
            SetFilter(null);
        }
    }
}
