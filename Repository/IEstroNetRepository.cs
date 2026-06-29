using TradeWeb.API.Models;

namespace TradeWeb.API.Repository
{
    public interface IEstroNetRepository
    {
        public dynamic GetFilterSql(Filter filter);
    }
}
