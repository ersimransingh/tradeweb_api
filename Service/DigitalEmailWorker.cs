using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TradeWeb.API.Service
{
    public class DigitalEmailWorker : BackgroundService
    {
        private readonly Channel<DigitalEmailJob> _channel;
        private readonly IServiceScopeFactory _scopeFactory;

        public DigitalEmailWorker(Channel<DigitalEmailJob> channel, IServiceScopeFactory scopeFactory)
        {
            _channel = channel;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<DigitalEmailService>();

                SendRunResult result;
                try { result = await svc.RunSendAsync(job.JobId, job.UserId); }
                catch (Exception ex) { result = new SendRunResult(false, "Error: " + ex.Message, 0, 0); }

                await svc.FinishJobAsync(job.JobId, result);   // always frees the lock
            }
        }
    }
}
