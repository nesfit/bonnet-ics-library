using System;
using System.IO;

namespace Traffix.DataView
{
    public enum OutputFormat { Json, Csv, Yaml, Markdown }
    /// <summary>
    /// The fatory for providing writers of the supported file formats.
    /// </summary>
    public class DataViewWriterFactory
    {
        /// <summary>
        /// Creates a writer of the request file format.
        /// <para/>
        /// Writer is created for the specific file format and data view schema. The data view of the compatible schema can 
        /// be written using the created writer. It is because some formats, e.g., CSV needs to know the schema in advance 
        /// in order to write the header.
        /// </summary>
        /// <param name="format">File format.</param>
        /// <param name="writer">Underlying text writer.</param>
        /// <param name="schema">The data view schema.</param>
        /// <returns>A writer for the requested <paramref name="format"/>. </returns>
        /// <exception cref="NotSupportedException"></exception>
        public static IDataViewWriter CreateWriter(OutputFormat format, TextWriter writer, Microsoft.ML.DataViewSchema schema)
        {
            return format switch
            {
                OutputFormat.Json => new JsonDataViewWriter(writer, schema),
                OutputFormat.Csv => new CsvDataViewWriter(writer, schema),
                OutputFormat.Markdown => new MarkdownDataViewWriter(writer, schema),
                OutputFormat.Yaml => new YamlDataViewWriter(writer, schema),
                _ => throw new NotSupportedException(),
            };
        }
    }
}