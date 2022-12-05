using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure;
using Azure.Data.Tables;
using System.Collections.Generic;
using System.Linq;

namespace LandReg
{
    using LandReg.Models;
    using LandReg.Converters;

    public static class GetPrices
    {
        [FunctionName("GetPrices")]
        public static async Task<IActionResult> GetPriceList(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "postcode/{startsWith}")] HttpRequest req,
            [Table("LandregPrice", Connection="blah")] TableClient priceTable,
            ILogger log,
            string startsWith)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var source = new System.Threading.CancellationTokenSource();
            var ct = source.Token;

            string partKey = startsWith.Split(' ')[0];
            string startScan = startsWith.ToUpper();
            string endScan = startScan.Substring(0, startScan.Length - 1) + (char)(startScan.Last() + 1);

            // use ODATA filter syntax.
            var queryFilter = $"(PartitionKey eq '{partKey}') and ((RowKey ge '{startScan}') and (RowKey lt '{endScan}'))";
            log.LogInformation($"Search Filter = {queryFilter}");

            AsyncPageable<PriceData> queryResults = 
                priceTable.QueryAsync<PriceData>(
                    filter: queryFilter,
                    maxPerPage: 100,
                    cancellationToken: ct
                );

            List<PriceResult> returnData = new();

            await foreach (Page<PriceData> locPage in queryResults.AsPages())
            {
                foreach (PriceData landregPrice in locPage.Values)
                {
                    returnData.AddRange(landregPrice.DataToPrices());
                }
                
                // note: if you set the cancellation token then entire process aborts, maybe
                // this would be useful to test if it nears an execution duration time limit.
                //source.Cancel();
                
                // limit the request to a single page.
                break;
            }
            log.LogInformation($"Accessed {returnData.Count} Localities from the data store.");

            if (returnData.Any())
            {
                return new OkObjectResult(returnData);
            }
            else
            {
                return new NotFoundObjectResult($"No records found in LOCALITY partition of {priceTable.Name} table");
            }
        }
    }
}
