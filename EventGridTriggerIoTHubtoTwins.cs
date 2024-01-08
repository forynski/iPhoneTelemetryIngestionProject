using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;
using Azure.Identity;
using Azure.DigitalTwins.Core;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Azure;
using System.Text;
using System.Threading.Tasks;

namespace iPhoneTelemetryIngestionProject
{
    public static class EventGridTriggerIoTHubtoTwins
    {
        private static readonly string ADT_SERVICE_URL = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");

        [FunctionName("EventGridTriggerIoTHubtoTwins")]
        public static async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            if (ADT_SERVICE_URL == null)
            {
                log.LogError("Application setting 'ADT_SERVICE_URL' not set");
                return;
            }

            try
            {
                DefaultAzureCredential defaultAzureCredential = new DefaultAzureCredential();
                DigitalTwinsClient digitalTwinsClient = new DigitalTwinsClient(new Uri(ADT_SERVICE_URL), defaultAzureCredential);
                log.LogInformation($"ADT service client connection created.");

                if (eventGridEvent != null && eventGridEvent.Data != null)
                {
                    log.LogInformation(eventGridEvent.Data.ToString());
                    JObject eventGridDataJObject = (JObject)JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());
                    string iotHubConnectionDeviceId = (string)eventGridDataJObject["systemProperties"]["iothub-connection-device-id"];

                    if (iotHubConnectionDeviceId.Equals("iPhone509"))
                    {
                        // Base64 decode the payload string
                        string base64EncodedPayload = (string)eventGridDataJObject["body"];
                        string payload = Encoding.UTF8.GetString(Convert.FromBase64String(base64EncodedPayload));

                        // Extract values from the payload string
                        var accelerometerValues = ExtractAccelerometerValues(payload);

                        // Process accelerometer values
                        try
                        {
                            if (accelerometerValues != null)
                            {
                                log.LogInformation($"Accelerometer values: X={accelerometerValues.X}, Y={accelerometerValues.Y}, Z={accelerometerValues.Z}");

                                // Create a JSON patch document with the extracted values
                                JsonPatchDocument azureJsonPatchDocument = new JsonPatchDocument();
                                azureJsonPatchDocument.AppendAdd("/x", accelerometerValues.X);
                                azureJsonPatchDocument.AppendAdd("/y", accelerometerValues.Y);
                                azureJsonPatchDocument.AppendAdd("/z", accelerometerValues.Z);

                                // Update the Digital Twin
                                await digitalTwinsClient.UpdateDigitalTwinAsync("iPhone509Twin", azureJsonPatchDocument);
                            }
                            else
                            {
                                log.LogError("Accelerometer values are null. Check payload format.");
                            }
                        }
                        catch (Exception ex)
                        {
                            log.LogError($"Error processing accelerometer values: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error in ingest function: {ex.Message}");
            }
        }

        // Helper function to extract accelerometer values from the payload string
        private static AccelerometerValues ExtractAccelerometerValues(string payload)
        {
            // Example payload: "X: -0.0283660888671875, Y: -0.905120849609375, Z: -0.4291839599609375"
            var values = payload.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (values.Length == 3)
            {
                return new AccelerometerValues
                {
                    X = ParseValue(values[0]),
                    Y = ParseValue(values[1]),
                    Z = ParseValue(values[2])
                };
            }

            // Return default values or handle the error as needed
            return new AccelerometerValues();
        }

        // Helper function to parse accelerometer values from the payload string
        private static double ParseValue(string valueString)
        {
            var parts = valueString.Split(':', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2 && double.TryParse(parts[1].Trim(), out double result))
            {
                return result;
            }

            // Return default value or handle the error as needed
            return 0.0;
        }

        // Data structure to hold accelerometer values
        private class AccelerometerValues
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
        }
    }
}
