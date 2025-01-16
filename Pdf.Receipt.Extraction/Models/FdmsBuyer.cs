using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Text;

namespace Pdf.Receipt.Extraction
{
    public class FdmsBuyer
    {
        public string BuyerRegisterName { get; set; } = string.Empty;
        public string? BuyerTradeName { get; set; }
        public string? BuyerTIN { get; set; }
        public string? VATNumber { get; set; }
        public FdmsContact? BuyerContacts { get; set; }
        public FdmsAddress? BuyerAddress { get; set; }
    }
}
