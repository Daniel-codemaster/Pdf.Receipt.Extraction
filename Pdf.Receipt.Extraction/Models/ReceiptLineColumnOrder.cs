using System;
using System.Collections.Generic;
using System.Text;

namespace Pdf.Receipt.Extraction
{
    public class ReceiptLineColumnOrder
    {
        public ReceiptLineColumnOrder(int desc, int price, int qty, int taxPerc, int total) 
        {
            Description = desc;
            Price = price;
            Quantity = qty;
            TaxPercentage = taxPerc;
            Total = total;
        }
        public ReceiptLineColumnOrder() { }
        public int Description { get; set; }
        public int Price { get; set; }
        public int Quantity { get; set; }
        public int TaxPercentage { get; set; }
        public int Total {  get; set; }
    }
}
