using FluentValidation;
using SolutionsTools.DAL;
using SolutionsTools.Services;
using SolutionsTools.WebApi.Models;
using System;
using System.Linq;
using System.Web.Http;

namespace SolutionsTools.WebApi.Controllers
{
    [RoutePrefixAttributeCustom("client")]
    public class ClientController : ApiController
    {
        [HttpGet]
        [Route("GetByEmail")]
        public Response GetByEmail(string token, string email)
        {
            var response = new Response();

            var tm = new TokenManager();

            var ret = tm.ValidaToken(token);

            if (ret.ret_code != 0)
            {
                response.status = false;
                response.message = ret.ret_info;
                return response;
            }

            if (string.IsNullOrEmpty(email))
            {
                response.status = false;
                response.message = "O parâmetro 'Email' é obrigatório para essa ação.";
                return response;
            }

            var cs = new ClientServices();

            var cliente = cs.GetByEmail(email);

            response.status = true;
            response.message = ret.ret_info;
            response._return = cliente;

            return response;
        }

        [HttpGet]
        [Route("GetByCpf")]
        public Response GetByCpf(string token, string cpf)
        {
            var response = new Response();

            var tm = new TokenManager();

            var ret = tm.ValidaToken(token);

            if (ret.ret_code != 0)
            {
                response.status = false;
                response.message = ret.ret_info;
                return response;
            }

            if (string.IsNullOrEmpty(cpf))
            {
                response.status = false;
                response.message = "O parâmetro 'CPF' é obrigatório para essa ação.";
                return response;
            }

            var cs = new ClientServices();

            var cliente = cs.GetByCpf(cpf);

            response.status = true;
            response.message = ret.ret_info;
            response._return = cliente;

            return response;
        }

        [HttpGet]
        [Route("GetByCompany")]
        public Response GetByCompany(string token, string cnpj)
        {
            var response = new Response();

            var tm = new TokenManager();

            var ret = tm.ValidaToken(token);

            if (ret.ret_code != 0)
            {
                response.status = false;
                response.message = ret.ret_info;
                return response;
            }

            if (string.IsNullOrEmpty(cnpj))
            {
                response.status = false;
                response.message = "O parâmetro 'CNPJ' é obrigatório para essa ação.";
                return response;
            }

            var dal = new EmpresaDAL();
            var empresa = dal.GetByCnpj(cnpj);

            var cs = new ClientServices();

            var cliente = cs.GetByCompany(empresa.Id_Parceiro);

            response.status = true;
            response.message = ret.ret_info;
            response._return = cliente;

            return response;
        }

        [HttpPost]
        [Route("AddCompany")]
        public Response AddCompany(string token, Client cliente)
        {
            var response = new Response();

            var tm = new TokenManager();

            var ret = tm.ValidaToken(token);

            if (ret.ret_code != 0)
            {
                response.status = false;
                response.message = ret.ret_info;
                return response;
            }

            var login = ret.ret_info;

            var validator = new ClientValidator();

            var valResults = validator.Validate(cliente, options => options.IncludeRuleSets("AddRule"));

            if (valResults.IsValid)
            {
                var cs = new ClientServices();

                var retCpf = cs.GetByCpf(cliente.Cpf);

                if (!retCpf.Id.Equals(new Guid()))
                {
                    response.status = false;
                    response.message = "Cliente já existe na base.";
                    response._return = retCpf.Id;
                    return response;
                }
                else
                {
                    var retEmail = cs.GetByEmail(cliente.Email);

                    if (!retEmail.Id.Equals(new Guid()))
                    {
                        response.status = false;
                        response.message = "Cliente já existe na base.";
                        response._return = retEmail.Id;
                        return response;
                    }
                }

                var dal = new EmpresaDAL();
                var empresa = dal.GetByCnpj(cliente.Cnpj);

                var clienteDto = new Cliente 
                { 
                    Nome = cliente.Nome,
                    Sobrenome = cliente.Sobrenome,
                    Cpf = cliente.Cpf,
                    Email = cliente.Email,
                    Empresa = empresa.Nome_Fantasia,
                    EmpresaId = empresa.Id_Parceiro,
                    Departamento = cliente.Departamento,
                    Cargo = cliente.Cargo,
                    Aprovado = false
                };

                var retCs = cs.AddClient(clienteDto);

                if (string.IsNullOrEmpty(retCs.Href))
                {
                    response.status = true;
                    response.message = "Cliente inserido";
                    response._return = retCs.DocumentId;
                    Utils.Log(login, EntityType.CL, retCs.DocumentId.ToString(), "I");
                }
                else
                {
                    response.status = false;
                    response.message = "Cliente não inserido";
                }
            }
            else
            {
                var erros = valResults.Errors.Select(m => m.ErrorMessage).Aggregate((a, b) => a + " - " + b);
                response.status = true;
                response.message = string.Format("Erro ao validar o objeto de entrada. Erro(s): {0}", erros);
            }

            return response;
        }

        [HttpPut]
        [Route("UpdateCompany")]
        public Response UpdCompany(string token, Client cliente)
        {
            var response = new Response();

            var tm = new TokenManager();

            var ret = tm.ValidaToken(token);

            if (ret.ret_code != 0)
            {
                response.status = false;
                response.message = ret.ret_info;
            }

            var login = ret.ret_info;

            var validator = new ClientValidator();

            var valResults = validator.Validate(cliente, options => options.IncludeRuleSets("UpdRule"));

            if (valResults.IsValid)
            {
                var cs = new ClientServices();

                var clienteNovo = cs.GetByCpf(cliente.Cpf);

                clienteNovo.Departamento = cliente.Departamento;
                clienteNovo.Cargo = cliente.Cargo;

                var retCs = cs.UpdClient(clienteNovo);

                response.status = true;
                response.message = "Cliente atualizado";
                response._return = retCs.DocumentId;

                Utils.Log(login, EntityType.CL, retCs.DocumentId.ToString(), "U");

                return response;
            }
            else
            {
                var erros = valResults.Errors.Select(m => m.ErrorMessage).Aggregate((a, b) => a + " - " + b);
                response.status = true;
                response.message = string.Format("Erro ao validar o objeto de entrada. Erro(s): {0}", erros);
                return response;
            }
        }

        [HttpDelete]
        [Route("DeleteCompany")]
        public Response DelCompany(string token, Client cliente)
        {
            var response = new Response();

            var tm = new TokenManager();

            var ret = tm.ValidaToken(token);

            if (ret.ret_code != 0)
            {
                response.status = false;
                response.message = ret.ret_info;
            }

            var login = ret.ret_info;

            var validator = new ClientValidator();

            var valResults = validator.Validate(cliente, options => options.IncludeRuleSets("DelRule"));

            if (valResults.IsValid)
            {
                var cs = new ClientServices();

                var clienteNovo = cs.GetByCpf(cliente.Cpf);

                //cs.DelClient(clienteNovo.Id);

                response.status = true;
                response.message = "Cliente excluído";
                response._return = clienteNovo.Id;

                Utils.Log(login, EntityType.CL, clienteNovo.Id.ToString(), "D");

                return response;
            }
            else
            {
                var erros = valResults.Errors.Select(m => m.ErrorMessage).Aggregate((a, b) => a + " - " + b);
                response.status = true;
                response.message = string.Format("Erro ao validar o objeto de entrada. Erro(s): {0}", erros);
                return response;
            }
        }
    }
}