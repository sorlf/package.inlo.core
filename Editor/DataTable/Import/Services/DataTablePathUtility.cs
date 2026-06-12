using System.IO;
using UnityEngine;

namespace INLO.Core.DataTable.Editor
{
    public static class DataTablePathUtility
    {
        public static string ProjectRootPath
        {
            get
            {
                string dataPath = Application.dataPath;
                DirectoryInfo assetsDirectory = Directory.GetParent(dataPath);
                return assetsDirectory?.FullName.Replace("\\", "/") ?? string.Empty;
            }
        }

        public static string NormalizeSeparators(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            return path.Replace("\\", "/");
        }

        public static string ToProjectRelativePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            if (path.StartsWith("http:", System.StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https:", System.StringComparison.OrdinalIgnoreCase))
            {
                return FixUrlDoubleSlash(path);
            }

            string normalizedPath = NormalizeSeparators(Path.GetFullPath(path));
            string projectRoot = NormalizeSeparators(ProjectRootPath);

            if (string.IsNullOrWhiteSpace(projectRoot))
                return normalizedPath;

            if (!normalizedPath.StartsWith(projectRoot))
                return normalizedPath;

            string relativePath = normalizedPath.Substring(projectRoot.Length);

            if (relativePath.StartsWith("/"))
                relativePath = relativePath.Substring(1);

            return relativePath;
        }

        public static string ToAbsolutePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            if (path.StartsWith("http:", System.StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https:", System.StringComparison.OrdinalIgnoreCase))
            {
                return FixUrlDoubleSlash(path);
            }

            string normalizedPath = NormalizeSeparators(path);

            if (Path.IsPathRooted(normalizedPath))
                return NormalizeSeparators(Path.GetFullPath(normalizedPath));

            string combinedPath = Path.Combine(ProjectRootPath, normalizedPath);
            return NormalizeSeparators(Path.GetFullPath(combinedPath));
        }

        public static bool Exists(string path)
        {
            string absolutePath = ToAbsolutePath(path);
            return File.Exists(absolutePath);
        }

        public static string FixUrlDoubleSlash(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            if (url.StartsWith("https:/", System.StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            {
                return "https://" + url.Substring(7);
            }

            if (url.StartsWith("http:/", System.StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase))
            {
                return "http://" + url.Substring(6);
            }

            return url;
        }
    }
}
