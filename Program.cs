using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Text;

namespace TradeWeb.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    ReadWriteFile();
                });

        public static void ReadWriteFile()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            string filePath = System.IO.Directory.GetCurrentDirectory();
            string json = File.ReadAllText(Path.Combine(filePath, "appsettings.json"));
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

            string sectionPath = jsonObj["ConnectionStrings"]["DefaultConnection"].ToString();
            if (string.IsNullOrEmpty(sectionPath))
            {
                string serverName = "", Database = "", userId = "", pwd = "";

                string[] iniLines = File.ReadAllLines(Path.Combine(filePath, "TPlus.INI"), Encoding.GetEncoding("Windows-1252"));

                foreach (string line in iniLines)
                {
                    if (line.StartsWith("SERVER=", StringComparison.OrdinalIgnoreCase))
                    {
                        serverName = line.Substring("SERVER=".Length).Trim();
                    }
                    else if (line.StartsWith("DB=", StringComparison.OrdinalIgnoreCase))
                    {
                        Database = line.Substring("DB=".Length).Trim();
                    }
                    else if (line.StartsWith("USR=", StringComparison.OrdinalIgnoreCase))
                    {
                        userId = line.Substring("USR=".Length).Trim();
                    }
                    else if (line.StartsWith("PWD=", StringComparison.OrdinalIgnoreCase))
                    {
                        string originalString = line.Substring("PWD=".Length).Trim();
                        pwd = originalString;
                    }
                }
                string conString = $"server={serverName};Database={Database};Uid={userId};EPWD={pwd};Max Pool Size=200;Connect Timeout=20000;pooling='true';";

                jsonObj["ConnectionStrings"]["DefaultConnection"] = conString;

                string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(Path.Combine(filePath, "appsettings.json"), output);
            }
        }
    }
}
