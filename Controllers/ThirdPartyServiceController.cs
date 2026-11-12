using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TradeWeb.API.Logs;
using TradeWeb.API.Models;
using TradeWeb.API.QuestPdfTemplates;
using TradeWeb.API.Repository;
using TradeWeb.API.Service;

//using TradeWeb.API.Service;
using TradeWeb.Service;

namespace TradeWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThirdPartyServiceController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon _objUtility;
        private readonly ITradeWebRepository _tradewebRepo;
        private readonly ILogger<ThirdPartyServiceController> _logger;

        //ThirdPartyServiceDAO dao = new ThirdPartyServiceDAO();
        ConvertData returnJson = new ConvertData();
        DataTable retrunDt = new DataTable();

        public ThirdPartyServiceController(IConfiguration configuration, UtilityCommon objUtility, ILogger<ThirdPartyServiceController> logger, ITradeWebRepository repo)
        {
            _configuration = configuration;
            _objUtility = objUtility;
            _logger = logger;
            _tradewebRepo = repo;
        }


        // [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpPost("RequestService", Name = "RequestService")]
        public IActionResult RequestService([FromBody] ThirdPartyModel fModel)
        {
            if (ModelState.IsValid)
            {
                DataTable retrunDt = new DataTable();
                try
                {

                    object outPut = null;
                    string modelJson = JsonConvert.SerializeObject(fModel);
                    string JsonString = "";
                    var desJsoj = JsonConvert.DeserializeObject<dynamic>(modelJson.ToString());
                    try
                    {
                        desJsoj["Request"] = JsonConvert.DeserializeObject<dynamic>(fModel.Request.ToString());
                        JsonString = JsonConvert.SerializeObject(desJsoj);
                    }
                    catch (Exception)
                    {
                        desJsoj["Request"] = fModel.Request.ToString();
                        JsonString = JsonConvert.SerializeObject(desJsoj);
                    }

                    try
                    {
                        string soapResult = "";
                        string url = fModel.APIURL ?? "";
                        string contentType = fModel.ContentType ?? "";
                        string apiType = fModel.APIType ?? "";
                        string apiCallingType = fModel.APICallingType ?? "";
                        string authKey = fModel.AuthKey ?? "";
                        string input = fModel.Request.ToString();

                        //#region If Service is GET then if User Wants to send any input Parameters as a query string

                        List<GetInputModel> GetInputFields = fModel.FieldsForGET;
                        if (apiCallingType.ToUpper() == "GET" && GetInputFields != null && GetInputFields.Count > 0)
                        {
                            url = url + "?";
                            foreach (GetInputModel model in GetInputFields)
                            {
                                url = url + model.Key + "=" + model.Value + "&";
                            }
                            url = url.TrimEnd('&');
                        }

                        //#endregion

                        HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                        webRequest.ContentType = contentType;
                        webRequest.Method = apiCallingType;
                        if (apiCallingType != "GET")
                        {
                            webRequest.ContentLength = input.Length;
                        }
                        if (authKey != "")
                        {
                            webRequest.Headers.Add("Authorization", authKey);
                            webRequest.PreAuthenticate = true;
                        }

                        #region Add Headers Dynamically

                        List<GetInputModel> headerFields = fModel.FieldsForHeader;
                        if (headerFields != null && headerFields.Count > 0)
                        {
                            foreach (GetInputModel model in headerFields)
                            {
                                webRequest.Headers.Add(model.Key, model.Value);
                            }
                        }
                        #endregion
                        //ServicePointManager.Expect100Continue = true;
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                        LogInfo.WriteErrorLog("streamWriter:1 ");

                        if (apiCallingType != "GET")
                        {
                            using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
                            {
                                streamWriter.Write(input);
                                streamWriter.Flush();
                            }
                        }
                        LogInfo.WriteErrorLog("CheckLongRequestResponse:begin async call to web request.");
                        // begin async call to web request.
                        IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                        LogInfo.WriteErrorLog("CheckLongRequestResponse:suspend this thread until call is complete. You might want to do something usefull here like update your UI.");
                        // suspend this thread until call is complete. You might want to do something usefull here like update your UI.
                        asyncResult.AsyncWaitHandle.WaitOne();
                        LogInfo.WriteErrorLog("CheckLongRequestResponse:get the response from the completed web request.");
                        // get the response from the completed web request.
                        using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                        {
                            using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                            {
                                outPut = rd.ReadToEnd();
                            }
                        }
                        //soapResult = System.IO.File.ReadAllText(@"D:\ankur-professional\881response.txt");
                        soapResult = outPut.ToString();
                        LogInfo.WriteErrorLog("Soap Result: " + soapResult);
                        soapResult = soapResult.Replace("&lt;", "<");
                        soapResult = soapResult.Replace("&gt;", ">");
                        soapResult = soapResult.Replace("'", "''"); // Add by ankur 22/12/2022 
                        //soapResult = soapResult.Replace("\\r\\n", ""); // Add by ankur 22/12/2022 

                        //If Output Type is JSON need to convert string to JSON Object
                        if (fModel.OutputType != null && fModel.OutputType.ToUpper() == "JSON")
                        {
                            //JObject js = JObject.Parse(json);
                            //outPut = js;
                            var json = JsonConvert.DeserializeObject(soapResult).ToString();
                            outPut = json;
                        }
                        LogInfo.WriteErrorLog("Soap Result:: " + outPut);
                        //return Request.CreateResponse(HttpStatusCode.OK, outPut);
                        return Ok(outPut);
                    }
                    catch (WebException ex)
                    {
                        if (ex.Response != null)
                        {
                            using (WebResponse response = ex.Response)
                            {
                                HttpWebResponse httpResponse = (HttpWebResponse)response;
                                LogInfo.WriteErrorLog("Error code: " + httpResponse.StatusCode + " -> " + ex.Response.ToString());

                                using (Stream data = response.GetResponseStream())
                                using (var reader = new StreamReader(data))
                                {
                                    // text is the response body
                                    string text = reader.ReadToEnd();
                                    outPut = text;

                                    //If Output Type is JSON need to convert string to JSON Object
                                    if (fModel.OutputType != null && fModel.OutputType.ToUpper() == "JSON")
                                    {
                                        //JObject json = JObject.Parse(text);
                                        var json = JsonConvert.DeserializeObject(text).ToString();
                                        outPut = json;
                                    }
                                    LogInfo.WriteErrorLog("Soap Result catch:: " + outPut);
                                    //return Request.CreateResponse(httpResponse.StatusCode, outPut);
                                    return Ok(outPut);
                                }
                            }
                        }
                        else
                        {
                            outPut = ex.ToString();
                            //LogInfo.WriteErrorLog(ex);
                            LogInfo.WriteErrorLog("Soap Result ::: " + outPut);
                            //return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message.ToString());
                            return Ok(outPut);
                        }
                    }
                    catch (Exception ex)
                    {
                        outPut = ex.ToString();
                        //LogInfo.WriteErrorLog(ex);
                        LogInfo.WriteErrorLog("Soap Result :::: " + outPut);
                        //return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
                        return Ok(outPut);
                    }
                    finally
                    {
                        #region Insert Service Response in tbl_GenericAPILog

                        LogInfo.WriteErrorLog("Soap Result ::::@ " + outPut.ToString());
                        _objUtility.InsertThirdPartyServiceData(fModel, JsonString, outPut == null ? "" : outPut.ToString()); //changeit
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    return Ok(ex.Message.ToString());
                }
            }
            return BadRequest();
        }


        [HttpPost("UploadMultiPartImage", Name = "UploadMultiPartImage")]
        public IActionResult UploadMultiPartImage([FromBody] ThirdPartyServiceMultiPartModel fModel)
        {
            object outPut = null;
            string modelJson = JsonConvert.SerializeObject(fModel);
            var desJsoj = JsonConvert.DeserializeObject<dynamic>(modelJson.ToString());
            desJsoj["Request"] = JsonConvert.DeserializeObject<dynamic>(fModel.Request.ToString());
            string JsonString = JsonConvert.SerializeObject(desJsoj);

            LogInfo.WriteErrorLog("ThirdPartyServiceController,RequestService  :: JsonString Exist!!");
            try
            {
                string url = fModel.APIURL ?? "";
                string contentType = fModel.ContentType ?? "";
                string apiType = fModel.APIType ?? "";
                string apiCallingType = fModel.APICallingType ?? "";
                string authKey = fModel.AuthKey ?? "";
                string input = fModel.Request.ToString();
                string fileType = fModel.FileType ?? "";
                string file = fModel.File ?? "";
                string fileName = fModel.FileName ?? "";
                string filePassword = fModel.FilePassword ?? "";
                string fileFormat = fModel.FileFormat ?? "";

                if (fileType.ToUpper() == "PDF")
                {
                    fileType = "document";
                }

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.ExpectContinue = true;
                    using (var request = new HttpRequestMessage(new HttpMethod(apiCallingType), url))
                    {
                        if (authKey != "")
                        {
                            request.Headers.Add("Authorization", authKey);
                        }

                        #region Add Headers Dynamically

                        List<GetInputModel> headerFields = fModel.FieldsForHeader;
                        if (headerFields != null && headerFields.Count > 0)
                        {
                            foreach (GetInputModel model in headerFields)
                            {
                                request.Headers.Add(model.Key, model.Value);
                            }
                        }

                        #endregion

                        #region Multi Part Logic

                        byte[] fileBytes = null;
                        if (fileFormat.ToUpper() == "PATH")
                        {
                            fileBytes = System.IO.File.ReadAllBytes(file);
                        }
                        else if (fileFormat.ToUpper() == "BASE64")
                        {
                            fileBytes = System.Convert.FromBase64String(file);
                        }
                        var ms = new MemoryStream();
                        MemoryStream stream = new MemoryStream(fileBytes);
                        IFormFile file2 = new FormFile(stream, 0, fileBytes.Length, fileName, fileName);
                        file2.CopyTo(ms);
                        var multipartContent = new MultipartFormDataContent();
                        var fileContent = new ByteArrayContent(ms.ToArray());
                        multipartContent.Add(fileContent, fileType, fileName);
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                        multipartContent.Add(new StringContent(fileName), "name");
                        multipartContent.Add(new StringContent(""), "password");
                        //if (!string.IsNullOrWhiteSpace(filePassword))
                        //{
                        //    multipartContent.Add(new StringContent(filePassword), "password");
                        //}

                        #endregion

                        request.Content = multipartContent;
                        //ServicePointManager.Expect100Continue = true;
                        //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                        var handler = new HttpClientHandler
                        {
                            SslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12
                        };
                        var response = httpClient.SendAsync(request).Result;
                        string content = "";
                        if (!response.IsSuccessStatusCode)
                        {
                            LogInfo.WriteErrorLog("Error code: " + response.StatusCode + " -> ");
                            content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            var parseJson = JObject.Parse(content);
                        }
                        else
                        {
                            content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            var parseJson = JObject.Parse(content);
                        }
                        content = content.Replace("'", "''"); // Add by ankur 22/12/2022 
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(content))
                            {
                                var json = JsonConvert.DeserializeObject(content).ToString();
                                //JObject json = JObject.Parse(content);
                                //var parseJson = JObject.Parse(content);
                                outPut = json;
                                return Ok(outPut);
                            }
                            else
                            {
                                return Ok(outPut);
                            }
                        }
                        catch (JsonReaderException)
                        {
                            outPut = content;
                            return Ok(outPut);
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                #region Handle WebResponse Exception

                if (ex.Response != null)
                {
                    using (WebResponse response = ex.Response)
                    {
                        HttpWebResponse httpResponse = (HttpWebResponse)response;
                        LogInfo.WriteErrorLog("Error code: " + httpResponse.StatusCode + " -> " + ex.Response.ToString());
                        using (Stream data = response.GetResponseStream())
                        using (var reader = new StreamReader(data))
                        {
                            string text = reader.ReadToEnd();
                            JObject json = JObject.Parse(text);
                            outPut = json;
                            return Ok(outPut);
                        }
                    }
                }
                else
                {
                    outPut = ex.ToString();
                    LogInfo.WriteErrorLog(ex);
                    return Ok(outPut);
                }

                #endregion
            }
            catch (Exception ex)
            {
                outPut = ex.ToString();
                LogInfo.WriteErrorLog(ex.ToString());
                return Ok(outPut);
            }
            finally
            {
                #region Insert Service Response in tbl_GenericAPILog

                //ThirdPartyServiceDAO dao = new ThirdPartyServiceDAO();
                _objUtility.InsertThirdPartyServiceData(fModel, JsonString, outPut == null ? "" : outPut.ToString()); //changeit
                #endregion
            }
        }


        //[Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("Base64FromUrl", Name = "Base64FromUrl")]
        public async Task<IActionResult> Base64FromUrl([FromBody] SingleVlaueParam fileUrl)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    ////JwtSecurityToken token = _objUtility.GetToken();
                    ////var userId = token.Claims.First(claim => claim.Type == "username").Value;
                    using (var client = new HttpClient())
                    {
                        byte[] getPdfBytes = await client.GetByteArrayAsync(fileUrl.fileUrl);
                        try
                        {
                            var base64 = Convert.ToBase64String(getPdfBytes);
                            return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "File download successfully", base64, ""));
                        }
                        catch (Exception e)
                        {
                            return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", e.Message.ToString(), ""));
                        }
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.BadRequest, "error", ex.Message.ToString(), ""));
                }
            }
            return BadRequest();
        }

        //[Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("Base64ToBinary", Name = "Base64ToBinary")]
        public async Task<IActionResult> Base64ToBinary([FromBody] SingleStringModel request)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var image = Convert.FromBase64String(request.request);
                    string hex = BitConverter.ToString(image);
                    hex = hex.Replace("-", "");
                    hex = "0x" + hex;
                    return Ok(hex);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message.ToString());
                }
            }
            return BadRequest();
        }

        [HttpPost("EncodeBase64", Name = "EncodeBase64")]
        public IActionResult EncodeBase64([FromBody] EncodeBase64Model request)
        {
            if (ModelState.IsValid)
            {
                string responseStr = "";
                try
                {
                    if (request.type.ToLower() == "encode")
                    {
                        responseStr = Convert.ToBase64String(System.Text.Encoding.GetEncoding(request.format).GetBytes(request.reqString));
                    }
                    else if (request.type.ToLower() == "decode")
                    {
                        byte[] decodedBytes = Convert.FromBase64String(request.reqString);
                        responseStr = System.Text.Encoding.GetEncoding(request.format).GetString(decodedBytes);
                    }
                    return Ok(responseStr);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message.ToString());
                }
            }
            return BadRequest();
        }

        //[HttpPost("Encrypt", Name = "Encrypt")]
        //public IActionResult Encrypt([FromBody] EncryptModel request)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            string responseStr = _objUtility.EncryptAES(request.PlainText, request.Key);
        //            return Ok(responseStr);
        //        }
        //        catch (Exception ex)
        //        {
        //            return BadRequest(ex.Message.ToString());
        //        }
        //    }
        //    return BadRequest();
        //}

        [HttpPost("EncryptKRA", Name = "EncryptKRA")]
        public IActionResult EncryptKRA([FromBody] EncryptKRA_Model encryptRequest)
        {
            try
            {
                var plainText = encryptRequest.data.ToString().Trim();
                byte[] Key = Base64UrlEncoder.DecodeBytes(encryptRequest.aesKey);
                byte[] encrypted;
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Key;
                    // _configuration["kraIV"] = Base64UrlEncoder.Encode(aesAlg.IV);
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(plainText);
                            }
                            encrypted = msEncrypt.ToArray();
                            string encValue = Base64UrlEncoder.Encode(encrypted);
                            return Ok(new { encryptedData = encValue, IV = Base64UrlEncoder.Encode(aesAlg.IV) });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogInfo.WriteErrorLog(ex.ToString());
                return Ok("");
            }
        }
        [HttpPost("DecryptKRA", Name = "DecryptKRA")]
        public IActionResult DecryptKRA([FromBody] DecryptKRA_Model decryptRequest)
        {
            string plaintext = null;
            try
            {
                var cipherText = Base64UrlEncoder.DecodeBytes(decryptRequest.encryptedText.ToString().Trim());
                byte[] Key = Base64UrlEncoder.DecodeBytes(decryptRequest.aesKey);
                byte[] iv = Base64UrlEncoder.DecodeBytes(decryptRequest.IV);
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Key;
                    aesAlg.IV = iv;
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogInfo.WriteErrorLog("DecryptKRA:=> " + ex.ToString());
                return Ok("-1");
            }
            return Ok(plaintext);
        }

        [HttpPost("SFTP_Upload", Name = "SFTP_Upload")]
        public IActionResult SFTP_Upload([FromBody] SFTP_Model request)
        {

            string host = request.hostName;  // The SFTP server hostname or IP address
            string username = request.username;     // SFTP username
            string password = request.password;     // SFTP password
            //string localFilePath = @"D:\" + request.fileName;  // Path to the local file you want to upload
            int port = request.port;  // Path on the SFTP server to upload to
            string remoteFilePath = request.filePath;  // Path on the SFTP server to upload to
            //string remoteFilePath = "/02012025/CKYC/" + request.fileName;  // Path on the SFTP server to upload to
            string ouputResp = "";
            try
            {
                // Create an SFTP client
                using (var sftp = new SftpClient(host, port, username, password))
                {
                    try
                    {
                        int oprTimeOut = 0, keepLive = 0;
                        oprTimeOut = Convert.ToInt32(_configuration["oprTimeout"]);
                        keepLive = Convert.ToInt32(_configuration["keepLiveTime"]);
                        sftp.OperationTimeout = TimeSpan.FromMinutes(oprTimeOut);
                        sftp.KeepAliveInterval = TimeSpan.FromSeconds(keepLive);
                        uint bufferSize;
                        if (!uint.TryParse(_configuration["bufferSize"], out bufferSize))
                        {
                            bufferSize = 32768; // default 32 KB
                        }
                        sftp.BufferSize = bufferSize;
                        sftp.Connect();

                        // Check if the SFTP connection is successful
                        if (sftp.IsConnected)
                        {
                            if (!sftp.Exists(remoteFilePath))
                                sftp.CreateDirectory(remoteFilePath);
                            // Open the local file stream
                            //using (var fileStream = new FileStream(localFilePath, FileMode.Open))
                            //{
                            //    sftp.UploadFile(fileStream, remoteFilePath + "/" + Path.GetFileName(localFilePath));
                            //    // Upload the file to the SFTP server
                            //    sftp.UploadFile(fileStream, remoteFilePath);
                            //    Console.WriteLine("File uploaded successfully to " + remoteFilePath);
                            //}
                            foreach (var item in request.files)
                            {
                                // Decode the Base64 content into a byte array
                                byte[] fileBytes = Convert.FromBase64String(item.base64);
                                using (var memoryStream = new MemoryStream(fileBytes))
                                {
                                    //sftp.UploadFile(memoryStream, remoteFilePath + "/" + item.fileName);
                                    memoryStream.Position = 0;
                                    sftp.UploadFile(memoryStream, $"{remoteFilePath}/{item.fileName}");
                                    ouputResp = "File uploaded successfully";
                                    item.base64 = "";
                                }
                            }
                        }
                        else
                        {
                            //Console.WriteLine("Could not connect to the SFTP server.");
                            ouputResp = "Could not connect to the SFTP server";
                            return Ok("Could not connect to the SFTP server.");
                        }
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine("Error: " + ex.Message);
                        ouputResp = "Error: " + ex.Message;
                        return BadRequest("Error: " + ex.Message);
                    }
                    finally
                    {
                        // Always ensure to disconnect after the operation
                        if (sftp.IsConnected)
                        {
                            sftp.Disconnect();
                        }
                        ThirdPartyModel fModel = new ThirdPartyModel();
                        fModel.ProjectName = "REKYC";
                        fModel.APIVendorName = "SFTP-File Upload";
                        string JsonString = JsonConvert.SerializeObject(request);
                        _objUtility.InsertThirdPartyServiceData(fModel, JsonString, ouputResp); //changeit
                    }
                }
                return Ok("File uploaded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return Ok("Something went wrong!");
        }

        [HttpPost("SFTP_Download", Name = "SFTP_Download")]
        public IActionResult SFTP_Download([FromBody] SFTP_DownloadModel request)
        {
            string host = request.hostName;  // The SFTP server hostname or IP address
            string username = request.username;     // SFTP username
            string password = request.password;     // SFTP password
            //string localFilePath = @"D:\" + request.fileName;  // Path to the local file you want to upload
            int port = request.port;  // Path on the SFTP server to upload to
            string remoteFilePath = request.filePath;// + "/" + request.fileName;  // Path on the SFTP server to upload to
            //string remoteFilePath = "/02012025/CKYC/" + request.fileName;  // Path on the SFTP server to upload to
            string strBase64 = "", ouputResp = "";
            try
            {
                // Create an SFTP client
                using (var sftp = new SftpClient(host, port, username, password))
                {
                    try
                    {
                        // Connect to the SFTP server
                        sftp.Connect();
                        // Check if the SFTP connection is successful
                        if (sftp.IsConnected)
                        {
                            // Download the file from the server
                            using (var fileStream = new MemoryStream())
                            {
                                sftp.DownloadFile(remoteFilePath + "/" + request.fileName, fileStream);

                                // Convert the downloaded file to Base64
                                byte[] fileBytes = fileStream.ToArray();
                                strBase64 = Convert.ToBase64String(fileBytes);
                                ouputResp = "File downloaded successfully";
                                return Ok(new { status = true, message = "File downloaded successfully", file = strBase64 });
                            }
                        }
                        else
                        {
                            ouputResp = "Could not connect to the SFTP server";
                            return Ok(new { status = false, message = "Could not connect to the SFTP server", file = "" });
                        }
                        // Console.WriteLine("File downloaded successfully.");
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine("Error: " + ex.Message);
                        ouputResp = "Error: " + ex.Message.ToString();
                        return Ok(new { status = false, message = "Error: " + ex.Message.ToString(), file = "" });
                    }
                    finally
                    {
                        // Always ensure to disconnect after the operation
                        if (sftp.IsConnected)
                        {
                            sftp.Disconnect();
                        }
                        ThirdPartyModel fModel = new ThirdPartyModel();
                        fModel.ProjectName = "REKYC";
                        fModel.APIVendorName = "SFTP-File Download";
                        string JsonString = JsonConvert.SerializeObject(request);
                        _objUtility.InsertThirdPartyServiceData(fModel, JsonString, ouputResp); //changeit
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return Ok("Something went wrong!");
        }

        [HttpPost("EncryptCDSL", Name = "EncryptCDSL")]
        public IActionResult EncryptCDSL([FromBody] EncryptKRA_Model encryptRequest)
        {
            try
            {
                var plainText = encryptRequest.data.ToString().Trim();
                string encValue = _objUtility.EDIS_Encrypt(plainText, encryptRequest.aesKey.Trim());
                return Ok(new { Data = encValue });
            }
            catch (Exception ex)
            {
                LogInfo.WriteErrorLog("EncryptCDSL:=> " + ex.ToString());
                return Ok("");
            }
        }

        [HttpPost("DecryptCDSL", Name = "DecryptCDSL")]
        public IActionResult DecryptCDSL([FromBody] EncryptKRA_Model decryptRequest)
        {
            try
            {
                var plainText = decryptRequest.data.ToString().Trim();
                string encValue = _objUtility.EDIS_Decrypt(plainText, decryptRequest.aesKey.Trim());
                return Ok(new { Data = encValue });
            }
            catch (Exception ex)
            {
                LogInfo.WriteErrorLog("DecryptKRA:=> " + ex.ToString());
                return Ok("-1");
            }
        }


        [HttpPost("RupeeseedEncy")]
        public IActionResult RupeeseedEncy([FromBody] HmacRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Key) || string.IsNullOrWhiteSpace(request.Data))
                return BadRequest("Key and Data must be provided.");

            var result = HmacEncode(request.Key, request.Data);
            return Ok(new { hmac = result });
        }


        [HttpPost("SendOTP")]
        public IActionResult SendOTP([FromBody] SendOTP request)
        {
            string chars1 = "1234567890", finalResponse = "";
            bool mobOtp = false, emailOtp = false, smsLogs = false; //emailLogs = false
            char[] stringChars1 = new char[4];

            Random random1 = new Random();
            for (int i = 0; i < stringChars1.Length; i++)
            {
                stringChars1[i] = chars1[random1.Next(chars1.Length)];
            }
            string strOTP = new String(stringChars1);

            string ChangeMethod = string.Empty;
            string type = "MO", reqFor = "";
            if (request.Type == "BT")
            {
                emailOtp = true;
                mobOtp = true;
                type = "BT";
                reqFor = request.Mobile;
            }
            else if (request.Type == "EI")
            {
                emailOtp = true;
                type = "EI";
                reqFor = request.Email;
            }
            else if (request.Type == "MO")
            {
                mobOtp = true;
                type = "MO";
                reqFor = request.Mobile;
            }
            else if (request.Type == "GI")
            {
                emailOtp = true;
                mobOtp = true;
                type = "GI";
                reqFor = request.Mobile;
            }

            if (mobOtp)
            {
                if (string.IsNullOrEmpty(request.Mobile))
                { return Ok("Mobile number should not be blank!"); }

                string strMsgTxt = request.SMSText.Replace("<OTP>", strOTP);
                finalResponse = _objUtility.SendSmsCommon(request.userId, strMsgTxt, request.Mobile);
                if (finalResponse.ToLower().Contains("success"))
                {
                    string encMobNo = request.Mobile.Substring(request.Mobile.Length - 4);
                    finalResponse = "OTP sent successfully on mobile xxxxxx" + encMobNo;
                    smsLogs = true;
                }
            }
            if (emailOtp)
            {
                if (string.IsNullOrEmpty(request.Email))
                { return Ok("Email id should not be blank!"); }

                string strMsgTxt = request.EmailBody.Replace("<OTP>", strOTP);
                string emailResponse = _objUtility.SendEmail_OTP(request.userId, request.Email, request.EmailSubject, strMsgTxt, smsLogs);
                if (emailResponse.ToLower().Contains("success"))
                {
                    string encEmail = request.Email.Substring(0, 4);
                    finalResponse = finalResponse != "" ? finalResponse + " and sent successfully on email " + encEmail + "xxxxxxxxx" : "OTP sent successfully on email " + encEmail;
                }
                else
                {
                    finalResponse = finalResponse + " \n" + emailResponse;
                }
            }

            if (finalResponse.ToLower().Contains("success"))
            {
                int intOTPValidTime = 5;
                _objUtility.CreateOTPMasterTable();

                string strsql = "Update OTP_Master set OTP_Status = 'E' where OTP_ClientCode='" + request.userId.ToUpper() + "' and OTP_Product = 'TradeWeb' and OTP_Type = 'RKC' and OTP_Status = 'P' and OTP_SentTo = '" + reqFor + "' ";
                _objUtility.ExecuteSQL(strsql);
                strsql = "Insert into OTP_Master values('" + request.userId.ToUpper() + "', '" + type + "', '" + strOTP.Trim() + "','" + reqFor + "', CONVERT(CHAR(8),GETDATE(),112), CONVERT(VARCHAR(8), GETDATE(), 108), CONVERT(CHAR(8), DATEADD(MINUTE, " + intOTPValidTime + ", GETDATE()), 112),CONVERT(VARCHAR(8), DATEADD(MINUTE, " + intOTPValidTime + ", GETDATE()), 108),'P','" + request.Product + "', '" + request.OTPType + "','','','',0,0,0)";
                _objUtility.ExecuteSQL(strsql);
                if (request.Type == "BT")
                {
                    strsql = "Insert into OTP_Master values('" + request.userId.ToUpper() + "', '" + type + "', '" + strOTP.Trim() + "','" + request.Email + "', CONVERT(CHAR(8),GETDATE(),112), CONVERT(VARCHAR(8), GETDATE(), 108), CONVERT(CHAR(8), DATEADD(MINUTE, " + intOTPValidTime + ", GETDATE()), 112),CONVERT(VARCHAR(8), DATEADD(MINUTE, " + intOTPValidTime + ", GETDATE()), 108),'P','" + request.Product + "', '" + request.OTPType + "','','','',0,0,0)";
                    _objUtility.ExecuteSQL(strsql);
                }
            }

            return Ok(finalResponse);
        }

        [HttpPost("VerifyOTP")]
        public IActionResult VerifyOTP([FromBody] VerifyOTP request)
        {
            ////// Check otp
            string checkOTP = "", strIdentity = "", strMobileNo = "", OTP = request.otp;
            int maxOtpFailCount = 0;
            string Strsql = "select top 1 OTP_Identity,OTP_OTP,OTP_ValidTillDate,OTP_ValidTillTime, OTP_SentTo, OTP_NFiller1  from OTP_Master with (NoLock) where OTP_ClientCode='" + request.userId + "' and OTP_Status='P' and OTP_Product='" + request.Product + "' and OTP_Type = '" + request.OTPType + "' and OTP_SentTo = '" + request.otpFor + "'   order by OTP_Identity desc";
            DataTable dt = _objUtility.OpenDataTable(Strsql);
            if (dt.Rows.Count > 0)
            {
                checkOTP = dt.Rows[0]["OTP_OTP"].ToString().Trim();
                strIdentity = dt.Rows[0]["OTP_Identity"].ToString().Trim();
                strMobileNo = dt.Rows[0]["OTP_SentTo"].ToString().Trim();
                maxOtpFailCount = Convert.ToInt32(dt.Rows[0]["OTP_NFiller1"].ToString());
            }
            if (maxOtpFailCount >= 3)
            {
                return Ok("Maximum OTP attempts reached. Please generate a new OTP.");
            }
            else if (!string.IsNullOrWhiteSpace(OTP) && !string.IsNullOrWhiteSpace(checkOTP) && OTP == checkOTP)
            {
                DateTime dtNow = _objUtility.GetSqlCurrentDateTime();
                DateTime dt3 = _objUtility.stod(dt.Rows[0]["OTP_ValidTillDate"].ToString());
                TimeSpan time = TimeSpan.Parse(dt.Rows[0]["OTP_ValidTillTime"].ToString());
                DateTime dtExpiry = dt3 + time;
                if (dtNow > dtExpiry)
                {
                    Strsql = "update OTP_Master set OTP_Status='E' where OTP_Identity='" + strIdentity + "'";
                    _objUtility.ExecuteSQL(Strsql);
                    return Ok("OTP has been expired");
                }
                Strsql = "update OTP_Master set OTP_Status='M' where OTP_Identity='" + strIdentity + "'";
                _objUtility.ExecuteSQL(Strsql);
                // return "Password has been changed successfully!";
            }
            else
            {
                Strsql = "update OTP_Master set OTP_NFiller1=" + (maxOtpFailCount + 1) + " where OTP_Identity='" + strIdentity + "'";
                _objUtility.ExecuteSQL(Strsql);
                return Ok("OTP Mismatched");
            }
            return Ok("OTP verified");

        }

        private readonly string _currentDirectory = Directory.GetCurrentDirectory();
        [HttpPost("CKYC_FileDownload", Name = "CKYC_FileDownload")]
        public async Task<IActionResult> ProcessFilesAsync(CkycDownloadFileModel request)
        {
            string folderPath = "", subFolderPath = "", insideFolderPath = "";
            string projectPath = Path.Combine(_currentDirectory);
            try
            {
                //// For create file only
                if (string.IsNullOrEmpty(request.baseFolderName.Trim()))
                {
                    string filePath = request.baseFileName.Trim();
                    await System.IO.File.WriteAllTextAsync(filePath, request.baseFileString.Trim());

                    string ext = Path.GetExtension(request.baseFileName.Trim()).TrimStart('.');

                    byte[] strBytes = await System.IO.File.ReadAllBytesAsync(filePath.Trim());

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    // return Ok(File(strBytes, "application/zip", request.baseFolderName.Trim() + ".zip"));
                    return Ok(File(strBytes, "application/" + ext, request.baseFileName.Trim()));
                }

                //// For create zip file with nested folders
                // Create base folder and write base file
                if (!string.IsNullOrEmpty(request.baseFolderName.Trim()))
                {
                    folderPath = Path.Combine(_currentDirectory, request.baseFolderName.Trim());
                    insideFolderPath = Path.Combine(_currentDirectory, request.baseFolderName.Trim(), request.baseFolderName.Trim());
                    await CreateFolderAndWriteFileAsync(insideFolderPath.Trim(), request.baseFileName.Trim(), request.baseFileString);
                }
                // Create subfolder and write sub files
                if (request.subFileDetails.Count > 0)
                {
                    //// subFolderPath = Path.Combine(_currentDirectory, request.subFolderName);
                    await CreateFolderAndWriteSubFilesAsync(subFolderPath, request.subFileDetails);
                }
                //// Create and copy the zip files
                await CreateAndCopyZipFilesAsync(folderPath.Trim(), insideFolderPath.Trim(), request.subFileDetails);

                //// Read the final folder zip and return as a response
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(folderPath.Trim() + ".zip");

                //// Delete zip files and folders
                await DeleteFolderAndZipFilesAsync(projectPath.Trim(), folderPath.Trim(), request.subFileDetails);

                //return File(fileBytes, "application/zip", request.baseFolderName + ".zip");
                return Ok(File(fileBytes, "application/zip", request.baseFolderName.Trim() + ".zip"));
            }
            catch (Exception ex)
            {
                // Log the exception (use a proper logging framework)
                //Console.Error.WriteLine($"Error processing files: {ex.Message}");
                throw new InvalidOperationException("An error occurred during file processing.", ex);
            }
            finally
            {
                await DeleteFolderAndZipFilesAsync(projectPath.Trim(), folderPath.Trim(), request.subFileDetails);
            }
        }

        [HttpPost("DownloadCKYCNO", Name = "DownloadCKYCNO")]
        public async Task<IActionResult> DownloadCKYCNO(CkycDownloadFileModel request)
        {
            string folderPath = "", subFolderPath = "", insideFolderPath = "";
            string projectPath = Path.Combine(_currentDirectory);
            try
            {
                //// For create file only
                if (string.IsNullOrEmpty(request.baseFolderName.Trim()))
                {
                    string filePath = request.baseFileName.Trim();
                    await System.IO.File.WriteAllTextAsync(filePath, request.baseFileString.Trim());

                    string ext = Path.GetExtension(request.baseFileName.Trim()).TrimStart('.');

                    byte[] strBytes = await System.IO.File.ReadAllBytesAsync(filePath.Trim());

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    // return Ok(File(strBytes, "application/zip", request.baseFolderName.Trim() + ".zip"));
                    return Ok(File(strBytes, "application/" + ext, request.baseFileName.Trim()));
                }

                //// For create zip file with nested folders
                // Create base folder and write base file
                if (!string.IsNullOrEmpty(request.baseFolderName.Trim()))
                {
                    folderPath = Path.Combine(_currentDirectory, request.baseFolderName.Trim());
                    insideFolderPath = Path.Combine(_currentDirectory, request.baseFolderName.Trim(), request.baseFolderName.Trim());
                    await CreateFolderAndWriteFileAsync(insideFolderPath.Trim(), request.baseFileName.Trim(), request.baseFileString);
                }
                // Create subfolder and write sub files
                if (request.subFileDetails.Count > 0)
                {
                    //// subFolderPath = Path.Combine(_currentDirectory, request.subFolderName);
                    await CreateFolderAndWriteSubFilesAsync(subFolderPath, request.subFileDetails);
                }
                //// Create and copy the zip files
                await CreateAndCopyZipFilesAsync(folderPath.Trim(), insideFolderPath.Trim(), request.subFileDetails);

                //// Read the final folder zip and return as a response
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(folderPath.Trim() + ".zip");

                //// Delete zip files and folders
                await DeleteFolderAndZipFilesAsync(projectPath.Trim(), folderPath.Trim(), request.subFileDetails);

                //return File(fileBytes, "application/zip", request.baseFolderName + ".zip");
                return Ok(File(fileBytes, "application/zip", request.baseFolderName.Trim() + ".zip"));
            }
            catch (Exception ex)
            {
                // Log the exception (use a proper logging framework)
                //Console.Error.WriteLine($"Error processing files: {ex.Message}");
                throw new InvalidOperationException("An error occurred during file processing.", ex);
            }
            finally
            {
                await DeleteFolderAndZipFilesAsync(projectPath.Trim(), folderPath.Trim(), request.subFileDetails);
            }
        }


        [HttpPost("ImportLargeFile", Name = "ImportLargeFile")]
        public IActionResult ImportLargeFile([FromBody] object request)
        {
            try
            {
                if (request == null)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "Data should not be blank", ""));
                }
                else if (string.IsNullOrEmpty(request.ToString()) || request.ToString() == "string")
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "Data should not be blank", ""));
                }
                else
                {
                    DataSet sp = _tradewebRepo.ImportLargeFileRepo(request);
                    if (sp != null && sp.Tables.Count > 0 && sp.Tables[0].Rows.Count > 0)
                    {
                        string output = sp.Tables[0].Rows[0][0].ToString();
                        if (output == "S")
                        {
                            return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", sp.Tables[0], ""));
                        }
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", sp.Tables[0], ""));
                    }
                    else
                    {
                        return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "No record found", ""));
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "An error occurred during file processing. " + ex.Message.ToString(), ""));
            }
        }

        [HttpPost("EncryptDecryptEPI", Name = "EncryptDecryptEPI")]
        public IActionResult EncryptDecryptEPI([FromBody] EncryptDecryptEPIRequest request)
        {
            try
            {
                var payload = request.payload.ToString().Trim();
                var type = request.mode.ToString().Trim();
                var key = request.key.ToString().Trim();
                string strResponse = "";

                if (type.Equals("encrypt", StringComparison.OrdinalIgnoreCase) || type.Equals("decrypt", StringComparison.OrdinalIgnoreCase))
                {
                    if (type.Equals("encrypt", StringComparison.OrdinalIgnoreCase))
                    {
                        strResponse = _objUtility.Encrypt(payload, key);
                    }
                    else
                    {
                        strResponse = _objUtility.Decrypt(payload, key);
                    }

                    return Ok(new { message = "success", data = strResponse });
                }
                else
                {
                    return Ok(new { message = "Invalid type", data = "" });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { message = "An error occurred during processing. " + ex.Message.ToString(), data = "" });
            }
        }

        [HttpPost("Encrypt", Name = "Encrypt")]
        public IActionResult Encrypt([FromBody] EncryptModel request)
        {
            try
            {
                var plaintext = request.PlainText.ToString().Trim();
                var key = request.Key.ToString().Trim();
                string strResponse = "";

                strResponse = _objUtility.encryptIPO(plaintext, key);

                //return Ok(strResponse);
                return Ok(new { message = "success", data = strResponse });
            }
            catch (Exception ex)
            {
                //return Ok("Error in encryption" + ex.Message);
                return Ok(new { message = "Error in encryption", data = ex.Message });
            }
        }

        [HttpPost("GenerateDashboardPdf", Name = "GenerateDashboardPdf")]
        public IActionResult GenerateDashboardPdf([FromBody] DashboardPdfRequest req)
        {
            try
            {
                if (req == null || req.Widgets == null || req.Widgets.Count == 0)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "At least one widget is required.", ""));
                }

                QuestPDF.Settings.License = LicenseType.Community;
                FontSetup.EnsureRegistered();

                ApplyCompanyBranding((name, logo) =>
                {
                    if (!string.IsNullOrWhiteSpace(name)) req.CompanyName = name;
                    if (!string.IsNullOrWhiteSpace(logo)) req.CompanyLogoBase64 = logo;
                });

                PopulateTabRows(req);

                var document = new DashboardPdfDocument(req);

                byte[] pdfBytes;
                using (var ms = new MemoryStream())
                {
                    document.GeneratePdf(ms);
                    pdfBytes = ms.ToArray();
                }

                string safeTitle = string.IsNullOrWhiteSpace(req.Title) ? "Dashboard" : req.Title;
                foreach (char c in Path.GetInvalidFileNameChars())
                    safeTitle = safeTitle.Replace(c, '_');

                string pdfName = $"{safeTitle}.pdf";

                if (string.Equals(req.Output, "file", StringComparison.OrdinalIgnoreCase))
                    return File(pdfBytes, "application/pdf", pdfName);

                var response = new PDFResponse
                {
                    PDFName = pdfName,
                    Base64PDF = Convert.ToBase64String(pdfBytes)
                };

                return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", response, ""));
            }
            catch (Exception ex)
            {
                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "An error occurred during PDF generation. " + ex.Message, ""));
            }
        }

        // Company name/logo always come from the DB (SP_InitializeLogin's branding lookup)
        // rather than the caller, so the PDF header can't be spoofed by the request body.
        // Best-effort: a DB hiccup falls back to whatever the caller supplied (if anything)
        // instead of failing the whole PDF.
        private void ApplyCompanyBranding(Action<string, string> apply)
        {
            try
            {
                var result = _tradewebRepo.GetCompanyBranding();
                if (result is DataTable dt && dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    string companyName = dt.Columns.Contains("CompanyName") ? Convert.ToString(row["CompanyName"]) : "";
                    string companyLogo = dt.Columns.Contains("CompanyLogo") ? Convert.ToString(row["CompanyLogo"]) : "";
                    apply(companyName, companyLogo);
                }
            }
            catch
            {
            }
        }

        // Replays each tab's DataApi server-side when the caller didn't supply Rows:
        // same ActionName/Option -> stored procedure lookup (tbl_GenericAPIDefinition)
        // the main TradeWeb endpoint uses, so a layout-only payload still gets tab data.
        // Best-effort per tab: a failed tab renders "No data available" instead of
        // failing the whole PDF.
        private void PopulateTabRows(DashboardPdfRequest req)
        {
            foreach (var widget in req.Widgets ?? new List<DashboardWidget>())
            {
                if (!string.Equals(widget.Type, "tableWithGraph", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var tab in widget.GridTabs ?? new List<GridTab>())
                {
                    // Opt-in, same as DashboardPdfDocument's own tab filter: a tab that
                    // won't render in the PDF (Setting.ShowInPdf not explicitly true)
                    // shouldn't have its stored procedure called at all.
                    if (tab.Setting == null || tab.Setting.ShowInPdf != true)
                        continue;

                    if ((tab.Rows?.Count ?? 0) > 0)
                        continue;

                    var option = tab.DataApi?.JUi?.Option?.Trim();
                    if (string.IsNullOrEmpty(option))
                        continue;

                    try
                    {
                        string actionName = tab.DataApi.JUi.ActionName?.Trim();
                        if (string.IsNullOrEmpty(actionName))
                            actionName = "TradeWeb";

                        string userId = "";
                        if (tab.DataApi.JApi != null && tab.DataApi.JApi.TryGetValue("UserId", out var u))
                            userId = JsonValueToString(u);
                        if (!string.IsNullOrWhiteSpace(req.UserId))
                            userId = req.UserId.Trim();

                        // Same dsXml shape the page posts to api/Main/TradeWeb.
                        var sb = new StringBuilder();
                        sb.Append("<dsXml>");
                        sb.Append("<J_Ui>\"ActionName\":\"").Append(SecurityElement.Escape(actionName))
                            .Append("\", \"Option\":\"").Append(SecurityElement.Escape(option)).Append("\"</J_Ui>");
                        sb.Append("<Sql></Sql>");
                        AppendFilterXml(sb, "X_Filter", tab.DataApi.XFilter);
                        AppendFilterXml(sb, "X_Filter_Multiple", tab.DataApi.XFilterMultiple);
                        sb.Append("<J_Api>\"UserId\":\"").Append(SecurityElement.Escape(userId)).Append("\"</J_Api>");
                        sb.Append("</dsXml>");

                        var result = _tradewebRepo.CommonAPIrepoCall(actionName, option, sb.ToString());
                        if (result?.Data is DataSet ds && ds.Tables.Count > 0)
                            tab.Rows = DataTableToRows(ds.Tables[0]);
                    }
                    catch
                    {
                        // leave tab.Rows empty -> the table renders "No data available"
                    }
                }
            }
        }

        private static string JsonValueToString(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
                return "";
            return value.ValueKind == JsonValueKind.String ? (value.GetString() ?? "") : value.ToString();
        }

        private static void AppendFilterXml(StringBuilder sb, string elementName, Dictionary<string, JsonElement> filter)
        {
            sb.Append('<').Append(elementName).Append('>');
            if (filter != null)
            {
                foreach (var kv in filter)
                {
                    sb.Append('<').Append(kv.Key).Append('>')
                        .Append(SecurityElement.Escape(JsonValueToString(kv.Value)))
                        .Append("</").Append(kv.Key).Append('>');
                }
            }
            sb.Append("</").Append(elementName).Append('>');
        }

        private static List<Dictionary<string, JsonElement>> DataTableToRows(DataTable dt)
        {
            // TradeWeb's generic API stored procedures (tbl_GenericAPIDefinition) return
            // their result set as a single unnamed column - ADO.NET names it "Column1" -
            // holding the whole row set as one JSON array (SQL Server FOR JSON). This is
            // the same shape MainController.TradeWeb() unwraps into "rs0" for the page, so
            // mirror that here instead of treating the blob as one literal-text cell.
            if (dt.Columns.Count > 0 && dt.Columns[0].ColumnName == "Column1" && dt.Rows.Count > 0)
            {
                var raw = dt.Rows[0]["Column1"]?.ToString();
                if (string.IsNullOrWhiteSpace(raw))
                    return new List<Dictionary<string, JsonElement>>();

                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(raw)
                        ?? new List<Dictionary<string, JsonElement>>();
                }
                catch (System.Text.Json.JsonException)
                {
                    return new List<Dictionary<string, JsonElement>>();
                }
            }

            // Newtonsoft serializes a DataTable as a flat array of row objects
            // (dates as ISO strings, numerics as numbers, DBNull as null).
            var json = JsonConvert.SerializeObject(dt);
            return System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json)
                ?? new List<Dictionary<string, JsonElement>>();
        }

        [HttpPost("Match")]
        public IActionResult Match([FromBody] NameMatchRequest request)
        {
            if (request == null)
                return BadRequest(new NameMatchResult { Status = "Fail", MatchStatus = "INVALID INPUT" });

            var result = NameMatcher.Match(request.Name1, request.Name2);
            return Ok(result);
        }

        [HttpPost("ValidatePDF", Name = "ValidatePDF")]
        public IActionResult ValidateRekycPDF([FromBody] ValidatePDF request)
        {
            try
            {
                DataTable dt = _objUtility.OpenDataTable("Select ma_proof, ma_field, ma_filename From Client_ModifyAttach  Where  ma_field like '%Attachment%' And ma_cmcd = '" + request.clientCode + "' And ma_refno = " + request.refNo);
                bool finalValid = true;
                string messageOutput = "";
                foreach (DataRow row in dt.Rows)
                {
                    if (string.IsNullOrEmpty(row["ma_proof"].ToString()))
                    {
                        continue;
                    }
                    byte[] fileBytes = (byte[])row["ma_proof"];
                    string base64 = Encoding.UTF8.GetString(fileBytes);
                    byte[] pdfBytes = Convert.FromBase64String(base64);
                    string extension = GetFileExtTypst(base64); ;

                    bool isValid;
                    string message;

                    switch (extension)
                    {
                        case ".pdf":
                            isValid = ValidatePdf(pdfBytes, out message);
                            break;

                        case ".jpg":
                        case ".jpeg":
                        case ".png":
                        case ".bmp":
                            isValid = ValidateImage(pdfBytes, out message);
                            break;

                        default:
                            isValid = false;
                            message = "Unsupported file type";
                            break;
                    }
                    if (isValid == false)
                    {
                        finalValid = false;
                        message = message + " for " + row["ma_filename"].ToString() + "\n";
                        messageOutput = messageOutput + " " + message;
                    }

                    // Update database
                }
                if (!finalValid)
                {
                    return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", messageOutput, ""));
                }
                return Ok(returnJson.ConvertDataIntoJson(true, (int)HttpStatusCode.OK, "success", "All Attachments Are Valid!", ""));
            }
            catch (Exception ex)
            {
                return Ok(returnJson.ConvertDataIntoJson(false, (int)HttpStatusCode.OK, "error", "An error occurred during file processing. " + ex.Message.ToString(), ""));
            }
        }


        #region Private methods

        private async Task CreateFolderAndWriteFileAsync(string folderPath, string fileName, string baseStrContent)
        {
            if (Directory.Exists(folderPath.Trim()))
            {
                Directory.Delete(folderPath.Trim(), true);
            }
            Directory.CreateDirectory(folderPath.Trim());

            string baseFilePath = Path.Combine(folderPath.Trim(), fileName.Trim());
            ////byte[] fileBytes = Convert.FromBase64String(base64Content);

            await System.IO.File.WriteAllTextAsync(baseFilePath, baseStrContent.Trim());
        }

        private async Task CreateFolderAndWriteSubFilesAsync(string subFolderPath, List<CkycFiles> subFileDetails)
        {
            foreach (var file in subFileDetails)
            {
                subFolderPath = Path.Combine(_currentDirectory, file.subFolderName.Trim(), file.subFolderName.Trim());
                //subFolderPath = subFolderPath;
                if (!Directory.Exists(subFolderPath.Trim()))
                {
                    Directory.CreateDirectory(subFolderPath.Trim());
                    //Directory.Delete(subFolderPath, true);
                }
                string subFilePath = Path.Combine(subFolderPath, file.fileName.Trim());
                file.fileBase64 = file.fileBase64.Replace("data:image/jpeg;base64,", "");
                byte[] fileBytes = Convert.FromBase64String(file.fileBase64.Trim());
                await System.IO.File.WriteAllBytesAsync(subFilePath, fileBytes);
            }
        }

        private async Task CreateAndCopyZipFilesAsync(string folderPath, string insideFolderPath, List<CkycFiles> subFileDetails)
        {

            string folderZipPath = folderPath.Trim() + ".zip";
            foreach (var file in subFileDetails)
            {
                string subFolderPath = Path.Combine(_currentDirectory, file.subFolderName.Trim());
                string subFolderZipPath = file.subFolderName.Trim() + ".zip";
                if (!string.IsNullOrEmpty(subFolderPath))
                {
                    if (System.IO.File.Exists(subFolderZipPath))
                    {
                        System.IO.File.Delete(subFolderZipPath);
                    }
                    ZipFile.CreateFromDirectory(subFolderPath, subFolderZipPath);
                    // Copy zip file to the main folder
                    string destinationFilePath = Path.Combine(insideFolderPath.Trim(), Path.GetFileName(subFolderZipPath));
                    System.IO.File.Copy(subFolderZipPath, destinationFilePath, overwrite: true);
                }
            }
            if (System.IO.File.Exists(folderZipPath))
            {
                System.IO.File.Delete(folderZipPath);
            }
            ZipFile.CreateFromDirectory(folderPath.Trim(), folderZipPath);
        }

        private async Task DeleteFolderAndZipFilesAsync(string projectPath, string folderPath, List<CkycFiles> subFileDetails)
        {
            string folderZipPath = folderPath.Trim() + ".zip";
            //// Check zip files and delete
            if (projectPath.Trim() != folderPath.Trim())
            {
                if (!string.IsNullOrEmpty(folderPath.Trim()))
                {
                    if (Directory.Exists(folderPath.Trim()))
                    {
                        Directory.Delete(folderPath.Trim(), true);
                    }
                }
            }
            //// Check zip files and delete
            if (System.IO.File.Exists(folderZipPath))
            {
                System.IO.File.Delete(folderZipPath);
            }
            foreach (var file in subFileDetails)
            {
                string subFolderPath = Path.Combine(_currentDirectory, file.subFolderName.Trim());
                if (projectPath.Trim() != subFolderPath.Trim())
                {
                    if (Directory.Exists(subFolderPath.Trim()))
                    {
                        if (!string.IsNullOrEmpty(subFolderPath.Trim()))
                        {
                            Directory.Delete(subFolderPath.Trim(), true);
                        }
                    }
                }
                string subFolderPathZip = subFolderPath.Trim() + ".zip";
                if (System.IO.File.Exists(subFolderPathZip))
                {
                    System.IO.File.Delete(subFolderPathZip);
                }
            }
        }

        public class SingleVlaueParam
        {
            public string fileUrl { get; set; } = "";
        }
        private string generateJwtToken(User user)
        {
            // generate token that is valid till 00.00AM next day
            var tokenHandler = new JwtSecurityTokenHandler();
            var val = _configuration["AppSettings:Secret"];
            var key = Encoding.ASCII.GetBytes(val);
            DateTime currentDate = DateTime.Now;
            DateTime futureDate = DateTime.Today.AddDays(1);
            TimeSpan ts = futureDate - currentDate;
            var s = ts.TotalSeconds;
            if (user.Username.Equals("TEMP"))
            {
                s = 60;
            }
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", user.Username) }),
                Expires = DateTime.Now.AddSeconds(s),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static string HmacEncode(string key, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(dataBytes);
                var sb = new StringBuilder(hashBytes.Length * 2);

                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2")); // Always two digits
                }

                return sb.ToString();
            }
        }

        private static bool ValidatePdf(byte[] fileBytes, out string message)
        {
            message = string.Empty;

            try
            {
                using (var reader = new PdfReader(fileBytes))
                {
                    int pages = reader.NumberOfPages;

                    if (pages <= 0)
                    {
                        message = "No pages found ";
                        return false;
                    }
                    //message = $"Valid PDF ({pages} pages)";
                    return true;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        private string GetFileExtTypst(string base64)
        {
            var data = base64.Substring(0, 5);

            switch (data.ToUpper())
            {
                case "IVBOR":
                    return ".png";
                case "/9J/4":
                    return ".jpg";
                case "AAAAF":
                    return ".mp4";
                case "JVBER":
                    return ".pdf";
                case "AAABA":
                    return ".ico";
                case "UMFYI":
                    return ".rar";
                case "E1XYD":
                    return ".rtf";
                case "U1PKC":
                    return ".txt";
                default:
                    return ".jpg";
            }
        }

        private static bool ValidateImage(byte[] fileBytes, out string message)
        {
            try
            {
                using var ms = new MemoryStream(fileBytes);
                using var img = System.Drawing.Image.FromStream(ms);

                //message = $"Valid Image ({img.Width}x{img.Height})";
                message = "";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }
        #endregion
    }
}
