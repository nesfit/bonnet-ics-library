using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Traffix.DataView
{
    /// <summary>
    /// Implements data view writer for CSV output.
    /// </summary>
    internal class CsvDataViewWriter : DataViewWriterBase
    {
        private readonly CsvWriter _csv;
        private bool _disposed;
        /// <summary>
        /// Creates a new instance of the writer.
        /// </summary>
        /// <param name="writer">The underlying writer to be use for output.</param>
        /// <param name="schema">The data view schema (required to correctly write the output).</param>
        public CsvDataViewWriter(TextWriter writer, DataViewSchema schema) : base(writer, schema)
        {
            _csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            { SanitizeForInjection = false });
        }
        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _csv.Flush();
                    _csv.Dispose();
                }
                _disposed = true;
            }

            // Call base class implementation.
            base.Dispose(disposing);
        }
        /// <inheritdoc/>
        protected override void WriteFooter()
        {
        }
        /// <inheritdoc/>
        protected override void WriteHeader()
        {
            var hdr = GetExpandoScheme(Columns.Select(c => new KeyValuePair<string, object>(c.Name, c)));
            _csv.WriteDynamicHeader(hdr);
            _csv.NextRecord();
        }
        /// <inheritdoc/>
        protected override void WriteRow(IEnumerable<KeyValuePair<string, object>> values)
        {
            var obj = GetExpandoObject(values);
            _csv.WriteRecord(obj);
            _csv.NextRecord();
        }
    }
}