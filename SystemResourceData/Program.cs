using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResourceUsage
{
    internal class Program
    {
        public class PerformanceObject
        {
            [JsonPropertyName("systemMemoryUsage")]
            public float SystemMemoryUsage { get; set; }

            [JsonPropertyName("processes")]
            public List<ProcessData>? Processes { get; set; }
        }

        public class ProcessData
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("memoryUsage")]
            public float MemoryUsage { get; set; }

        }

        static async Task Main(string[] args)
        {
            const int MB = 1024 * 1024;

            using (PerformanceCounter memoryCounter = new PerformanceCounter("Memory", "Committed Bytes"))
            {
                while (!Console.KeyAvailable)
                {
                    float memUsage = memoryCounter.NextValue();

                    PerformanceObject performanceData = new PerformanceObject
                    {
                        SystemMemoryUsage = memUsage/MB
                    };

                    performanceData.Processes = new List<ProcessData>();

                    // Read every processes' memory consumption
                    foreach (var process in Process.GetProcesses())
                    {
                        ProcessData processData = new ProcessData
                        {
                            Name = process.ProcessName,
                            MemoryUsage = process.WorkingSet64/MB
                        };

                        performanceData.Processes.Add(processData);
                    }

                    string perfData = JsonSerializer.Serialize(performanceData); 

                    await PublishDataAsync(perfData);

                    // Publish the data every 750 milliseconds
                    await Task.Delay(750);

                    Console.WriteLine("Published performance data ... ");
                }
            }

            Console.WriteLine("Done");

        }

        static async Task PublishDataAsync(string perfData)
        {
            using (ClientWebSocket client = new ClientWebSocket())
            {
                byte[] buffer = Encoding.UTF8.GetBytes(perfData);
                Uri server = new Uri("ws://192.168.0.98:8080");
                await client.ConnectAsync(server, CancellationToken.None);
                await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}