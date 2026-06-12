using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace INLO.Core.EditorUI.Editor
{
    public static class InloProjectAuditMenu
    {
        private static readonly string[] ExcludeDirs = new[]
        {
            "Library", "Temp", "Logs", "UserSettings", "obj", "bin", ".git",
            ".antigravity-ide", ".gemini", "packages"
        };

        [MenuItem("Tools/INLO/Project/Run Code Convention Audit")]
        public static void RunAudit()
        {
            Debug.Log("[Project Audit] Starting project code convention check...");

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError("[Project Audit] Failed to resolve project root path.");
                return;
            }

            int scannedFiles = 0;
            int encodingViolations = 0;
            int legacyUiViolations = 0;

            try
            {
                // recursively scan Packages and Assets folder
                ScanDirectory(Path.Combine(projectRoot, "Packages"), ref scannedFiles, ref encodingViolations, ref legacyUiViolations);
                ScanDirectory(Path.Combine(projectRoot, "Assets"), ref scannedFiles, ref encodingViolations, ref legacyUiViolations);

                if (encodingViolations == 0 && legacyUiViolations == 0)
                {
                    Debug.Log($"<color=green>[Project Audit] Audit Completed Successfully! Scanned Files: {scannedFiles}. No violations found.</color>");
                }
                else
                {
                    Debug.LogWarning($"<color=yellow>[Project Audit] Audit Completed with issues. Scanned Files: {scannedFiles}. Encodings Violations: {encodingViolations}, Legacy UI Violations: {legacyUiViolations}. Check errors above.</color>");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Project Audit] Audit failed unexpectedly: {exception.Message}");
            }
        }

        private static void ScanDirectory(string dirPath, ref int scannedFiles, ref int encodingViolations, ref int legacyUiViolations)
        {
            if (!Directory.Exists(dirPath))
                return;

            string dirName = Path.GetFileName(dirPath);
            foreach (string exclude in ExcludeDirs)
            {
                if (string.Equals(dirName, exclude, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            // scan files in the current folder
            string[] files = Directory.GetFiles(dirPath, "*.*");
            foreach (string file in files)
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == ".cs" || ext == ".md")
                {
                    AuditFile(file, ref scannedFiles, ref encodingViolations, ref legacyUiViolations);
                }
            }

            // recursively scan subdirectories
            string[] subDirs = Directory.GetDirectories(dirPath);
            foreach (string subDir in subDirs)
            {
                ScanDirectory(subDir, ref scannedFiles, ref encodingViolations, ref legacyUiViolations);
            }
        }

        private static void AuditFile(string filePath, ref int scannedFiles, ref int encodingViolations, ref int legacyUiViolations)
        {
            scannedFiles++;
            
            // Resolve project relative path for AssetDatabase context load
            string relPath = GetProjectRelativePath(filePath);
            UnityEngine.Object contextAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relPath);

            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);

                // 1. Check UTF-8 BOM (0xEF 0xBB 0xBF)
                bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;

                // 2. Check CRLF line endings (0x0D / CR byte presence)
                bool hasCrlf = false;
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (bytes[i] == 13)
                    {
                        hasCrlf = true;
                        break;
                    }
                }

                if (hasBom || hasCrlf)
                {
                    encodingViolations++;
                    string errorMsg = $"[Project Audit] Encoding Violation: {relPath} | BOM={hasBom}, HasCRLF={hasCrlf} (Must use UTF-8 No BOM & LF)";
                    Debug.LogError(errorMsg, contextAsset);
                }

                // 3. For C# files, audit legacy UnityEngine.UI usage
                if (filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    string content = File.ReadAllText(filePath, Encoding.UTF8);

                    string[] legacyTypes = new[] { "UnityEngine.UI.Text", "UnityEngine.UI.InputField", "UnityEngine.UI.Dropdown" };
                    foreach (string type in legacyTypes)
                    {
                        if (content.Contains(type))
                        {
                            legacyUiViolations++;
                            string errorMsg = $"[Project Audit] Legacy UI Violation: {relPath} | Contains legacy {type} (Must use TextMeshPro equivalents)";
                            Debug.LogError(errorMsg, contextAsset);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Project Audit] Failed to read and audit {relPath}: {exception.Message}", contextAsset);
            }
        }

        private static string GetProjectRelativePath(string fullPath)
        {
            string cleanRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace("\\", "/");
            string cleanFull = Path.GetFullPath(fullPath).Replace("\\", "/");

            if (cleanFull.StartsWith(cleanRoot, StringComparison.OrdinalIgnoreCase))
            {
                string rel = cleanFull.Substring(cleanRoot.Length);
                if (rel.StartsWith("/"))
                {
                    rel = rel.Substring(1);
                }
                return rel;
            }

            return fullPath;
        }
    }
}
