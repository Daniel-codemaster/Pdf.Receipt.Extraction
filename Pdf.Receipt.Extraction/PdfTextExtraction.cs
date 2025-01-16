using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace Pdf.Receipt.Extraction
{
    public class PdfTextExtraction
    {
        public static PdfReceipt? ProcessPdf(byte[] fileAsByteArray, PdfExtractionOptions options)
        {
            using (MemoryStream ms = new MemoryStream(fileAsByteArray))
            using (PdfReader pdfReader = new PdfReader(ms))
            {
                using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
                {
                    string extractedText = string.Empty;
                    PdfPage? page1 = null;
                    for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                    {
                        PdfPage page = pdfDocument.GetPage(pageNum);
                        ITextExtractionStrategy strategy = new LocationTextExtractionStrategy();
                        string text = PdfTextExtractor.GetTextFromPage(page, strategy);

                        var customStrategy = new CustomLocationTextExtractionStrategy();
                        PdfTextExtractor.GetTextFromPage(page, customStrategy);

                        var t = customStrategy.GetResultandText();

                        extractedText += text;

                        if(pageNum == 1) page1 = page;
                    }                 
                    return ExtractReceipt(extractedText, options, page1);                        
                }
            }
        }
    
        static PdfReceipt? ExtractReceipt(string text, PdfExtractionOptions options, PdfPage? page1)
        {
            List<PdfReceiptLine> receiptItems = new List<PdfReceiptLine>();
            string? receiptNo = null;
            string? invoiceRef = null;
            CurrencyType? currencyType = null;
            ReceiptType? receiptType = null;
            bool? inclusive = null;

            if (!string.IsNullOrWhiteSpace(options.TypePattern) && receiptType == null)
            {
                receiptType = ExtractReceiptType(text, options.TypePattern!);
            }
            if (!string.IsNullOrWhiteSpace(options.NumberPattern) && string.IsNullOrWhiteSpace(receiptNo) && receiptType != null)
            {
                receiptNo = ExtractNumber(text, options.NumberPattern!, (ReceiptType)receiptType);
            }
            if (!string.IsNullOrWhiteSpace(options.CurrencyPattern) && currencyType == null)
            {
                currencyType = ExtractCurrency(text, options.CurrencyPattern!);
            }

            List<string> customerRawDataLines = new List<string>();
            FdmsBuyer? fdmsBuyer = null;
            if (!string.IsNullOrWhiteSpace(options.CustomerDataPattern) && page1 != null)
            {
                var res = ExtractCustomerData(page1, options.CustomerDataPattern!);

                customerRawDataLines = res.rawDataLines;
                fdmsBuyer = res.fdmsBuyer;
            }
            if (!string.IsNullOrWhiteSpace(options.InvoiceRefPattern) && string.IsNullOrWhiteSpace(invoiceRef))
            {
                var match = Regex.Match(text, options.InvoiceRefPattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    invoiceRef = match.Groups[1].Value;
                }
            }

            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var count = 0;
            int? prevMatchedLineIndex = null;
            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(options.InclusivePattern) && inclusive == null)
                {
                    inclusive = ExtractTaxInclusive(line, options.InclusivePattern!);
                }
                
                // Match line
                Match lineMatch = Regex.Match(line.Trim(), options.LinePattern);
                if (lineMatch.Success)
                {
                    string name = "";
                    string? hsCode = null;
                    if (options.ColumnOrder.Description == -1) 
                    {
                        name = lines[count + 1];
                    }
                    else if(options.ColumnOrder.Description == -2)
                    {
                        name = lines[count -1];
                        if (options.ExtractHsCodes)
                        {
                            var hsMatch = Regex.Match(name, @"\d{4,8}\b");
                            if (hsMatch.Success)
                            {
                                hsCode = hsMatch.Groups[0].Value;
                            }
                        }
                    }
                    else if (options.ColumnOrder.Description == -3)
                    {
                        if (prevMatchedLineIndex != null)
                        {
                            for(int i = (prevMatchedLineIndex.Value+1); i<count; i++)
                            {
                                name += lines[i];
                            }
                        }
                        else
                        {
                            name = lines[count - 2] + lines[count - 1];
                        }
                        if (options.ExtractHsCodes)
                        {
                            var hsMatch = Regex.Match(name, @"\d{4,8}\b");
                            if (hsMatch.Success)
                            {
                                hsCode = hsMatch.Groups[0].Value;
                            }
                        }
                    }
                    else {                    
                        name = lineMatch.Groups[options.ColumnOrder.Description].Value;
                        if (options.ExtractHsCodes)
                        {
                            var hsMatch = Regex.Match(name, @"\d{4,8}\b");
                            if (hsMatch.Success)
                            {
                                hsCode = hsMatch.Groups[0].Value;
                            }
                        }
                    }
                    decimal price = options.ColumnOrder.Price < 0 ? 0 : decimal.Parse(lineMatch.Groups[options.ColumnOrder.Price].Value.Trim().Replace(" ", ""), NumberStyles.Number, options.FormatInfo);
                    decimal qty = options.ColumnOrder.Quantity < 0 ? 1 : decimal.Parse(lineMatch.Groups[options.ColumnOrder.Quantity].Value.Trim().Replace(" ", ""), NumberStyles.Number, options.FormatInfo);

                    price = Math.Round(price, 2, MidpointRounding.AwayFromZero);
                    qty = Math.Round(qty, 2, MidpointRounding.AwayFromZero);

                    decimal totalAmount;
                    if (options.ColumnOrder.Total >= 0)
                    {
                        totalAmount = decimal.Parse(lineMatch.Groups[options.ColumnOrder.Total].Value.Trim().Replace(" ", ""), NumberStyles.Number, options.FormatInfo);
                    }
                    else
                    {
                        totalAmount = qty * price;
                        totalAmount = totalAmount < 0 ? totalAmount * -1 : totalAmount;
                    }
                    totalAmount = Math.Round(totalAmount, 2, MidpointRounding.AwayFromZero);

                    bool isTaxPercentage = true;
                    decimal? taxPercentage = null;
                    if (options.ColumnOrder.TaxPercentage >= 0)
                    {
                        var val = lineMatch.Groups[options.ColumnOrder.TaxPercentage].Value;
                        isTaxPercentage = decimal.TryParse(val, NumberStyles.Number, options.FormatInfo, out decimal perc);
                        if (isTaxPercentage)
                        {
                            taxPercentage = perc;
                        }
                        else
                        {
                            if (val == "0")
                            {
                                taxPercentage = 0;
                                isTaxPercentage = true;
                            }
                            else if (val == "T")
                            {
                                taxPercentage = 15;
                                isTaxPercentage = true;
                            }
                            else if(val == "VT")
                            {
                                taxPercentage = 15;
                                isTaxPercentage = true;
                            }
                            else if(val == "NV")
                            {
                                taxPercentage = 0;
                                isTaxPercentage = true;
                            }
                        }
                    }
                  
                    //Default -1
                    else
                    {
                        taxPercentage = 15;
                    }

                    // In case unit price is not given
                    if (options.ColumnOrder.Price == -2)
                    {
                        price = Math.Round(totalAmount / qty, 2, MidpointRounding.AwayFromZero);
                    }

                    // In case price is less than 0 i.e. negative prices of some credit notes
                    if(price < 0) price *= -1;

                    var item = new PdfReceiptLine
                    {
                        Description = name,
                        Quantity = qty < 0 ? qty * -1 : qty,
                        Price = options.ColumnOrder.Price == -1 ? totalAmount : price,
                        Total = totalAmount,
                        TaxPercentage = isTaxPercentage ? taxPercentage : null,
                        HsCode = hsCode
                    };
                    receiptItems.Add(item);
                    prevMatchedLineIndex = count;
                }
                count++;
            }

            if (string.IsNullOrWhiteSpace(receiptNo) || currencyType == null ||
            receiptType == null || inclusive == null || receiptItems.Count == 0)
            {
                return null;
            }
            if (receiptType != ReceiptType.FiscalInvoice && string.IsNullOrWhiteSpace(invoiceRef) && !string.IsNullOrWhiteSpace(options.InvoiceRefPattern)) return null;

            // update tax percantage if tax is not percentage
            if (!options.IsTaxPercentage)
            {
                foreach (var line in receiptItems)
                {
                    var lineTotal = line.Quantity * line.Price;
                    var taxAmount = (decimal)line.TaxPercentage!;
                    decimal taxPerc;
                    if ((bool)inclusive)
                    {
                        taxPerc = 100 * taxAmount / (lineTotal - taxAmount);
                    }
                    else
                    {
                        taxPerc = (taxAmount * 100) / lineTotal;
                    }
                    line.TaxPercentage = Math.Round(taxPerc, 2);
                }
            }
            return new PdfReceipt
            {
                LineTaxInclusive = (bool)inclusive,
                ReceiptLines = receiptItems,
                Number = receiptNo!,
                CurrencyType = (CurrencyType)currencyType,
                ReceiptType = (ReceiptType)receiptType,
                InvoiceRef = invoiceRef,
                CustomerRawDataLines = customerRawDataLines,
                FdmsBuyer = fdmsBuyer
            };
        }
        static string? ExtractNumber(string text, string pattern, ReceiptType type)
        {
            var match = Regex.Match(text, pattern);
            if (match.Success)
            {
                Regex regex = new Regex(pattern);
                if ((regex.GetGroupNames().Length - 1) > 1)
                {
                    if (!string.IsNullOrWhiteSpace(match.Groups[3].Value) && type == ReceiptType.DebitNote) return match.Groups[3].Value;
                    if (!string.IsNullOrWhiteSpace(match.Groups[2].Value) && type == ReceiptType.CreditNote) return match.Groups[2].Value;
                    if (!string.IsNullOrWhiteSpace(match.Groups[1].Value) && type == ReceiptType.FiscalInvoice) return match.Groups[1].Value;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(match.Groups[1].Value)) return match.Groups[1].Value;
                }                
            }
            return null;
        }
        static CurrencyType? ExtractCurrency(string line, string pattern)
        {
            if (pattern == "Default-USD") return CurrencyType.USD;
            var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var val = match.Groups[1].Value.Trim().ToLower();

                var zwgSymbols = new List<string>{"zig", "zwg", "$"};
                var usdSymbols = new List<string>{"us", "us$", "home currency"};
                var randSymbols = new List<string>{"r", "rand", "zar", "rands"};

                if (zwgSymbols.Contains(val)) val = "ZWG";
                if (usdSymbols.Contains(val)) val = "USD";
                if (randSymbols.Contains(val)) val = "ZAR";

                if (pattern == @"([A-Za-z]{1,3}|\$)(?:\d{1,3},)*\d{1,3}\.\d{2}\s+Total" && match.Groups[1].Value.Trim() == "$") val = "USD";

                if (Enum.TryParse<CurrencyType>(val, true, out var currencyType)) return currencyType;
            }
            return null;
        }
        static ReceiptType? ExtractReceiptType(string text, string pattern)
        {
            if (pattern == "Default-FiscalInvoice") return ReceiptType.FiscalInvoice;
            var formattedText = text.Replace("\n", " ");
            var match = Regex.Match(formattedText, pattern);
            if (match.Success)
            {
                if (!string.IsNullOrWhiteSpace(match.Groups[1].Value)) return ReceiptType.FiscalInvoice;
                if (!string.IsNullOrWhiteSpace(match.Groups[2].Value)) return ReceiptType.CreditNote;
                if (!string.IsNullOrWhiteSpace(match.Groups[3].Value)) return ReceiptType.DebitNote;
            }
            return null;
        }
        static bool? ExtractTaxInclusive(string line, string pattern)
        {
            if (pattern == "Default-Inclusive") return true;
            if (pattern == "Default-Exclusive") return false;
            var match = Regex.Match(line, pattern);
            if (match.Success)
            {
                if (!string.IsNullOrWhiteSpace(match.Groups[1].Value)) return true;
                if (!string.IsNullOrWhiteSpace(match.Groups[2].Value)) return false;
            }
            return null;
        }
        static (List<string> rawDataLines, FdmsBuyer? fdmsBuyer) ExtractCustomerData(PdfPage page, string pattern)
        {
            FdmsBuyer? buyer = null;
            List<string> customerDataLines = new List<string>();
            var customerPtn = JsonConvert.DeserializeObject<ICustomerDataPattern>(pattern, JsonHelper.Options);
            if (customerPtn != null)
            {
                var customStrategy = new CustomLocationTextExtractionStrategy();
                PdfTextExtractor.GetTextFromPage(page, customStrategy);

                customerDataLines = customStrategy.GetCustomerDataLines(customerPtn.LeftBoundary, customerPtn.RightBoundary, customerPtn.TopBoundary, customerPtn.BottomBoundary);
                
                if (customerDataLines.Count > 0)
                {
                    buyer = new FdmsBuyer();
                    if(string.IsNullOrWhiteSpace(customerPtn.NamePattern))
                    {
                        buyer.BuyerRegisterName = customerDataLines[0];                   
                    };

                    if (!string.IsNullOrWhiteSpace(customerPtn.PhonePattern) && !string.IsNullOrWhiteSpace(customerPtn.EmailPattern))
                    {
                        var contact = new FdmsContact();
                        foreach (var line in customerDataLines)
                        {                            
                            var match = Regex.Match(line.Trim(), customerPtn.PhonePattern);                           
                            if (match.Success)
                            {
                                contact.PhoneNo = match.Groups[1].Value;
                                continue;
                            }
                            match = Regex.Match(line.Trim(), customerPtn.EmailPattern);
                            if (match.Success)
                            {
                                contact.Email = match.Groups[1].Value;
                                continue;
                            }                            
                        }
                        if(!string.IsNullOrWhiteSpace(contact.Email) && !string.IsNullOrWhiteSpace(contact.PhoneNo))
                        {
                            buyer.BuyerContacts = contact;
                        }
                    }

                    foreach (var line in customerDataLines)
                    {
                        var match = Regex.Match(line.Trim(), customerPtn.TinPattern);
                        if(match.Success)
                        {
                            buyer.BuyerTIN = match.Groups[1].Value;
                            continue;
                        }
                        match = Regex.Match(line.Trim(), customerPtn.VatPattern);
                        if (match.Success)
                        {
                            buyer.VATNumber = match.Groups[1].Value;
                            continue;
                        }
                        if (!string.IsNullOrWhiteSpace(customerPtn.NamePattern))
                        {
                            match = Regex.Match(line.Trim(), customerPtn.NamePattern);
                            if (match.Success)
                            {
                                buyer.BuyerRegisterName = match.Groups[1].Value;
                                continue;
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(customerPtn.AddressPattern))
                        {
                            match = Regex.Match(line.Trim(), customerPtn.AddressPattern);
                            if (match.Success)
                            {
                                var lineIndex = customerDataLines.IndexOf(line);
                                var len = customerDataLines.Count;
                                if (lineIndex + 1 < len && lineIndex + 2 < len)
                                {
                                    buyer.BuyerAddress = new FdmsAddress 
                                    { 
                                        HouseNo = match.Groups[1].Value,
                                        Street = customerDataLines[lineIndex + 1],
                                        City = customerDataLines[lineIndex + 2],
                                    };
                                }
                                continue;
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(customerPtn.ProvincePattern) && buyer.BuyerAddress != null)
                        {
                            match = Regex.Match(line.Trim(), customerPtn.ProvincePattern);
                            if (match.Success)
                            {                                                         
                                buyer.BuyerAddress.Province = match.Groups[1].Value;                                
                            }
                        }
                    }
                    if (buyer.BuyerAddress != null && string.IsNullOrWhiteSpace(buyer.BuyerAddress.Province))
                    {
                        buyer.BuyerAddress = null;
                    }
                    if (string.IsNullOrWhiteSpace(buyer.BuyerRegisterName) || string.IsNullOrWhiteSpace(buyer.BuyerTIN)) buyer = null;
                }
            }
            return (customerDataLines, buyer);
        }
    }
}
