using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using SolutionsTools.DAL;
using System.Net;

namespace SolutionsTools.Services
{
    public class ClientServices
    {
        private string _url = @"https://api.vtex.com/lojabestoff/dataentities/";
        private string _key = "vtexappkey-lojabestoff-HNEDDW";
        private string _token = "ISVOADDGTWJASQLZKWCSBXPHKILTSGKSSLJESYMHPZHVUULRWEEBTLLLCNDSSJONMRHGGGHMKUCBQUNQXUXNBJYHAYCEUTPFLTJCNEKPXRRZIJUIANUXMALDTZGHEDRQ";
        private string _fields = "id,partnerId,firstName,lastName,document,email,gender,birthDate,company,previousCompany,cargo,department,approved";
        private string _entity = "CL";
        public Cliente GetById(Guid id)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls11;

            Cliente cliente = null;

            var service = new RestClient(_url);

            var request_service = string.Format("{0}/documents/{1}", _entity, id);

            var request = new RestRequest(request_service, Method.GET);
            request.AddHeader("x-vtex-api-appkey", _key);
            request.AddHeader("x-vtex-api-apptoken", _token);
            request.AddParameter("_fields", _fields, ParameterType.QueryString);

            IRestResponse response = service.Execute(request);

            if (!response.Content.Equals("[]"))
                cliente = JsonConvert.DeserializeObject<Cliente>(response.Content);

            return cliente;
        }
        public Cliente GetByCpf(string cpf)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls11;

            Cliente cliente = null;

            var service = new RestClient(_url);

            var request_service = string.Format("{0}/search", _entity);

            var request = new RestRequest(request_service, Method.GET);
            request.AddHeader("x-vtex-api-appkey", _key);
            request.AddHeader("x-vtex-api-apptoken", _token);
            request.AddParameter("_fields", _fields, ParameterType.QueryString);
            request.AddParameter("_where", "document=" + cpf, ParameterType.QueryString);

            IRestResponse response = service.Execute(request);

            if (!response.Content.Equals("[]"))
                cliente = JsonConvert.DeserializeObject<List<Cliente>>(response.Content).First();

            return cliente;
        }
        public Cliente GetByEmail(string email)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls11;

            Cliente cliente = null;

            var service = new RestClient(_url);

            var request_service = string.Format("{0}/search", _entity);

            var request = new RestRequest(request_service, Method.GET);
            request.AddHeader("x-vtex-api-appkey", _key);
            request.AddHeader("x-vtex-api-apptoken", _token);
            request.AddParameter("_fields", _fields, ParameterType.QueryString);
            request.AddParameter("_where", "email=" + email, ParameterType.QueryString);

            IRestResponse response = service.Execute(request);

            if ((response.StatusCode == HttpStatusCode.OK) && !response.Content.Equals("[]"))
                cliente = JsonConvert.DeserializeObject<List<Cliente>>(response.Content).First();

            return cliente;
        }
        public List<Cliente> GetByCompany(Guid partnerId) 
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls11;

            var clientes = new List<Cliente>();

            var service = new RestClient(_url);

            var request_service = string.Format("{0}/scroll", _entity);

            var request = new RestRequest(request_service, Method.GET);
            request.AddHeader("x-vtex-api-appkey", _key);
            request.AddHeader("x-vtex-api-apptoken", _token);
            
            request.AddParameter("_size", "0", ParameterType.QueryString);

            request.AddParameter("_fields", _fields, ParameterType.QueryString);

            var partner = partnerId.Equals(new Guid()) ? "partnerId is not null" : string.Format("partnerId={0}", partnerId);

            request.AddParameter("_where", partner, ParameterType.QueryString);
           
            IRestResponse response = service.Execute(request);

            if ((response.StatusCode == HttpStatusCode.OK) && (response.Content != "[]"))
            {
                clientes = JsonConvert.DeserializeObject<List<Cliente>>(response.Content);
            }
            return clientes;
        }

        public List<Cliente> GetByCompanyTabocas()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls11;

            var clientes = new List<Cliente>();

            var service = new RestClient(_url);

            var request_service = string.Format("{0}/search", _entity);

            var request = new RestRequest(request_service, Method.GET);
            request.AddHeader("x-vtex-api-appkey", _key);
            request.AddHeader("x-vtex-api-apptoken", _token);

            request.AddParameter("_fields", "id,firstName,company", ParameterType.QueryString);

            request.AddParameter("_where", "partnerId=044205D4-B1CD-11EB-82AC-0E41C9B6D60F", ParameterType.QueryString);

            request.AddParameter("_sort", "firstName asc", ParameterType.QueryString);

            IRestResponse response = service.Execute(request);

            if ((response.StatusCode == HttpStatusCode.OK) && (response.Content != "[]"))
            {
                clientes = JsonConvert.DeserializeObject<List<Cliente>>(response.Content);
            }
            return clientes;
        }

        public List<Cliente> GetOnlyOneTime()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls11;

            var clientes = new List<Cliente>();

            var service = new RestClient(_url);

            var request_service = string.Format("{0}/scroll", _entity);

            var request = new RestRequest(request_service, Method.GET);
            request.AddHeader("x-vtex-api-appkey", _key);
            request.AddHeader("x-vtex-api-apptoken", _token);

            request.AddParameter("_size", "0", ParameterType.QueryString);

            request.AddParameter("_fields", _fields, ParameterType.QueryString);

            request.AddParameter("_where", "partnerId is not null", ParameterType.QueryString);

            IRestResponse response = service.Execute(request);

            if (!response.Content.Equals("[]"))
            {
                clientes = JsonConvert.DeserializeObject<List<Cliente>>(response.Content);
            }
            return clientes;
        }

        public ReturnServices AddClient(Cliente client)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls11;

            var ret = new ReturnServices();

            var service = new RestClient(_url);

            var request_service = string.Format("{0}/documents", _entity); 

            var request = new RestRequest(request_service, Method.POST);
            request.AddHeader("x-vtex-api-appkey", _key);
            request.AddHeader("x-vtex-api-apptoken", _token);
            
            var obj = PrepareRequestBody(client);
            request.AddJsonBody(obj);

            IRestResponse response = service.Execute(request);

            if (!response.Content.Equals("[]"))
                ret = JsonConvert.DeserializeObject<ReturnServices>(response.Content);

            return ret;
        }
        public ReturnServices UpdClient(Cliente client)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls11;

            var ret = new ReturnServices();

            var service = new RestClient(_url);

            var request_service = string.Format("{0}/documents", _entity);

            var request = new RestRequest(request_service, Method.PUT);
            request.AddHeader("x-vtex-api-appkey", _key);
            request.AddHeader("x-vtex-api-apptoken", _token);

            var obj = PrepareRequestBody(client);
            request.AddJsonBody(obj);

            IRestResponse response = service.Execute(request);

            if (!string.IsNullOrEmpty(response.Content))
                ret = JsonConvert.DeserializeObject<ReturnServices>(response.Content);

            return ret;
        }
        public void DelClient(Guid id)
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
        private object PrepareRequestBody(Cliente client)
        {
            dynamic obj = new ExpandoObject();

            var dtNasc = new DateTime();

            obj.cargo = client.Cargo;
            obj.company = client.Empresa;
            obj.previousCompany = client.EmpresaAnterior;
            obj.partnerId = client.EmpresaId;
            obj.gender = client.Sexo;
            obj.department = client.Departamento;
            obj.firstName = client.Nome;
            obj.lastName = client.Sobrenome;
            obj.approved = client.Aprovado;

            if (DateTime.TryParse(client.Dt_Nascimento, out dtNasc))
                obj.birthDate = dtNasc.ToString("yyyy-MM-dd HH:mm:ss");

            if (!string.IsNullOrEmpty(client.Email))
                obj.email = client.Email;

            if (!string.IsNullOrEmpty(client.Cpf))
                obj.document = client.Cpf;

            if (!client.Id_Cliente.Equals(new Guid()))
                obj.id = client.Id_Cliente;

            return obj;
        }
    }
}
