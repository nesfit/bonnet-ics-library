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
        /// <summary>
        /// The constructor of MD data view writer.
        /// </summary>
        /// <param name="writer">The output writer.</param>
        /// <param name="schema">the data view schema.</param>
        public MarkdownDataViewWriter(TextWriter writer, DataViewSchema schema) : base(writer, schema)
        {
        }
        /// <inheritdoc/>
        protected override void WriteFooter()
        {
        }
        /// <inheritdoc/>
        protected override void WriteHeader()
        {
            var headerLine = String.Join(" | ", Columns.Select(col => col.Name));
            var separatorLine = String.Join(" | ", Columns.Select(col => "---"));
            Writer.WriteLine($"| {headerLine} |");
            Writer.WriteLine($"| {separatorLine} |");
        }
        /// <inheritdoc/>
        protected override void WriteRow(IEnumerable<KeyValuePair<string, object>> values)
        {
            var valueLine = String.Join(" | ", values.Select(x => x.Value));
            Writer.WriteLine($"| {valueLine} |");
        }
    }
}