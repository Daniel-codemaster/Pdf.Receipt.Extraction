using System;
using System.Collections.Generic;

namespace Pdf.Receipt.Extraction
{
    public class PdfReceipt
    {
        public List<PdfReceiptLine> ReceiptLines { get; set; } = new List<PdfReceiptLine>();
        public CurrencyType CurrencyType { get; set; }
        public ReceiptType ReceiptType { get; set; }
        public string Number { get; set; } = string.Empty;
        public MoneyType MoneyType { get; set; }
        public bool LineTaxInclusive { get; set; }
        public string? InvoiceRef { get; set; }
        public List<string> CustomerRawDataLines { get; set; } = new List<string>();
        public FdmsBuyer? FdmsBuyer { get; set; }
    }
}
