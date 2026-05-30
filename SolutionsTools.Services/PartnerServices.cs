using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using SolutionsTools.DAL;

namespace SolutionsTools.Services
{
    public class PartnerServices
    {
        private string _url = @"https://api.vtex.com/lojabestoff/dataentities/";
        private string _key = "vtexappkey-lojabestoff-HNEDDW";
        private string _token = "ISVOADDGTWJASQLZKWCSBXPHKILTSGKSSLJESYMHPZHVUULRWEEBTLLLCNDSSJONMRHGGGHMKUCBQUNQXUXNBJYHAYCEUTPFLTJCNEKPXRRZIJUIANUXMALDTZGHEDRQ";
        private string _fields = "_all";
        private string _entity = "PR"; 
        public Empresa GetById(Guid id) 
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls11;

            Empresa empresa = null;

            var service = new RestClient(_url);

            var request_service = string.Format("{0}/search", _entity); 

            var request = new RestRequest(request_service, Method.GET);
            request.AddHeader("x-vtex-api-appkey", _key);
            request.AddHeader("x-vtex-api-apptoken", _token);
            request.AddParameter("_fields", _fields, ParameterType.QueryString);
            request.AddParameter("_where", "id=" + id, ParameterType.QueryString);

            IRestResponse response = service.Execute(request);

            if (!response.Content.Equals("[]"))
                empresa = JsonConvert.DeserializeObject<List<Empresa>>(response.Content).First();

            return empresa;
        }
        public Empresa GetByCnpj(string cnpj)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls11;

            Empresa empresa = null;

            var service = new RestClient(_url);

            var request_service = string.Format("{0}/search", _entity); 

            var request = new RestRequest(request_service, Method.GET);
            request.AddHeader("x-vtex-api-appkey", _key);
            request.AddHeader("x-vtex-api-apptoken", _token);
            request.AddParameter("_fields", _fields, ParameterType.QueryString);
            request.AddParameter("_where", "cnpj=" + cnpj, ParameterType.QueryString);

            IRestResponse response = service.Execute(request);

            if (!response.Content.Equals("[]"))
                empresa = JsonConvert.DeserializeObject<List<Empresa>>(response.Content).First();

            return empresa;
        }
        public ReturnServices AddPartner(Empresa partner)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls11;

            var ret = new ReturnServices();

            var service = new RestClient(_url);

            var request_service = string.Format("{0}/documents", _entity); 

            var request = new RestRequest(request_service, Method.POST);
            request.AddHeader("x-vtex-api-appkey", _key);
            request.AddHeader("x-vtex-api-apptoken", _token);
            
            var obj = PrepareRequestBody(partner);
            request.AddJsonBody(obj);

            IRestResponse response = service.Execute(request);

            if (!string.IsNullOrEmpty(response.Content))
                ret = JsonConvert.DeserializeObject<ReturnServices>(response.Content);

            return ret;
        }
        public ReturnServices UpdPartner(Empresa partner)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls11;

            var ret = new ReturnServices();

            var service = new RestClient(_url);

            var request_service = string.Format("{0}/documents", _entity); 

            var request = new RestRequest(request_service, Method.PUT);
            request.AddHeader("x-vtex-api-appkey", _key);
            request.AddHeader("x-vtex-api-apptoken", _token);

            var obj = PrepareRequestBody(partner);
            request.AddJsonBody(obj);

            IRestResponse response = service.Execute(request);

            if (!string.IsNullOrEmpty(response.Content))
                ret = JsonConvert.DeserializeObject<ReturnServices>(response.Content);

            return ret;
        }
        public void DelPartner(Guid id)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls11;

            var ret = new ReturnServices();

            var service = new RestClient(_url);

            var request_service = string.Format("{0}/documents/{1}", _entity, id); 

            var request = new RestRequest(request_service, Method.DELETE);
            request.AddHeader("x-vtex-api-appkey", _key);
            request.AddHeader("x-vtex-api-apptoken", _token);

            service.Execute(request);
        }
        private object PrepareRequestBody(Empresa partner)
        {
            dynamic obj = new ExpandoObject();

            obj.CNPJ = partner.Cnpj;
            obj.NomeFantasia = partner.Nome_Fantasia;
            obj.RazaoSocial = partner.Razao_Social;
            obj.GerenteResponsavel = partner.Gerente_Responsavel;
            obj.Ativo = partner.Ativo.Equals("S") ? true : false;

            if (partner.Id_Parceiro != (new Guid()))
                obj.id = partner.Id_Parceiro;

            return obj;
        }
    }
}
