using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace INLO.Core.DataTable.Editor
{
    public static class GooglePublishedCsvRequest
    {
        public const int TimeoutSeconds = 30;
        public const ulong MaximumResponseBytes = 10UL * 1024UL * 1024UL;

        public static async Task<string> DownloadAsync(
            string url,
            CancellationToken cancellationToken)
        {
            if (!GooglePublishedCsvGridReader.IsSupportedUrl(url))
            {
                throw new InvalidOperationException(
                    "Only HTTPS Google Published CSV URLs are allowed.");
            }

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = TimeoutSeconds;
            request.redirectLimit = 3;
            request.SetRequestHeader("User-Agent", "INLO-Core-DataTable-Importer");

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    request.Abort();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (request.downloadedBytes > MaximumResponseBytes)
                {
                    request.Abort();
                    throw new InvalidOperationException(
                        $"Published CSV exceeds the {MaximumResponseBytes / 1024UL / 1024UL} MB limit.");
                }

                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException(
                    $"Failed to download published CSV. HTTP {request.responseCode}: {request.error}");
            }

            if (request.downloadedBytes > MaximumResponseBytes)
            {
                throw new InvalidOperationException(
                    $"Published CSV exceeds the {MaximumResponseBytes / 1024UL / 1024UL} MB limit.");
            }

            string contentType = request.GetResponseHeader("Content-Type") ?? string.Empty;

            bool supportedContentType =
                contentType.IndexOf("csv", StringComparison.OrdinalIgnoreCase) >= 0 ||
                contentType.IndexOf("text/plain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                contentType.IndexOf("application/octet-stream", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!supportedContentType)
            {
                throw new InvalidOperationException(
                    $"Published CSV request returned an unsupported Content-Type: {contentType}");
            }

            return request.downloadHandler.text;
        }
    }
}
