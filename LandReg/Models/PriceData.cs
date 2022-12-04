using System;
using Azure;
using Azure.Data.Tables;

namespace LandReg.Models
{
    public class PriceData : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string Prices { get; set; }
        public string Postcode { get; set; }
        public string Address { get; set; }
        public string Locality { get; set; }
        public string Town { get; set; }
        public string District { get; set; }
        public string County { get; set; }
    }
}