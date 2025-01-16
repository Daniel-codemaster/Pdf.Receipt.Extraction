using System;
using System.Collections.Generic;
using System.Text;

namespace Pdf.Receipt.Extraction
{
    public enum MoneyType : int
    {
        Cash = 0,
        Card = 1,
        MobileWallet = 2,
        Coupon = 3,
        Credit = 4,
        BankTransfer = 5,
        Other = 6,
    }
}
