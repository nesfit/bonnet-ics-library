using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace Traffix.DataView
{
    /// <summary>
    /// Implements JSON data view writer.
    /// </summary>
    internal class JsonDataViewWriter : DataViewWriterBase
    {
        /// <summary>
        /// The constructor of JSON data view writer.
        /// </summary>
        /// <param name="writer">The output writer.</param>
        /// <param name="schema">the data view schema.</param>
        public JsonDataViewWriter(TextWriter writer, DataViewSchema schema) : base(writer, schema)
        {
        }

        /// <inheritdoc/>
        protected override void WriteFooter()
        {

            Writer.WriteLine();
            Writer.WriteLine("]");
        }

        /// <inheritdoc/>
        protected override void WriteHeader()
        {
            Writer.WriteLine("[");
        }
        bool firstRow = true;

        /// <inheritdoc/>
        protected override void WriteRow(IEnumerable<KeyValuePair<string, object>> values)
        {
            if (!firstRow)
            {
                Writer.WriteLine(",");
            }
            else
            {
                firstRow = false;
            }

            var obj = GetExpandoObject(values);
            try
            {
                var jsonString = System.Text.Json.JsonSerializer.Serialize(obj);
                Writer.Write(jsonString);
            }
            catch(Exception)
            {

            }
        }
    }
}