using System;
using System.Collections.Generic;

namespace My_Api
{
    public partial class Stock
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public int StockLevel { get; set; }
        public string Currency { get; set; }
        public double UnitPrice { get; set; }
        public string Description { get; set; }
    }
}
