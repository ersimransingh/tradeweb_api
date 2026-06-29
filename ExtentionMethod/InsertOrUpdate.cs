using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using TradeWeb.API.Models;
using TradeWeb.API.Service;

namespace TradeWeb.API.ExtentionMethod
{
    public class InsertOrUpdate
    {
        //  private readonly UtilityCommon objUtility;
        IblogUtility objUtility = new IblogUtility();
        public InsertOrUpdate()
        {
            //objUtility = utly;
        }

        public int InsertOrUpdateData(Dictionary<string, object> FormDataValues, string conn)
        {
            int insertCount = 0;
            try
            {
                string functionName = FormDataValues["FunctionName"].ToString();
                string inputValue = FormDataValues["Name"].ToString();
                string databaseTableName = FormDataValues["TableName"].ToString();
                string whereCondition = FormDataValues["WhereCondition"].ToString();
                //this line create a new array from string using string split.
                string[] formDataArray = inputValue.Split(new[] { '^' }, StringSplitOptions.None).ToArray();

                //this line create a new dictionary object.
                Dictionary<string, object> dictionaryObject = new Dictionary<string, object>();
                foreach (string formDataIndividualString in formDataArray)
                {
                    //create new array from string
                    string[] keyValueArrayFromIndividualString = formDataIndividualString.Split(new[] { '=' }, StringSplitOptions.None);
                    //add new keyvaluepair to dictionary.
                    dictionaryObject.Add(keyValueArrayFromIndividualString[0], keyValueArrayFromIndividualString[1]);
                }
                if (string.IsNullOrEmpty(whereCondition))
                {
                    insertCount = InsertData(functionName, databaseTableName, dictionaryObject);
                }
                else
                {
                    insertCount = UpdateData(functionName, databaseTableName, dictionaryObject, whereCondition, conn);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {

            }
            return insertCount;
        }


        public int InsertData(string functionName, string strTableName, Dictionary<string, object> values)
        {
            string insertQuery = GenerateSQLStatements.SqlInsert(strTableName, values);
            return Convert.ToInt32(objUtility.SelectSqlExecuteScalar(insertQuery + " Select Scope_Identity()"));
            //return 1;
        }
        public int UpdateData(string functionName, string strTableName, Dictionary<string, object> values, string strWhereCondition, string alchemyConn)
        {
            string updateZoneMasterQuery = GenerateSQLStatements.SqlUpdate(strTableName, values, strWhereCondition);
            //return GenericStatements.UpdateStatement(updateZoneMasterQuery, alchemyConn);
            return 1;
        }

        public dynamic checkPassword(User model)
        {
            string strPass = Encrypt(model.Password);
            string strUser = model.Username;
            string dbPass = "";
            // string sql = "select UserPassword from VW_UsersMaster where UserId=@UserId";
            string sql = "Select UserPassword from tbl_apiusersmaster (nolock) where UserId=@UserId";
            SqlParameter[] sqlPrm = new[]
                       {
                            new SqlParameter("@UserId",model.Username),
                        };
            dbPass = objUtility.Execute_QueryParam(sql, sqlPrm);

            if (dbPass.Equals(strPass))
                return true;
            return false;
        }

        private static string Encrypt(string strenc)
        {
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                int m = 0;
                string strEncKey = null;
                string gsEcDc = null;
                string gsFinal = null;
                string gsCompare = null;
                int glNumber = 0;
                strEncKey = "ASHOKKHE";
                gsFinal = "";
                glNumber = Strings.Len(Strings.Trim(strenc));
                for (m = 1; m <= Math.Round((decimal)glNumber / 8) + 1; m++)
                {
                    strEncKey = strEncKey + strEncKey;
                }
                m = 0;
                for (m = 1; m <= glNumber; m++)
                {
                    gsEcDc = Strings.Mid(strenc, m, 1);
                    gsCompare = Strings.Mid(strEncKey, m, 1);
                    gsFinal = gsFinal + Strings.Chr(Strings.Asc(gsEcDc) + Strings.Asc(gsCompare) + 13);
                }
                return gsFinal;
                //string key_Val = "k2hLr4X0ozNyZByj5DT66edtCEee1x+6";
                //if (string.IsNullOrEmpty(plainText))
                //{
                //    return plainText;
                //}

                //byte[] Key = Encoding.UTF8.GetBytes(key_Val);
                //using (System.Security.Cryptography.AesCryptoServiceProvider aesAlg = new System.Security.Cryptography.AesCryptoServiceProvider())
                //{
                //    aesAlg.Key = Key;
                //    aesAlg.Mode = System.Security.Cryptography.CipherMode.ECB;

                //    System.Security.Cryptography.ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                //    using (System.IO.MemoryStream outputStream = new System.IO.MemoryStream())
                //    {
                //        using (System.Security.Cryptography.CryptoStream csEncrypt = new System.Security.Cryptography.CryptoStream(outputStream, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
                //        {
                //            using (System.IO.StreamWriter swEncrypt = new System.IO.StreamWriter(csEncrypt))
                //            {
                //                swEncrypt.Write(plainText);
                //            }
                //        }
                //        return "\\x" + ByteArrayToString(outputStream.ToArray());

                //    }
                //}
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

    }
}