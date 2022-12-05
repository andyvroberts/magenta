using System.Collections.Generic;
using LandReg.Models;
using System.Linq;

namespace LandReg.Converters
{
    public static class Mappers
    {
        public static List<PriceResult> DataToPrices(this PriceData pd)
        {
            List<PriceResult> priceList = new();

            List<string> prices = pd.Prices.Split(',').ToList();
            foreach(string p in prices)
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
    }
}