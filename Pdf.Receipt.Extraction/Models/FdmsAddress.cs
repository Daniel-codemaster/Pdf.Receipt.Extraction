using System;
using System.Collections.Generic;
using System.Text;

namespace Pdf.Receipt.Extraction
{
    public class FdmsAddress
    {
        public string Province { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string HouseNo { get; set; } = string.Empty;
    }
}
