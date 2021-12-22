using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Traffix.DataView
{
    /// <summary>
    /// Implements Markdown dataview writer.
    /// </summary>
    internal class MarkdownDataViewWriter : DataViewWriterBase
    {
        public MarkdownDataViewWriter(TextWriter writer, DataViewSchema schema) : base(writer, schema)
        {
        }

        protected override void WriteFooter()
        {
        }

        protected override void WriteHeader()
        {
            var headerLine = String.Join(" | ", Columns.Select(col => col.Name));
            var separatorLine = String.Join(" | ", Columns.Select(col => "---"));
            Writer.WriteLine($"| {headerLine} |");
            Writer.WriteLine($"| {separatorLine} |");
        }

        protected override void WriteRow(IEnumerable<KeyValuePair<string, object>> values)
        {
            var valueLine = String.Join(" | ", values.Select(x => x.Value));
            Writer.WriteLine($"| {valueLine} |");
        }
    }
}