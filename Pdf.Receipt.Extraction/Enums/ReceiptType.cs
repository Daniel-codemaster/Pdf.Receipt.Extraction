using System;
using System.Collections.Generic;
using System.Text;

namespace Pdf.Receipt.Extraction
{
    public enum ReceiptType : int
    {
        FiscalInvoice = 0,
        CreditNote = 1,
        DebitNote = 2,
    }
}
