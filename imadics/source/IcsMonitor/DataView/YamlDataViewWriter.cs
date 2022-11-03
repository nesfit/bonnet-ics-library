using Microsoft.ML;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Traffix.DataView
{
    /// <summary>
    /// Implements YAML dataview writer.
    /// </summary>
    internal class YamlDataViewWriter : DataViewWriterBase
    {
        private readonly ISerializer _serializer;

        /// <summary>
        /// The constructor of JSON data view writer.
        /// </summary>
        /// <param name="writer">The output writer.</param>
        /// <param name="schema">the data view schema.</param>
        public YamlDataViewWriter(TextWriter writer, DataViewSchema schema) : base(writer, schema)
        {
            _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        }
        /// <inheritdoc/>
        protected override void WriteFooter()
        {

        }
        /// <inheritdoc/>
        protected override void WriteHeader()
        {

        }
        /// <inheritdoc/>
        protected override void WriteRow(IEnumerable<KeyValuePair<string, object>> values)
        {

            var obj = GetExpandoObject(values);
            var yaml = _serializer.Serialize(obj);
            Writer.WriteLine("-");
            Writer.Indent += 1;
            foreach(var line in yaml.Split('\n'))
            {
                Writer.WriteLine(line.TrimEnd());
            }
            Writer.Indent -= 1;
        }
    }
}