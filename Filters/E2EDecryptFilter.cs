using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TradeWeb.API.Helpers;

namespace TradeWeb.API.Filters
{
    public class E2EContext
    {
        public byte[] AesKey { get; set; }
    }

    public class E2EDecryptFilter : IAsyncActionFilter
    {
        public const string E2EContextKey = "E2E";
        public const string RequestEncryptedHeader = "x-e2e-request-encrypted";

        private readonly E2EKeyService _e2eKeyService;

        public E2EDecryptFilter(E2EKeyService e2eKeyService)
        {
            _e2eKeyService = e2eKeyService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            if (request.Headers.TryGetValue(E2EEncryptionHelper.PublicKeyHeader, out var publicKeyValue))
            {
                try
                {
                    var clientPublicKey = Convert.FromBase64String(publicKeyValue.ToString());
                    var sharedSecret = E2EEncryptionHelper.DeriveSharedSecret(_e2eKeyService.Parameters, clientPublicKey);
                    var aesKey = E2EEncryptionHelper.DeriveAesKey(sharedSecret);

                    request.EnableBuffering();
                    request.Body.Position = 0;
                    string requestBody;
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
                    }
                    else if (!string.IsNullOrWhiteSpace(requestBody))
                    {
                        request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
                        request.Body.Position = 0;
                    }
                    else
                    {
                        request.Body = new MemoryStream();
                    }

                    context.HttpContext.Items[E2EContextKey] = new E2EContext
                    {
                        AesKey = aesKey
                    };
                }
                catch
                {
                    // Not a valid E2E request; continue unencrypted.
                }
            }

            await next();
        }
    }
}
