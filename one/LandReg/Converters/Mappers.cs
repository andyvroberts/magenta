using System.Collections.Generic;
using LandReg.Models;
using System.Linq;
using System.Text;

namespace LandReg.Converters
{
    public static class Mappers
    {
        public static List<PriceResult> DataToPrices(this PriceData pd)
        {
            List<PriceResult> priceList = new();

            List<string> prices = pd.Prices.Split(',').ToList();
            foreach (string p in prices)
            {
                string[] priceParts = p.Split('~');

                PriceResult pr = new();
                pr.Postcode = pd.Postcode;
                pr.Address = pd.Address;
                pr.Locality = pd.Locality;
                pr.Price = int.Parse(priceParts[1]);
                pr.Date = priceParts[0];

                priceList.Add(pr);
            }

            return priceList;
        }

        public static string ToCsv<T>(this IEnumerable<T> recs)
            where T : class
        {
            var csvBuilder = new StringBuilder();
            var properties = typeof(T).GetProperties();

            // header
            string head = string.Join(",", properties.Select(h => h.Name).ToArray());
            csvBuilder.AppendLine(head);

            // records
            foreach (T item in recs)
            {
                string line = string.Join(",", properties.Select(p => p.GetValue(item, null)).ToArray());
                csvBuilder.AppendLine(line);
            }
            return csvBuilder.ToString();
        }

    }
}