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
        public JsonDataViewWriter(TextWriter writer, DataViewSchema schema) : base(writer, schema)
        {
        }

        protected override void WriteFooter()
        {

            Writer.WriteLine();
            Writer.WriteLine("]");
        }

        protected override void WriteHeader()
        {
            Writer.WriteLine("[");
        }
        bool firstRow = true;

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

            var obj = GetExpando(values);
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