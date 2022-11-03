using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace IcsMonitor.Flows
{
    /// <summary>
    /// Represents a packet annotation source file.
    /// <para/>
    /// Packet annotation is a CSV file that matches labesl to packet numbers. 
    /// </summary>
    public class PacketAnnotationSourceFile
    {
        /// <summary>
        /// A single packet label record. It contains annotation for reading and writing it direclty with CSVHelper library..
        /// </summary>
        public class LabeledPackets
        {
            /// <summary>
            /// Packet number column.
            /// </summary>
            [Index(0)]
            public int PacketNumber { get; set; }
            /// <summary>
            /// Packet label column.
            /// </summary>
            [Index(1)]
            public int PacketLabel { get; set; }
        }
        /// <summary>
        /// Reads the labels from the CSV file and provides them as enumerable.
        /// </summary>
        /// <param name="csvPath">The source CSV file.</param>
        /// <returns>The enumerable of packet label records.</returns>

        public static IEnumerable<LabeledPackets> ReadLabels(string csvPath)
        {
            using var reader = new StreamReader(csvPath);
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ";"
            };

            using (var csv = new CsvReader(reader, csvConfig))
            {
                return csv.GetRecords<LabeledPackets>().ToList();
            }
        }
        /// <summary>
        /// Reads the packet labels from the CSV file and provides them as observable.
        /// </summary>
        /// <param name="csvPath">The source CSV file.</param>
        /// <returns>The observable of packet label records.</returns>
        public static IObservable<LabeledPackets> ReadLabelsAsync(string csvPath)
        {
            return Observable.Create<LabeledPackets>((observer, cancellation) => Task.Factory.StartNew(
                () =>
                {
                    using var reader = new StreamReader(csvPath);
                    var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = false,
                        Delimiter = ";"
                    };

                    using var csv = new CsvReader(reader, csvConfig);
                    while (!cancellation.IsCancellationRequested && csv.Read())
                    {

                        observer.OnNext(csv.GetRecord<LabeledPackets>());
                    }
                    observer.OnCompleted();
                }));
        }
    }
}
