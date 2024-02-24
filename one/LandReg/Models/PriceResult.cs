using System;

namespace LandReg.Models
{
    public class PriceResult
    {
        public string Postcode { get; set; }
        public string Address { get; set; }
        public string Date { get; set; }
        public int Price { get; set; }
        public string Locality { get; set; }
    }
}