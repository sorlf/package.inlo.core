using INLO.Core.DataTable;
using System;
using System.Collections;
using System.Collections.Generic;

namespace INLO.Core.DataTable.Editor
{
    public static class DataTableImportService
    {
        public static bool ValidateXlsx(
            string sourcePath,
            string sheetName,
            DataTableAsset targetTableAsset,
            List<DataTableValidationError> validationErrors,
            out DataTableGrid grid,
            out string statusMessage)
        {
            grid = null;

            ValidateBasicInputs(
                sourcePath,
                targetTableAsset,
                validationErrors);

            if (validationErrors.Count > 0)
            {
                statusMessage = "Validation failed.";
                return false;
            }

            try
            {
                string absoluteSourcePath = DataTablePathUtility.ToAbsolutePath(sourcePath);

                grid = XlsxTableGridReader.ReadSheet(
                    absoluteSourcePath,
                    sheetName);

                ValidateGridForTarget(
                    grid,
                    targetTableAsset,
                    validationErrors);

                if (validationErrors.Count > 0)
                {
                    statusMessage = "Validation failed.";
                    return false;
                }

                string sheetText = string.IsNullOrWhiteSpace(sheetName)
                    ? "First Sheet"
                    : sheetName;

                statusMessage =
                    $"Validation passed. Sheet: {sheetText}, Headers: {grid.HeaderCount}, Rows: {grid.RowCount}";

                return true;
            }
            catch (Exception exception)
            {
                validationErrors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.Unknown,
                        -1,
                        string.Empty,
                        exception.Message));

                statusMessage = "Validation failed.";
                return false;
            }
        }

        public static bool ValidateXlsx(
            string sourcePath,
            DataTableAsset targetTableAsset,
            List<DataTableValidationError> validationErrors,
            out DataTableGrid grid,
            out string statusMessage)
        {
            return ValidateXlsx(
                sourcePath,
                targetTableAsset != null ? targetTableAsset.EditorSheetName : string.Empty,
                targetTableAsset,
                validationErrors,
                out grid,
                out statusMessage);
        }

        public static bool ValidateGooglePublishedCsv(
            string sourceUrl,
            DataTableAsset targetTableAsset,
            List<DataTableValidationError> validationErrors,
            out DataTableGrid grid,
            out string statusMessage)
        {
            grid = null;

            ValidateUrlInputs(
                sourceUrl,
                targetTableAsset,
                validationErrors);

            if (validationErrors.Count > 0)
            {
                statusMessage = "Validation failed.";
                return false;
            }

            try
            {
                grid = GooglePublishedCsvGridReader.Read(sourceUrl);

                ValidateGridForTarget(
                    grid,
                    targetTableAsset,
                    validationErrors);

                if (validationErrors.Count > 0)
                {
                    statusMessage = "Validation failed.";
                    return false;
                }

                statusMessage =
                    $"Validation passed. Source: Published CSV, Headers: {grid.HeaderCount}, Rows: {grid.RowCount}";

                return true;
            }
            catch (Exception exception)
            {
                validationErrors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.Unknown,
                        -1,
                        string.Empty,
                        exception.Message));

                statusMessage = "Validation failed.";
                return false;
            }
        }

        public static bool ImportXlsx(
            string sourcePath,
            string sheetName,
            DataTableAsset targetTableAsset,
            List<DataTableValidationError> validationErrors,
            ref DataTableGrid previewGrid,
            out string statusMessage)
        {
            bool validationPassed = ValidateXlsx(
                sourcePath,
                sheetName,
                targetTableAsset,
                validationErrors,
                out DataTableGrid validatedGrid,
                out statusMessage);

            if (!validationPassed)
            {
                statusMessage = "Import cancelled because validation failed.";
                previewGrid = validatedGrid;
                return false;
            }

            previewGrid = validatedGrid;

            try
            {
                if (!DataTableRowTypeUtility.TryGetRowType(targetTableAsset, out Type rowType))
                {
                    validationErrors.Add(
                        new DataTableValidationError(
                            DataTableValidationErrorType.Unknown,
                            -1,
                            string.Empty,
                            "Could not resolve row type from target DataTable asset."));

                    statusMessage = "Import failed.";
                    return false;
                }

                IList rows = DataTableRowMapper.MapRows(
                    previewGrid,
                    rowType,
                    validationErrors);

                if (validationErrors.Count > 0)
                {
                    statusMessage = "Import failed because row mapping has errors.";
                    return false;
                }

                bool success = DataTableAssetGenerator.ApplyRowsUntyped(
                    targetTableAsset,
                    rows,
                    rowType,
                    out string errorMessage);

                if (!success)
                {
                    validationErrors.Add(
                        new DataTableValidationError(
                            DataTableValidationErrorType.Unknown,
                            -1,
                            string.Empty,
                            errorMessage));

                    statusMessage = "Import failed.";
                    return false;
                }

                DataTableAssetGenerator.ApplySourceConfig(
                    targetTableAsset,
                    sourcePath,
                    sheetName);

                statusMessage = $"Import completed. Row Count: {rows.Count}";
                return true;
            }
            catch (Exception exception)
            {
                validationErrors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.Unknown,
                        -1,
                        string.Empty,
                        exception.Message));

                statusMessage = "Import failed.";
                return false;
            }
        }

        public static bool ImportXlsx(
            string sourcePath,
            DataTableAsset targetTableAsset,
            List<DataTableValidationError> validationErrors,
            ref DataTableGrid previewGrid,
            out string statusMessage)
        {
            return ImportXlsx(
                sourcePath,
                targetTableAsset != null ? targetTableAsset.EditorSheetName : string.Empty,
                targetTableAsset,
                validationErrors,
                ref previewGrid,
                out statusMessage);
        }

        public static bool ImportGooglePublishedCsv(
            string sourceUrl,
            DataTableAsset targetTableAsset,
            List<DataTableValidationError> validationErrors,
            ref DataTableGrid previewGrid,
            out string statusMessage)
        {
            bool validationPassed = ValidateGooglePublishedCsv(
                sourceUrl,
                targetTableAsset,
                validationErrors,
                out DataTableGrid validatedGrid,
                out statusMessage);

            if (!validationPassed)
            {
                statusMessage = "Import cancelled because validation failed.";
                previewGrid = validatedGrid;
                return false;
            }

            previewGrid = validatedGrid;

            try
            {
                if (!DataTableRowTypeUtility.TryGetRowType(targetTableAsset, out Type rowType))
                {
                    validationErrors.Add(
                        new DataTableValidationError(
                            DataTableValidationErrorType.Unknown,
                            -1,
                            string.Empty,
                            "Could not resolve row type from target DataTable asset."));

                    statusMessage = "Import failed.";
                    return false;
                }

                IList rows = DataTableRowMapper.MapRows(
                    previewGrid,
                    rowType,
                    validationErrors);

                if (validationErrors.Count > 0)
                {
                    statusMessage = "Import failed because row mapping has errors.";
                    return false;
                }

                bool success = DataTableAssetGenerator.ApplyRowsUntyped(
                    targetTableAsset,
                    rows,
                    rowType,
                    out string errorMessage);

                if (!success)
                {
                    validationErrors.Add(
                        new DataTableValidationError(
                            DataTableValidationErrorType.Unknown,
                            -1,
                            string.Empty,
                            errorMessage));

                    statusMessage = "Import failed.";
                    return false;
                }

                DataTableAssetGenerator.ApplySourceConfig(
                    targetTableAsset,
                    sourceUrl,
                    string.Empty);

                statusMessage = $"Import completed. Row Count: {rows.Count}";
                return true;
            }
            catch (Exception exception)
            {
                validationErrors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.Unknown,
                        -1,
                        string.Empty,
                        exception.Message));

                statusMessage = "Import failed.";
                return false;
            }
        }

        private static void ValidateBasicInputs(
            string sourcePath,
            DataTableAsset targetTableAsset,
            List<DataTableValidationError> validationErrors)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                validationErrors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.Unknown,
                        -1,
                        string.Empty,
                        "Source path or URL is empty."));

                return;
            }

            if (!DataTablePathUtility.Exists(sourcePath))
            {
                validationErrors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.Unknown,
                        -1,
                        string.Empty,
                        $"Source file does not exist: {sourcePath}"));
            }

            if (targetTableAsset == null)
            {
                validationErrors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.Unknown,
                        -1,
                        string.Empty,
                        "Target DataTable asset is not assigned."));
            }
        }

        private static void ValidateUrlInputs(
            string sourceUrl,
            DataTableAsset targetTableAsset,
            List<DataTableValidationError> validationErrors)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
            {
                validationErrors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.Unknown,
                        -1,
                        string.Empty,
                        "Published CSV URL is empty."));

                return;
            }

            if (!GooglePublishedCsvGridReader.IsSupportedUrl(sourceUrl))
            {
                validationErrors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.Unknown,
                        -1,
                        string.Empty,
                        "Only HTTPS Google Published CSV URLs are allowed."));
            }

            if (targetTableAsset == null)
            {
                validationErrors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.Unknown,
                        -1,
                        string.Empty,
                        "Target DataTable asset is not assigned."));
            }
        }

        private static void ValidateGridForTarget(
            DataTableGrid grid,
            DataTableAsset targetTableAsset,
            List<DataTableValidationError> validationErrors)
        {
            ValidateGrid(
                grid,
                validationErrors);

            if (validationErrors.Count > 0)
                return;

            if (DataTableRowTypeUtility.TryGetRowType(targetTableAsset, out Type rowType))
            {
                DataTableSchemaValidator.Validate(
                    grid,
                    rowType,
                    validationErrors);
            }
            else
            {
                validationErrors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.Unknown,
                        -1,
                        string.Empty,
                        "Could not resolve row type from target DataTable asset."));
            }
        }

        public static void ValidateGrid(
            DataTableGrid grid,
            List<DataTableValidationError> validationErrors)
        {
            if (grid == null)
            {
                validationErrors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.Unknown,
                        -1,
                        string.Empty,
                        "DataTable grid is null."));

                return;
            }

            if (grid.HeaderCount == 0)
            {
                validationErrors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.MissingColumn,
                        1,
                        string.Empty,
                        "Header row is empty."));

                return;
            }

            HashSet<string> headerSet = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < grid.Headers.Count; i++)
            {
                string header = grid.Headers[i];

                if (string.IsNullOrWhiteSpace(header))
                {
                    validationErrors.Add(
                        new DataTableValidationError(
                            DataTableValidationErrorType.MissingColumn,
                            1,
                            $"Column {i + 1}",
                            "Header is empty."));

                    continue;
                }

                if (DataTableImportRules.IsIgnoredColumn(header))
                    continue;

                if (!headerSet.Add(header))
                {
                    validationErrors.Add(
                        new DataTableValidationError(
                            DataTableValidationErrorType.DuplicateId,
                            1,
                            header,
                            $"Duplicate header found: {header}"));
                }
            }

            int idColumnIndex = grid.Headers.FindIndex(
                header => string.Equals(header, "id", StringComparison.OrdinalIgnoreCase));

            if (idColumnIndex < 0)
            {
                validationErrors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.MissingColumn,
                        1,
                        "id",
                        "Required column is missing: id"));
            }
        }
    }
}
