using System.IO;
using System.IO.Compression;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IcsMonitor.Utils
{
    /// <summary>
    /// An extension class for I/O operations with <see cref="ZipArchiveEntry"/>.
    /// </summary>
    public static class ZipEntryYamlIO
    {
        /// <summary>
        /// Writes <paramref name="value"/> as yaml file to Zip archive <paramref name="entry"/>.
        /// </summary>
        /// <typeparam name="T">The type of object to write.</typeparam>
        /// <param name="entry">Zip archive entry.</param>
        /// <param name="value">Value of the object to write.</param>
        public static void WriteYaml<T>(this ZipArchiveEntry entry, T value)
        {
            using var writer = new StreamWriter(entry.Open());
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(value);
            writer.WriteLine(yaml);
        }
        /// <summary>
        /// Reads the Zip archive <paramref name="entry"/> as YAML document of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the document.</typeparam>
        /// <param name="entry">The Zip archive entry.</param>
        /// <returns>Object of type <typeparamref name="T"/> read from the given <paramref name="entry"/>.</returns>
        public static T ReadYaml<T>(this ZipArchiveEntry entry)
        {
            using var reader = new StreamReader(entry.Open());
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = reader.ReadToEnd();
            reader.Close();
            return deserializer.Deserialize<T>(yaml);
        }
    }
}
