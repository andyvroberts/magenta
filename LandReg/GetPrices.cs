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
        /// <summary>
        /// Perform a scan for price records.
        /// The http request value must be at least a partial postcode with the complete outcode.
        /// </summary>
        [FunctionName("GetPricesByScan")]
        public static async Task<IActionResult> GetPriceListByScan(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "postcode/scan/{startsWith}")] HttpRequest req,
            [Table("LandregPrice", Connection = "LandregDataStorage")] TableClient priceTable,
            ILogger log,
            string startsWith)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var source = new System.Threading.CancellationTokenSource();
            var ct = source.Token;
            int maxPageCount = 4;

            string partKey = startsWith.Split(' ')[0].ToUpper();
            string startScan = startsWith.ToUpper();
            string endScan = startScan.Substring(0, startScan.Length - 1) + (char)(startScan.Last() + 1);

            // use ODATA filter syntax.
            var queryFilter = $"(PartitionKey eq '{partKey}') and ((RowKey ge '{startScan}') and (RowKey lt '{endScan}'))";
            log.LogInformation($"Search Filter = {queryFilter}");

            AsyncPageable<PriceData> queryResults =
                priceTable.QueryAsync<PriceData>(
                    filter: queryFilter,
                    maxPerPage: 500,
                    cancellationToken: ct
                );

            List<PriceResult> returnData = await TableQueryPagination(log, maxPageCount, queryResults);

            if (returnData.Any())
            {
                return new OkObjectResult(returnData);
            }
            else
            {
                return new NotFoundObjectResult($"No records found in Outcode partition of {priceTable.Name}.");
            }
        }

        /// <summary>
        /// Perform an exact postcode seach for price records.
        /// </summary>
        [FunctionName("GetPricesByPostcode")]
        public static async Task<IActionResult> GetPriceListByPostcode(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "postcode/{pCode}")] HttpRequest req,
            [Table("LandregPrice", Connection = "LandregDataStorage")] TableClient priceTable,
            ILogger log,
            string pCode)
        {
            int rowsPerPage = 500;
            int maxPageCount = 2;

            string partKey = pCode.Split(' ')[0].ToUpper();
            string rowKey = pCode.ToUpper();
            log.LogInformation($"Search partition key = {partKey} and row key = {rowKey}.");

            AsyncPageable<PriceData> queryResults =
                priceTable.QueryAsync<PriceData>(x => x.PartitionKey == partKey && x.RowKey == rowKey, maxPerPage: rowsPerPage);

            List<PriceResult> returnData = await TableQueryPagination(log, maxPageCount, queryResults);

            if (returnData.Any())
            {
                return new OkObjectResult(returnData);
            }
            else
            {
                return new NotFoundObjectResult($"No records found in Outcode partition of {priceTable.Name}.");
            }
        }

        /// <summary>
        /// The common pagination method for accessing the Azure table service.
        /// </summary>
        private static async Task<List<PriceResult>> TableQueryPagination(ILogger log, int maxPageCount, AsyncPageable<PriceData> queryResults)
        {
            List<PriceResult> returnData = new();
            int pageCounter = 0;

            await foreach (Page<PriceData> pricePage in queryResults.AsPages())
            {
                pageCounter++;
                foreach (PriceData landregPrice in pricePage.Values)
                {
                    returnData.AddRange(landregPrice.DataToPrices());
                }

                if (pageCounter == maxPageCount)
                {
                    log.LogInformation($"Maximum pages reached {pageCounter}.");
                    break;
                }
            }
            log.LogInformation($"Retrieved {returnData.Count} Localities from the data store.");
            return returnData;
        }
    }
}
