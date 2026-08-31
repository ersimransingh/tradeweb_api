namespace TradeWeb.API.Models
{
    public class NameMatchRequest
    {
        public string Name1 { get; set; }
        public string Name2 { get; set; }
    }

    public class NameMatchResult
    {
        public double TokenSortRatio { get; set; }
        public double TokenSetRatio { get; set; }
        public double JaroWinkler { get; set; }
        public double FinalScore { get; set; }
        public string MatchStatus { get; set; }
        public string Status { get; set; }
    }
}
