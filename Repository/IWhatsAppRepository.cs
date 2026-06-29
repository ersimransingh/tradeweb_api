using TradeWeb.API.Models;

namespace TradeWeb.API.Repository
{
    public interface IWhatsAppRepository
    {
        public dynamic ChatBot(WhatsAppBotRequest req);
    }
}
