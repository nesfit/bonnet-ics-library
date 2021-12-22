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
    internal class CsvDataViewWriter : DataViewWriterBase
    {
        private readonly CsvWriter _csv;
        private bool _disposed;
        public CsvDataViewWriter(TextWriter writer, DataViewSchema schema) : base(writer, schema)
        {
            _csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
            { SanitizeForInjection = true });
        }
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

        protected override void WriteFooter()
        {
        }

        protected override void WriteHeader()
        {
            var hdr = GetExpandoScheme(Columns.Select(c => new KeyValuePair<string, object>(c.Name, c)));
            _csv.WriteDynamicHeader(hdr);
            _csv.NextRecord();
        }

        protected override void WriteRow(IEnumerable<KeyValuePair<string, object>> values)
        {
            var obj = GetExpando(values);
            _csv.WriteRecord(obj);
            _csv.NextRecord();
        }
    }
}