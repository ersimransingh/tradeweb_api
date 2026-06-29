using System.Collections.Generic;

namespace TradeWeb.API.Models
{
    public class ModificationRequestDetailResponse
    {
        public List<Modification> Details { get; set; }
    }
    public class Modification
    {
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string GrossAnnualIncome { get; set; }
    }

    public class AccountCloserModel
    {
        public string clientCode { get; set; }
        public string dematAccount { get; set; }
        public string closureType { get; set; }
    }


}
