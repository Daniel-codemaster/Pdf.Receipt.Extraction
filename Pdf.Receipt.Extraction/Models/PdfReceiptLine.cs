using System;
using System.Collections.Generic;
using System.Text;

namespace Pdf.Receipt.Extraction
{
    public class PdfReceiptLine
    {
        public string Description { get; set; } = string.Empty;
        public decimal Price {  get; set; }
        public decimal Quantity { get; set; }
        public decimal Total { get; set; }
        public decimal? TaxPercentage { get; set; }
        public string? HsCode { get; set; }
    }
}
