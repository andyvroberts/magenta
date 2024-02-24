using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;
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
        [FunctionName("GetPrices")]
        public static async Task<IActionResult> GetPriceList(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "postcode/{startsWith}")] HttpRequest req,
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
        /// Perform a scan for price records.
        /// Allow speification of return formats (JSON = default | CSV | XML)
        /// </summary>
        [FunctionName("GetPricesFormatted")]
        public static async Task<IActionResult> GetPriceListFormatted(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "postcode/{startsWith}/{format}")] HttpRequest req,
            [Table("LandregPrice", Connection = "LandregDataStorage")] TableClient priceTable,
            ILogger log,
            string startsWith, string format)
        {
            int rowsPerPage = 1000;
            int maxPageCount = 4;

            string resultFormat = format.ToUpper();
            string partKey = startsWith.Split(' ')[0].ToUpper();
            string startScan = startsWith.ToUpper();
            string endScan = startScan.Substring(0, startScan.Length - 1) + (char)(startScan.Last() + 1);

            // use ODATA filter syntax.
            var queryFilter = $"(PartitionKey eq '{partKey}') and ((RowKey ge '{startScan}') and (RowKey lt '{endScan}'))";
            log.LogInformation($"Search Filter = {queryFilter}");

            AsyncPageable<PriceData> queryResults =
                priceTable.QueryAsync<PriceData>(filter: queryFilter, maxPerPage: rowsPerPage);

            List<PriceResult> returnData = await TableQueryPagination(log, maxPageCount, queryResults);

            if (returnData.Any())
            {
                switch (resultFormat)
                {
                    case "CSV":
                        {
                            byte[] dataBytes = Encoding.UTF8.GetBytes(returnData.ToCsv());
                            return new FileContentResult(dataBytes, "text/csv")
                            {
                                FileDownloadName = $"LandregPrices.{startsWith}.csv"
                            };
                        }
                    default:
                        return new OkObjectResult(returnData);
                }
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
