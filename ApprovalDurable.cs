using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace OrderApproval_Durable
{
    public static class ApprovalDurable
    {
        [FunctionName("ApprovalDurable")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            double id = 1;
            bool paid = true;
            List<double> products = new List<double>() { 5.99, 15.99, 550.99, 14.99, 18.50};

            bool approved = await context.CallActivityAsync<bool>("VerifyOrder", paid);

            if(approved)
            {
                outputs.Add(await context.CallActivityAsync<string>("TotalValue", products));

                outputs.Add(await context.CallActivityAsync<string>("ApproveOrder", id));
            }
            else
            {
                outputs.Add("Pedido não foi aprovado!");
            }
            return outputs;
        }

        [FunctionName("TotalValue")]
        public static string TotalValue([ActivityTrigger] List<double> products, ILogger log)
        {
            double total = 0;
            for(int i = 0; i < products.Count(); i++)
            {
                total = total + products[i];
            }
            log.LogInformation("Somando total do pedido.");
            return $"Valor total do pedido: R${total}!";
        }

        [FunctionName("VerifyOrder")]
        public static bool VerifyOrder([ActivityTrigger] bool paid, ILogger log)
        {
            log.LogInformation("Verificando se o pedido foi pago!");

            if (paid)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [FunctionName("ApproveOrder")]
        public static string ApproveOrder([ActivityTrigger] int orderId, ILogger log)
        {
            var date = DateTime.UtcNow.ToLocalTime();

            log.LogInformation("Aprovando pedido {orderId}.", orderId);
            return $"Pedido com Id {orderId} foi aprovado com sucesso {date}!";
        }

        [FunctionName("ApprovalDurable_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("ApprovalDurable", null);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}