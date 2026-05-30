using SolutionsTools.DAL;
using SolutionsTools.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace SolutionsTools.UI
{
    /// <summary>
    /// Summary description for FileProcessHandler
    /// </summary>
    public class FileProcessHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            try
            {
                if (context.Request.RequestType == "POST")
                {
                    var cliente = new DAL.Cliente();
                    cliente.Nome = context.Request.Form["Nome"].Trim();
                    cliente.Sobrenome = context.Request.Form["Sobrenome"].Trim();
                    cliente.Cpf = context.Request.Form["Cpf"];
                    cliente.Email = context.Request.Form["Email"];
                    cliente.Sexo = context.Request.Form["Sexo"];
                    cliente.Dt_Nascimento = context.Request.Form["Dt_Nascimento"];
                    cliente.Departamento = context.Request.Form["Departamento"];
                    cliente.Cargo = context.Request.Form["Cargo"];
                    cliente.Acao = context.Request.Form["Acao"];
                    cliente.Empresa = context.Request.Form["Empresa"];
                    cliente.EmpresaId = Guid.Parse(context.Request.Form["EmpresaId"]);
                    cliente.Aprovado = false;

                    //ajustes necessários / validações: email, cpf

                    cliente.Cpf = Utils.FormatCPF(cliente.Cpf);

                    if (!Utils.IsCpf(cliente.Cpf) && !string.IsNullOrEmpty(cliente.Cpf))
                    {
                        context.Response.Write(string.Format("O CPF '{0}' é invalido!", cliente.Cpf));
                        return;
                    }

                    cliente.Email = cliente.Email.Replace(" ", "");

                    if (!Utils.IsEmail(cliente.Email) && !string.IsNullOrEmpty(cliente.Email))
                    {
                        context.Response.Write(string.Format("O Email '{0}' é invalido!", cliente.Email));
                        return;
                    }

                    cliente.Acao = Regex.Replace(cliente.Acao, "[^0-9a-zA-Z_]+", "");

                    //verificar se existe e exec ações

                    var dal = new DAL.ClienteDAL();

                    var md = new CryptographyMD5();

                    var cpf_hash = md.ReturnMD5(cliente.Cpf);

                    var cliRet = dal.GetByCpf(cpf_hash);

                    if (cliRet == null)
                        cliRet = dal.GetByEmail(cliente.Email);

                    if (cliRet == null)
                    {
                        if (cliente.Acao.Equals("inserir"))
                        {
                            //InsertClient(cliente, cpf_hash);
                            context.Response.Write("Cliente inserido com sucesso.");
                        }
                        else
                        {
                            context.Response.Write(string.Format("Não foi possível {0}, pois o cliente não existe na base.", cliente.Acao));
                        }
                    }
                    else
                    {
                        if (cliente.Acao.Equals("atualizar"))
                        {
                            //UpdateClient(cliente, cliRet);
                            context.Response.Write("Cliente atualizado com sucesso.");
                        }
                        else if (cliente.Acao.Equals("excluir"))
                        {
                            //UpdateClientCompany(cliRet);
                            context.Response.Write("Cliente excluído com sucesso.");
                        }
                        else
                        {
                            context.Response.Write("Não foi possível inserir, pois o cliente já existe na base.");
                        }
                    }
                       
                }
                else
                {
                    context.Response.Write("Erro: Request was sent incorrectly somehow");
                }

            }
            catch (Exception ex)
            {
                context.Response.Write("Error: " + ex.Message);
            }
        }
        private void InsertClient(Cliente csv, string cpf_hash)
        {
            var movto = "I";

            var cs = new ClientServices();

            var ret = cs.AddClient(csv);

            if (ret.DocumentId == new Guid())
            {
                throw new Exception(ret.Message);
            }

            var cli = new ClienteDAL();
            csv.Id_Cliente = ret.DocumentId;
            csv.Id_Parceiro = csv.EmpresaId;
            csv.Cpf = cpf_hash;
            csv.Ativo = "S";
            cli.Insert(csv);

            Utils.Log(EntityType.CL, ret.DocumentId, movto);
        }
        private void UpdateClient(Cliente csv, Cliente cliRet)
        {
            var movto = "U";

            var cs = new ClientServices();

            var client = cs.GetById(cliRet.Id_Cliente);
            client.Cargo = csv.Cargo;
            client.Departamento = csv.Departamento;

            cs.UpdClient(client);

            Utils.Log(EntityType.CL, cliRet.Id_Cliente, movto);
        }
        private void UpdateClientCompany(Cliente cliRet)
        {
            var movto = "U";

            var cs = new ClientServices();

            var cliente = cs.GetById(cliRet.Id_Cliente);

            var dal = new EmpresaDAL();
            var empresa = dal.GetByCnpj("00000000000000");

            cliente.EmpresaAnterior = cliente.Empresa;
            cliente.Empresa = empresa.Nome_Fantasia;
            cliente.EmpresaId = empresa.Id_Parceiro;
            var ret = cs.UpdClient(cliente);

            var cli = new ClienteDAL();
            cliente = cli.GetByIdCliente(cliRet.Id_Cliente);
            cliente.Id_Cliente = ret.DocumentId;
            cliente.Id_Parceiro = empresa.Id_Parceiro;
            cli.Update(cliente);

            Utils.Log(EntityType.CL, cliRet.Id_Cliente, movto);
        }
        private string ConvertGender(string gender)
        {
            var convertGender = string.IsNullOrEmpty(gender) ? string.Empty : gender;

            convertGender = convertGender.Length == 0 ? string.Empty : convertGender.Substring(0, 1).ToUpper();

            switch (convertGender)
            {
                case "M":
                    return "male";
                case "F":
                    return "female";
                default:
                    return "not-to-say";
            }
        }
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}