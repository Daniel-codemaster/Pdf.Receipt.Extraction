using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Pdf.Receipt.Extraction
{
    public class PdfExtractionOptions
    {
        public string LinePattern { get; set; } = string.Empty;
        public ReceiptLineColumnOrder ColumnOrder { get; set; } = null!;
        public IFormatProvider? FormatInfo { get; set; }
        public string? CurrencyPattern { get; set; }
        public string? TypePattern {  get; set; }
        public string? PaymentPattern { get; set; }
        public string? NumberPattern { get; set; }
        public string? InclusivePattern {  get; set; }
        public string? CustomerDataPattern {  get; set; }
        public int? CustomerDataLines { get; set; }
        public string? InvoiceRefPattern {  get; set; }
        public bool IsTaxPercentage { get; set; }
        public bool ExtractHsCodes { get; set; }
    }
}
