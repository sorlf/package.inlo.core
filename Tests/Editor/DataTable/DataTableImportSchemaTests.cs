using INLO.Core.DataTable;
using INLO.Core.DataTable.Editor;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace INLO.Core.Editor.Tests
{
    public sealed class DataTableImportSchemaTests
    {
        [Test]
        public void Schema_UsesOneRequiredRuleForStringNullableAndValueTypes()
        {
            List<DataTableValidationError> errors = new();

            bool success = DataTableImportSchema.TryCreate(
                typeof(SchemaRow),
                errors,
                out DataTableImportSchema schema);

            Assert.That(success, Is.True);
            Assert.That(errors, Is.Empty);
            Assert.That(schema.TryGetField("id", out DataTableImportField id), Is.True);
            Assert.That(id.Required, Is.True);
            Assert.That(schema.TryGetField("description", out DataTableImportField description), Is.True);
            Assert.That(description.Required, Is.False);
            Assert.That(schema.TryGetField("optionalNumber", out DataTableImportField optionalNumber), Is.True);
            Assert.That(optionalNumber.Required, Is.False);
            Assert.That(schema.TryGetField("count", out DataTableImportField count), Is.True);
            Assert.That(count.Required, Is.True);
        }

        [Test]
        public void Schema_RejectsConflictingOptionalAndRequiredAttributes()
        {
            List<DataTableValidationError> errors = new();

            bool success = DataTableImportSchema.TryCreate(
                typeof(ConflictingRow),
                errors,
                out _);

            Assert.That(success, Is.False);
            Assert.That(errors, Has.Some.Matches<DataTableValidationError>(
                error => error.Message.Contains("both DataTableRequired and DataTableOptional")));
        }

        [Serializable]
        public sealed class SchemaRow : IDataTableRow
        {
            public string id;
            public string description;
            public int? optionalNumber;
            public int count;
            public string Id => id;
        }

        [Serializable]
        public sealed class ConflictingRow : IDataTableRow
        {
            public string id;

            [DataTableRequired]
            [DataTableOptional]
            public string invalid;

            public string Id => id;
        }
    }
}
