using Newtonsoft.Json;
using RestSharp;
using SolutionsTools.DAL.Class;
using System.Collections.Generic;
using System.Net;
using SolutionsTools.DAL;

namespace SolutionsTools.Services
{
    public class OrderServices
    {
        private string _url = @"https://lojabestoff.vtexcommercestable.com.br/api/oms/pvt/";
        private string _key = "vtexappkey-lojabestoff-HNEDDW";
        private string _token = "ISVOADDGTWJASQLZKWCSBXPHKILTSGKSSLJESYMHPZHVUULRWEEBTLLLCNDSSJONMRHGGGHMKUCBQUNQXUXNBJYHAYCEUTPFLTJCNEKPXRRZIJUIANUXMALDTZGHEDRQ";
        //private string _fields = "_all";
        //https://lojabestoff.vtexcommercestable.com.br/api/oms/pvt/orders/1118200538800-01
        //https://lojabestoff.vtexcommercestable.com.br/api/oms/pvt/orders?q=edson.arantes.nascimento.pele33@gmail.com
        public List<Pedido> GetByOrderNo(string orderNo)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls11;

            List<Pedido> pedidos = null;

            var service = new RestClient(_url);

            var request_service = string.Format("/orders/" + orderNo);

            var request = new RestRequest(request_service, Method.GET);
            request.AddHeader("x-vtex-api-appkey", _key);
            request.AddHeader("x-vtex-api-apptoken", _token);
            //request.AddParameter("_fields", _fields, ParameterType.QueryString);

            IRestResponse response = service.Execute(request);

            if (!response.Content.Equals("[]"))
                pedidos = JsonConvert.DeserializeObject<List<Pedido>>(response.Content);

            return pedidos;
        }
        public ListaPedido GetByEmail(string email)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls11;

            ListaPedido pedidos = null;

            var service = new RestClient(_url);

            var request_service = string.Format("/orders?q=" + email);

            var request = new RestRequest(request_service, Method.GET);
            request.AddHeader("x-vtex-api-appkey", _key);
            request.AddHeader("x-vtex-api-apptoken", _token);

            IRestResponse response = service.Execute(request);

            if (!response.StatusCode.Equals(HttpStatusCode.NotFound))
                pedidos = JsonConvert.DeserializeObject<ListaPedido>(response.Content);
                
            return pedidos;
        }
    }
}
