// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
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
using System.Threading.Tasks;

namespace iPhoneTelemetryIngestionProject
{
    public static class EventGridTriggerIoTHubtoTwins
    {
        private static readonly string ADT_SERVICE_URL = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");
        [FunctionName("EventGridTriggerIoTHubtoTwins")]
        public static async Task Run([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
            if (ADT_SERVICE_URL == null) log.LogError("Application setting 'ADT_SERVICE_URL' not set");
            else
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
                            JsonPatchDocument azureJsonPatchDocument = new JsonPatchDocument();
                            //azureJsonPatchDocument.AppendAdd("/x", Convert.ToDouble(eventGridDataJObject["body"]["speed_scaling"]));
                            //azureJsonPatchDocument.AppendAdd("/y", Convert.ToDouble(eventGridDataJObject["body"]["actual_momentum"]));
                            //azureJsonPatchDocument.AppendAdd("/z", Convert.ToDouble(eventGridDataJObject["body"]["actual_main_voltage"]));
                            azureJsonPatchDocument.AppendAdd("/x", GetRandomNumber(1,10));
                            azureJsonPatchDocument.AppendAdd("/y", GetRandomNumber(11,20));
                            azureJsonPatchDocument.AppendAdd("/z", GetRandomNumber(21,30));

                            await digitalTwinsClient.UpdateDigitalTwinAsync("iPhone509Twin", azureJsonPatchDocument);
                            
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.LogError($"Error in ingest function: {ex.Message}");
                }
        }
        public static double GetRandomNumber(double minimum, double maximum)
        {
            Random random = new Random();
            return random.NextDouble() * (maximum - minimum) + minimum;
        }
    }
}