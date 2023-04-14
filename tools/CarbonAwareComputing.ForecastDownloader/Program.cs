using CarbonAwareComputing.ExecutionForecast;
using CsvHelper;
using System.Globalization;
using System.Text;

namespace CarbonAwareComputing.ForecastDownloader
{
    internal class Program
    {
        private static Dictionary<DateTime, Dictionary<string, double?>> table = new Dictionary<DateTime, Dictionary<string, double?>>();
        static async Task Main(FileInfo? outputFile)
        {
            if (outputFile == null)
            {
                Console.WriteLine("Option --output-file must be set");
                return;
            }

            var httpClient = new HttpClient();

            foreach (var computingLocation in ComputingLocations.All.Where(l => l.IsActive))
            {
                var uri = new Uri($"https://carbonawarecomputing.blob.core.windows.net/forecasts/{computingLocation.Name}.json");
                var json = await httpClient.GetStringAsync(uri);
                var jsonFile = System.Text.Json.JsonSerializer.Deserialize<EmissionsForecastJsonFile>(json)!;
                AddTable(jsonFile.Emissions, computingLocation.Name);
            }


            var sb = new StringBuilder();
            await using var textWriter = new StringWriter(sb);
            await using var csvWriter = new CsvWriter(textWriter, CultureInfo.CurrentCulture);

            var records = new List<CsvRecord>();
            //csvWriter.WriteHeader<CsvRecord>();
            foreach (var kv in table)
            {
                var record = new CsvRecord(kv.Key, default, default, default, default);
                foreach (var row in kv.Value)
                {
                    switch (row.Key)
                    {
                        case "de":
                            record = record with { De = row.Value };
                            break;
                        case "fr":
                            record = record with { Fr = row.Value };
                            break;
                        case "at":
                            record = record with { At = row.Value };
                            break;
                        case "ch":
                            record = record with { Ch = row.Value };
                            break;
                    }
                }
                records.Add(record);
            }
            await csvWriter.WriteRecordsAsync(records);
            await csvWriter.FlushAsync();
            await File.WriteAllTextAsync(outputFile.FullName, sb.ToString());

            Console.WriteLine("Hello, World!");
        }

        private static void AddTable(List<EmissionsDataRaw> emissions, string location)
        {
            foreach (var data in emissions)
            {
                var t = data.Time.LocalDateTime;
                var row = GetRow(t);
                row[location] = data.Rating;
            }
        }
        private static Dictionary<string, double?> GetRow(DateTime time)
        {
            if (table.TryGetValue(time, out var row))
            {
                return row;
            }
            var newRow = new Dictionary<string, double?>();
            table[time] = newRow;
            return newRow;
        }
    }

    internal record CsvRecord(DateTime Time, double? De, double? Fr, double? At, double? Ch);
}