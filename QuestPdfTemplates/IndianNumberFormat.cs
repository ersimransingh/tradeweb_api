using System;
using System.Globalization;
using System.Text;

namespace TradeWeb.API.QuestPdfTemplates
{
    // tradewebx formats every on-screen number (KPI values, table cells, chart axes/labels)
    // via Number.toLocaleString('en-IN', ...) - Indian digit grouping (lakh/crore), with 2
    // decimals only when the value has a fractional part. .NET can't use CultureInfo("en-IN")
    // here because the app runs with DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true, so this is
    // hand-rolled to match that exact behavior.
    public static class IndianNumberFormat
    {
        public static string Format(double value, int? decimalPlaces = null)
        {
            bool negative = value < 0;
            value = Math.Abs(value);

            int decimals = decimalPlaces ?? (value == Math.Floor(value) ? 0 : 2);
            string fixedStr = value.ToString("F" + decimals, CultureInfo.InvariantCulture);

            var parts = fixedStr.Split('.');
            string intPart = parts[0];
            string decPart = parts.Length > 1 ? parts[1] : null;

            string result;
            if (intPart.Length <= 3)
            {
                result = intPart;
            }
            else
            {
                string lastThree = intPart.Substring(intPart.Length - 3);
                string other = intPart.Substring(0, intPart.Length - 3);
                var sb = new StringBuilder();

                for (int i = 0; i < other.Length; i++)
                {
                    if (i > 0 && (other.Length - i) % 2 == 0)
                        sb.Append(',');
                    sb.Append(other[i]);
                }

                result = sb.ToString() + "," + lastThree;
            }

            if (decPart != null)
                result += "." + decPart;

            return (negative ? "-" : "") + result;
        }
    }
}
