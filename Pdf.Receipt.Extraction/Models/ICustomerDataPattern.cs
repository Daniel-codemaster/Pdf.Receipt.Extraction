using System;
using System.Collections.Generic;
using System.Text;

namespace Pdf.Receipt.Extraction
{
    public class ICustomerDataPattern
    {
        public string TinPattern { get; set; } = string.Empty;
        public string VatPattern { get; set; } = string.Empty;
        public int? LeftBoundary { get; set; }
        public int? RightBoundary { get; set;}
        public string TopBoundary { get; set; } = string.Empty;
        public string BottomBoundary { get; set;} = string.Empty;
        public string? NamePattern {  get; set; }
        public string? AddressPattern { get; set; }
        public string? ProvincePattern { get; set; }
        public string? PhonePattern { get; set; }
        public string? EmailPattern { get; set; }
    }
}
