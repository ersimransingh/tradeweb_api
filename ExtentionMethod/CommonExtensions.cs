using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace TradeWeb.API.ExtentionMethod
{
    public static class CommonExtensions
    {
        public static double ToDouble<T>(this T val, double defaultValue = 0)
        {
            double returnValue = defaultValue;
            try
            { returnValue = Convert.ToDouble(val); }
            catch { returnValue = defaultValue; }
            return returnValue;
        }

        public static int ToInt<T>(this T val, int defaultValue = 0)
        {
            int returnValue = defaultValue;
            try
            { returnValue = Convert.ToInt32(val); }
            catch { returnValue = defaultValue; }
            return returnValue;
        }

        public static long ToLong<T>(this T val, long defaultValue = 0)
        {
            long returnValue = defaultValue;
            try
            { returnValue = Convert.ToInt64(val); }
            catch { returnValue = defaultValue; }
            return returnValue;
        }

        public static Boolean ToBoolean<T>(this T val, Boolean defaultValue = false)
        {
            Boolean returnValue = defaultValue;
            try
            { returnValue = Convert.ToBoolean(val); }
            catch { returnValue = defaultValue; }
            return returnValue;
        }

        public static List<T> ConvertToList<T>(DataTable dt)
        {
            var columnNames = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName.ToLower()).ToList();
            var properties = typeof(T).GetProperties();
            return dt.AsEnumerable().Select(row =>
            {
                var objT = Activator.CreateInstance<T>();
                foreach (var pro in properties)
                {
                    if (columnNames.Contains(pro.Name.ToLower()))
                    {
                        try
                        {
                            pro.SetValue(objT, row[pro.Name]);
                        }
                        catch (Exception ex) { }
                    }
                }
                return objT;
            }).ToList();
        }
    }
}
