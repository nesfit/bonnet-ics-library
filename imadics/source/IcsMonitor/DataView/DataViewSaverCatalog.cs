using System.IO;

namespace Traffix.DataView
{
    /// <summary>
    /// Collection of extension methods for the DataOperationsCatalog to wrote to various text files such as csv, yaml, md and json.
    /// </summary>
    public static class DataViewSaverCatalog
    {
        /// <summary>
        /// Save the IDataView as CSV text.
        /// </summary>
        /// <param name="catalog">The DataOperationsCatalog catalog.</param>
        /// <param name="data">The data view to save.</param>
        /// <param name="stream">The stream to write to.</param>
        public static void SaveAsCsvText(this Microsoft.ML.DataOperationsCatalog _, Microsoft.ML.IDataView data, System.IO.Stream stream)
        {
            using var writer = new CsvDataViewWriter(new StreamWriter(stream), data.Schema);
            writer.BeginDocument();
            writer.AppendDataView(data);
            writer.EndDocument();
        }
        /// <summary>
        /// Save the IDataView as JSON text.
        /// </summary>
        /// <param name="catalog">The DataOperationsCatalog catalog.</param>
        /// <param name="data">The data view to save.</param>
        /// <param name="stream">The stream to write to.</param>
        public static void SaveAsJsonText(this Microsoft.ML.DataOperationsCatalog _, Microsoft.ML.IDataView data, Stream stream)
        {
            using var writer = new JsonDataViewWriter(new StreamWriter(stream), data.Schema);
            writer.BeginDocument();
            writer.AppendDataView(data);
            writer.EndDocument();
        }
        /// <summary>
        /// Save the IDataView as Markdown table.
        /// </summary>
        /// <param name="catalog">The DataOperationsCatalog catalog.</param>
        /// <param name="data">The data view to save.</param>
        /// <param name="stream">The stream to write to.</param>
        public static void SaveAsMarkdownText(this Microsoft.ML.DataOperationsCatalog _, Microsoft.ML.IDataView data, System.IO.Stream stream)
        {
            using var writer = new MarkdownDataViewWriter(new StreamWriter(stream), data.Schema);
            writer.BeginDocument();
            writer.AppendDataView(data);
            writer.EndDocument();
        }
        /// <summary>
        /// Save the IDataView as YAML table.
        /// </summary>
        /// <param name="catalog">The DataOperationsCatalog catalog.</param>
        /// <param name="data">The data view to save.</param>
        /// <param name="stream">The stream to write to.</param>
        public static void SaveAsYamlText(this Microsoft.ML.DataOperationsCatalog _, Microsoft.ML.IDataView data, System.IO.Stream stream)
        {
            using var writer = new YamlDataViewWriter(new StreamWriter(stream), data.Schema);
            writer.BeginDocument();
            writer.AppendDataView(data);
            writer.EndDocument();
        }
    }
}
