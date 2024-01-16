using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class MyParameters
{
    public int ServiceId { get; set; }
    public string VehicleName { get; set; }
    public decimal ServicePrice { get; set; }
    public string Email { get; set; }
    public bool Paid { get; set; }
}

namespace OrderApproval_Durable
{
    public static class ApprovalDurable
    {
        [FunctionName("ApprovalDurable")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            var inputs = context.GetInput<MyParameters>();

            bool approved = await context.CallActivityAsync<bool>("VerifyOrder", inputs);

            if(approved)
            {
                outputs.Add(await context.CallActivityAsync<string>("TotalValue", inputs));

                outputs.Add(await context.CallActivityAsync<string>("ApproveOrder", inputs));
            }
            else
            {
                outputs.Add("Pedido n�o foi aprovado!");
            }
            return outputs;
        }

        [FunctionName("TotalValue")]
        public static string TotalValue([ActivityTrigger] MyParameters inputs, ILogger log)
        {
            //List<double> products = inputs.ServicePrice;
            //double total = 0;
            //for(int i = 0; i < inputs.Products.Count(); i++)
            //{
            //    total = total + inputs.Products[i];
            //}
            log.LogInformation("Somando total do pedido.");
            return $"Valor total do pedido: R${Math.Round(inputs.ServicePrice,2)}!";
        }

        [FunctionName("VerifyOrder")]
        public static bool VerifyOrder([ActivityTrigger] MyParameters inputs, ILogger log)
        {
            log.LogInformation("Verificando se o pedido foi pago!");

            if (inputs.Paid)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [FunctionName("ApproveOrder")]
        public static string ApproveOrder([ActivityTrigger] MyParameters inputs, ILogger log)
        {
            var date = DateTime.UtcNow.ToLocalTime();

            log.LogInformation("Aprovando pedido {orderId}.", inputs.ServiceId);

            string messageResult = $"Pedido com Id {inputs.ServiceId}, do ve�culo {inputs.VehicleName} foi aprovado com sucesso {date}!";

            return messageResult;
        }

        [FunctionName("HttpStart_OrderApproval")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger log)
        {
            string requestBody = await req.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(requestBody))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Please insert order data to be approved.")
                };
            }

            MyParameters input = JsonConvert.DeserializeObject<MyParameters>(requestBody);

            string instanceId = await client.StartNewAsync("ApprovalDurable", input);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return client.CreateCheckStatusResponse(req, instanceId);
        }

    }
}