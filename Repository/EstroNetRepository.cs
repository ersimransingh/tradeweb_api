using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using System.Linq;
using TradeWeb.API.Models;

namespace TradeWeb.API.Repository
{
    public class EstroNetRepository : IEstroNetRepository
    {
        #region Class level declarations.
        private readonly IConfiguration _configuration;
        private readonly UtilityCommon objUtility;
        private string strsql = "";
        string strToken = string.Empty;
        ////NVPLSoapClient nVPLSoapClient = new NVPLSoapClient(EndpointConfiguration.INVPLSoap);
        IHttpContextAccessor _httpContextAccessor;
        // private readonly IWebHostEnvironment _environment;
        private readonly IWebHostEnvironment _environment;
        #endregion

        #region Constructor
        public EstroNetRepository(IConfiguration configuration, UtilityCommon objUtility, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            this.objUtility = objUtility;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
        }
        #endregion

        public dynamic GetFilterSql(Filter filter)
        {
            string strClientWhere = "";
            if (filter.Client != null)
            {
                if (filter.Client.All(y => y != ""))
                {
                    var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(filter.Client.ToArray(), "##"));
                    strClientWhere += " or cm_cd in('" + fltr.Replace("##", "','") + "')";
                }
            }
            if (filter.Branch != null)
            {
                if (filter.Branch.All(y => y != ""))
                {
                    var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(filter.Branch.ToArray(), "##"));
                    strClientWhere += " or cm_brboffcode in('" + fltr.Replace("##", "','") + "')";
                }
            }
            if (filter.Group != null)
            {
                if (filter.Group.All(y => y != ""))
                {
                    var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(filter.Group.ToArray(), "##"));
                    strClientWhere += " or cm_groupcd in('" + fltr.Replace("##", "','") + "')";
                }
            }
            if (filter.Family != null)
            {
                if (filter.Family.All(y => y != ""))
                {
                    var fltr = objUtility.mfnReplaceForSQLInjection(Strings.Join(filter.Family.ToArray(), "##"));
                    strClientWhere += " or cm_familycd in('" + fltr.Replace("##", "','") + "')";
                }
            }
            if (strClientWhere.Length > 0)
            {
                strClientWhere = " and (" + strClientWhere.Substring(3) + ") ";
            }

            return strClientWhere;
        }
    }
}
