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
    public class PacketAnnotationSourceFile
    {
        public class LabeledPackets
        {
            [Index(0)]
            public int PacketNumber { get; set; }
            [Index(1)]
            public int PacketLabel { get; set; }
        }

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
