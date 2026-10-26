using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TradeWeb.API.Helpers;
using TradeWeb.API.Repository;

namespace TradeWeb.API.Filters
{
    public class E2EContext
    {
        public byte[] AesKey { get; set; }
        public byte[] ServerPublicKey { get; set; }
    }

    public class E2EDecryptFilter : IAsyncActionFilter
    {
        public const string E2EContextKey = "E2E";
        public const string RequestEncryptedHeader = "x-e2e-request-encrypted";

        private readonly E2EKeyService _e2eKeyService;
        private readonly ILogger<E2EDecryptFilter> _logger;
        private readonly UtilityCommon _utility;

        public E2EDecryptFilter(E2EKeyService e2eKeyService, ILogger<E2EDecryptFilter> logger, UtilityCommon utility)
        {
            _e2eKeyService = e2eKeyService;
            _logger = logger;
            _utility = utility;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;

            // Server-side enforcement: skip E2E when APIENCDATA = 'N' in Sysparameter.
            bool e2eEnabled = _utility.fnchkTable("Sysparameter") &&
                              _utility.GetSysParmSt("APIENCDATA", "") == "Y";

            if (!e2eEnabled || !request.Headers.TryGetValue(E2EEncryptionHelper.PublicKeyHeader, out var publicKeyValue))
            {
                await next();
                return;
            }

            string requestBody = null;
            try
            {
                var clientPublicKey = Convert.FromBase64String(publicKeyValue.ToString());
                var (serverParams, serverPublicKey) = _e2eKeyService.GetSnapshot();
                var sharedSecret = E2EEncryptionHelper.DeriveSharedSecret(serverParams, clientPublicKey);
                var aesKey = E2EEncryptionHelper.DeriveAesKey(sharedSecret);

                request.EnableBuffering();
                request.Body.Position = 0;
                using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                if (request.Headers.TryGetValue(RequestEncryptedHeader, out var encryptedValue) &&
                    encryptedValue.ToString().Equals("true", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(requestBody))
                {
                    var decryptedBody = E2EEncryptionHelper.Decrypt(aesKey, requestBody);
                    var decryptedBytes = Encoding.UTF8.GetBytes(decryptedBody);
                    request.Body = new MemoryStream(decryptedBytes);
                    request.Body.Position = 0;
                    request.ContentLength = decryptedBytes.Length;
                }
                else if (!string.IsNullOrWhiteSpace(requestBody))
                {
                    var bodyBytes = Encoding.UTF8.GetBytes(requestBody);
                    request.Body = new MemoryStream(bodyBytes);
                    request.Body.Position = 0;
                }
                else
                {
                    request.Body = new MemoryStream();
                }

                context.HttpContext.Items[E2EContextKey] = new E2EContext
                {
                    AesKey = aesKey,
                    ServerPublicKey = serverPublicKey
                };
            }
            catch (CryptographicException ex)
            {
                _logger.LogWarning(ex, "E2E GCM authentication failed for {Path}", request.Path);
                RestoreBody(request, requestBody);
                context.Result = new BadRequestObjectResult(new { error = "E2E decryption failed." });
                return;
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "E2E public key is not valid base64 for {Path}", request.Path);
                RestoreBody(request, requestBody);
                context.Result = new BadRequestObjectResult(new { error = "Invalid E2E public key format." });
                return;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "E2E public key has invalid structure for {Path}", request.Path);
                RestoreBody(request, requestBody);
                context.Result = new BadRequestObjectResult(new { error = "Invalid E2E public key." });
                return;
            }

            await next();
        }

        private static void RestoreBody(HttpRequest request, string body)
        {
            if (body != null)
            {
                var bodyBytes = Encoding.UTF8.GetBytes(body);
                request.Body = new MemoryStream(bodyBytes);
                request.Body.Position = 0;
                request.ContentLength = bodyBytes.Length;
            }
        }
    }
}
