using TradeWeb.API.Models;

namespace TradeWeb.API.Repository
{
    public interface ISubscriptionRepository
    {
        public dynamic Subscription_Add(string userId, SubscriptionAddModel Model);
        public dynamic Subscription_Discontinue(SubscriptionDiscontinueModel Model, string UserId);
        public dynamic Subscription_Status(SubscriptionStatusModel Model);
        public dynamic Subscription_Delete(SubscriptionDiscontinueModel Model, string UserId);
    }
}
