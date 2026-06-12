using INLO.Core.DataTable;
using System;
using System.Collections;
using System.Collections.Generic;

namespace INLO.Core.DataTable.Editor
{
    public static class DataTableImportPlanService
    {
        public static DataTableImportPlan PrepareXlsx(
            string sourcePath,
            string sheetName,
            DataTableAsset target)
        {
            List<DataTableValidationError> errors = new();

            DataTableImportService.ValidateXlsx(
                sourcePath,
                sheetName,
                target,
                errors,
                out DataTableGrid grid,
                out _);

            return BuildPlan(target, sourcePath, sheetName, grid, errors);
        }

        public static DataTableImportPlan PrepareGooglePublishedCsv(
            string sourceUrl,
            DataTableAsset target)
        {
            List<DataTableValidationError> errors = new();

            DataTableImportService.ValidateGooglePublishedCsv(
                sourceUrl,
                target,
                errors,
                out DataTableGrid grid,
                out _);

            return BuildPlan(target, sourceUrl, string.Empty, grid, errors);
        }

        public static DataTableImportPlan PrepareGrid(
            string source,
            string sheet,
            DataTableAsset target,
            DataTableGrid grid)
        {
            List<DataTableValidationError> errors = new();

            DataTableImportService.ValidateGrid(grid, errors);

            return BuildPlan(target, source, sheet, grid, errors);
        }

        public static bool Apply(
            DataTableImportPlan plan,
            out string statusMessage)
        {
            if (plan == null || !plan.CanApply)
            {
                statusMessage = "Apply failed. A successful prepared import plan is required.";
                return false;
            }

            return DataTableAssetGenerator.ApplyImportPlan(plan, out statusMessage);
        }

        public static DataTableImportPlan CreateFailedPlan(
            DataTableAsset target,
            string source,
            string sheet,
            string message)
        {
            List<DataTableValidationError> errors = new()
            {
                new DataTableValidationError(
                    DataTableValidationErrorType.Unknown,
                    -1,
                    string.Empty,
                    message)
            };

            return new DataTableImportPlan(
                target,
                source,
                sheet,
                null,
                null,
                null,
                null,
                errors);
        }

        private static DataTableImportPlan BuildPlan(
            DataTableAsset target,
            string source,
            string sheet,
            DataTableGrid grid,
            List<DataTableValidationError> errors)
        {
            Type rowType = null;
            DataTableImportSchema schema = null;
            IList rows = null;

            if (errors.Count == 0 &&
                DataTableRowTypeUtility.TryGetRowType(target, out rowType) &&
                DataTableImportSchema.TryCreate(rowType, errors, out schema))
            {
                DataTableSchemaValidator.Validate(grid, schema, errors);

                if (errors.Count == 0)
                    rows = DataTableRowMapper.MapRows(grid, schema, errors);
            }

            if (rowType == null && errors.Count == 0)
            {
                errors.Add(
                    new DataTableValidationError(
                        DataTableValidationErrorType.Unknown,
                        -1,
                        string.Empty,
                        "Could not resolve row type from target DataTable asset."));
            }

            return new DataTableImportPlan(
                target,
                DataTablePathUtility.ToProjectRelativePath(source),
                sheet,
                rowType,
                grid,
                rows,
                schema,
                errors);
        }
    }
}
