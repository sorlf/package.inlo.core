using System.Text;
using UnityEngine;

namespace INLO.Core.Pooling.Editor
{
    public static class PoolKeyEditorUtility
    {
        public static string CreateKeyFromPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                return string.Empty;
            }

            return NormalizeKey(prefab.name);
        }

        public static string NormalizeKey(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            StringBuilder builder = new(raw.Length);

            bool previousWasSeparator = false;

            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];

                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(c);
                    previousWasSeparator = false;
                    continue;
                }

                if (c == '_' || c == '-' || c == ' ')
                {
                    if (!previousWasSeparator && builder.Length > 0)
                    {
                        builder.Append('_');
                        previousWasSeparator = true;
                    }
                }
            }

            string result = builder.ToString().Trim('_');

            return result;
        }
    }
}
