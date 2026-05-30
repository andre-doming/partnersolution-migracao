using SolutionsTools.DAL;
using SolutionsTools.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace SolutionsTools.UI
{
    public partial class Importar : BaseWebUi
    {
        private List<Cliente> clientes { get => (List<Cliente>)Session["Clientes"]; set => Session["Clientes"] = value; }
        private StringBuilder erros  { get => (StringBuilder)Session["Erros"]; set => Session["Erros"] = value; }
        private Color color_ok = Color.FromArgb(184, 212, 224);
        private Color color_error = Color.FromArgb(251, 207, 208);
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                Utils.LoadCompanies(mselEmpresas, false);
            }

            if (!string.IsNullOrEmpty(Request.QueryString["downloadReportErrors"]))
            {
                ExportReport(erros);
            }
        }
        protected void btnLerArquivo_Click(object sender, EventArgs e)
        {
            if (_isRefresh)
                return;

            try
            {
                ViewExportLog(false);

                var dir = Server.MapPath("~/Upload");

                clientes = FileHandling.ReadFile<Cliente>(fupClientes, Convert.ToChar(";"));

                PrepareClient(clientes);

                LoadClientes(clientes);
            }
            catch (Exception ex)
            {
                Tools.ShowMessage(this.Page, Page.GetType(), "Erro: " + ex.Message, Tools.MessageType.Warning);
            }
        }
        protected void btnProcessar_Click(object sender, EventArgs e)
        {
            if (_isRefresh)
                return;

            try
            {
                ProcessClients();
                ViewExportLog(true);
                Tools.ShowMessage(this.Page, Page.GetType(), "Operação finalizada!", Tools.MessageType.Info);
            }
            catch (Exception ex)
            {
                Tools.ShowMessage(this.Page, Page.GetType(), "Erro: " + ex.Message, Tools.MessageType.Error);
            }

        }
        private void PrepareClient(List<Cliente> clientes)
        {
            foreach (var cliente in clientes)
            {
                if (string.IsNullOrEmpty(cliente.Sobrenome))
                {
                    var nomes = cliente.Nome.Split(' ');
                    if (nomes.Length > 1)
                    {
                        cliente.Sobrenome = cliente.Nome.Replace(nomes[0], "").Trim();
                        cliente.Nome = nomes[0];
                    }
                }
            }
        }
        private void LoadClientes(List<Cliente> clientes)
        {
            grvClientes.DataSource = clientes;

            grvClientes.DataBind();

            ViewProcessClient(clientes);
        }
        private void ViewProcessClient(List<Cliente> clientes)
        {
            if (clientes.Count > 0)
            {
                btnProcessar.Visible = true;
            }
            else
            {
                Tools.ShowMessage(this.Page, Page.GetType(), "Não foram encontrados clientes no arquivo csv!", Tools.MessageType.Info);
            }
        }
        private void ViewExportLog(bool visible)
        {
            if ((erros != null) && visible)
            {
                divExportReport.Visible = true;
            }
            else
            {
                erros = null;
                divExportReport.Visible = false;
            }
        }
        private void ProcessClients()
        {
            foreach (GridViewRow row in grvClientes.Rows)
            {
                var chk = (HtmlInputCheckBox)row.FindControl("chkSelecionado");

                if (chk.Checked)
                {
                    var id = Convert.ToInt32(grvClientes.DataKeys[row.RowIndex].Values[0]);
                    var csv = clientes.Where(c => c.SeqId.Equals(id)).First();

                    try
                    {
                        var cpf = Utils.FormatCPF(grvClientes.DataKeys[row.RowIndex].Values[1].ToString());

                        if (!string.IsNullOrEmpty(cpf))
                        {
                            if (!Utils.IsCpf(cpf))
                            {
                                throw new Exception(string.Format("O CPF '{0}' é invalido!", cpf));
                            }
                            else
                            {
                                csv.Cpf = cpf;
                            }
                        }
                        
                        var email = grvClientes.DataKeys[row.RowIndex].Values[2].ToString();

                        var acao = Regex.Replace(grvClientes.DataKeys[row.RowIndex].Values[3].ToString().ToLower(), "[^0-9a-zA-Z_]+", "");

                        var cs = new ClientServices();

                        var dal = new ClienteDAL();

                        Cliente cliRet = null;

                        var md = new CryptographyMD5();

                        var cpf_hash = string.IsNullOrEmpty(cpf) ? null : md.ReturnMD5(cpf);

                        if (!string.IsNullOrEmpty(cpf_hash))
                            cliRet = dal.GetByCpf(cpf_hash);

                        email = email.Replace(" ", "");

                        if (!Utils.IsEmail(email) && !string.IsNullOrEmpty(email))
                        {
                            throw new Exception(string.Format("O Email '{0}' é invalido!", csv.Email));
                        }

                        if (cliRet == null && !string.IsNullOrEmpty(email))
                            cliRet = dal.GetByEmail(email); 

                        if (cliRet == null)
                        {
                            csv.Email = email;

                            if (acao.Equals("inserir"))
                            {
                                InsertClient(csv, cpf_hash);

                                row.BackColor = color_ok;
                                row.ToolTip = "Cliente inserido";
                            }
                            else
                            {
                                row.BackColor = color_error;
                                row.ToolTip = string.Format("Não foi possível {0}, pois o cliente não existe na base.", acao.ToLower());
                                PutErrosReport(id.ToString(), csv.Nome, csv.Sobrenome, row.ToolTip);
                            }
                        }
                        else
                        {
                            if (acao.Equals("atualizar"))
                            {
                                UpdateClient(csv, cliRet);

                                row.BackColor = color_ok;
                                row.ToolTip = "Cliente atualizado";
                            }
                            else if (acao.Equals("excluir"))
                            {
                                UpdateClientCompany(cliRet);

                                row.BackColor = color_ok;
                                row.ToolTip = "Cliente excluído";
                            }
                            else
                            {
                                row.BackColor = color_error;
                                row.ToolTip = "Não foi possível inserir, pois o cliente já existe na base.";
                                PutErrosReport(id.ToString(), csv.Nome, csv.Sobrenome, row.ToolTip);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        row.BackColor = color_error;
                        row.ToolTip = "Erro: " + ex.Message;
                        PutErrosReport(id.ToString(), csv.Nome, csv.Sobrenome, row.ToolTip);
                    }
                }
            }
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
        private void InsertClient(Cliente csv, string cpf_hash)
        {
            var movto = "I";

            var dal = new EmpresaDAL();
            var empresa = dal.GetById(Convert.ToInt32(mselEmpresas.Value));

            var cliente = new Cliente
            {
                Nome = csv.Nome.Trim(),
                Sobrenome = csv.Sobrenome.Trim(),
                Cpf = csv.Cpf,
                Email = csv.Email,
                Sexo = ConvertGender(csv.Sexo),
                Dt_Nascimento = csv.Dt_Nascimento,
                Cargo = csv.Cargo,
                Departamento = csv.Departamento,
                Empresa = empresa.Nome_Fantasia,
                EmpresaId = empresa.Id_Parceiro,
                Aprovado = false
            };

            var cs = new ClientServices();
            
            var ret = cs.AddClient(cliente);

            if (ret.DocumentId == new Guid())
            {
                throw new Exception(ret.Message);
            }

            var cli = new ClienteDAL();
            cliente.Id_Cliente = ret.DocumentId;
            cliente.Id_Parceiro = cliente.EmpresaId;
            cliente.Nome = csv.Nome;
            cliente.Sobrenome = csv.Sobrenome;
            cliente.Cpf = cpf_hash;
            cliente.Ativo = "S";
            cli.Insert(cliente);

            Utils.Log(EntityType.CL, ret.DocumentId, movto);
        }
        private void PutErrosReport(string line, string name, string sobrenome, string msg)
        {
            if (erros == null)
            {
                erros = new StringBuilder();
                erros.Append("linha;nome;sobrenome;erro;data\r\n");
            }
            
            var erro = string.Format("{0};{1};{2};{3};{4}\r\n", line, name, sobrenome, msg, DateTime.Now.ToString());

            erros.Append(erro);
        }
        private void ExportReport(StringBuilder reportErrors)
        {
            if (reportErrors == null)
            {
                Tools.ShowMessage(this.Page, Page.GetType(), "Não existem dados para exportar!", Tools.MessageType.Warning);
                return;            
            }

            string fileName = string.Format("Log_{0}.csv", DateTime.Now.ToString("yyyyMMddhhmmss"));

            // Add headers for a csv file or whatever
            Response.ContentType = "text/csv";
            Response.AddHeader("Content-Disposition", "attachment;filename=" + fileName);
            Response.AddHeader("Pragma", "no-cache");
            Response.AddHeader("Cache-Control", "no-cache");

            // Write the data as binary from a unicode string
            var buffer = System.Text.Encoding.Unicode.GetBytes(reportErrors.ToString());
            Response.BinaryWrite(buffer);

            // Sends the response buffer
            Response.Flush();

            // Prevents any other content from being sent to the browser
            Response.SuppressContent = true;

            // Directs the thread to finish, bypassing additional processing
            Response.End();

        }
    }
}