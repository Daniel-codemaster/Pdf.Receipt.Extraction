using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Pdf.Receipt.Extraction
{
    public class ReceiptPdfConfig
    {
        public Guid SystemVersionId { get; set; }

        public string LinePattern { get; set; } = null!;

        public string ColumnOrderJson { get; set; } = null!;

        public string? ReceiptTypePattern { get; set; }

        public string? ReceiptNumberPattern { get; set; }

        public string? CurrencyPattern { get; set; }

        public string? InclusivePattern { get; set; }

        public string? PaymentPattern { get; set; }

        public string? CustomerDataPattern { get; set; }

        public string? InvoiceRefPattern { get; set; }

        public bool IsTaxPercentage { get; set; }
        public ReceiptLineColumnOrder? ColumnOrder => ColumnOrderJson == null ? null : JsonConvert.DeserializeObject<ReceiptLineColumnOrder>(ColumnOrderJson);

    }
}
