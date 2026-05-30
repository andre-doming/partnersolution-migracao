using SolutionsTools.DAL;
using SolutionsTools.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls; 

namespace SolutionsTools.UI
{
    public partial class Clientes : BaseWebUi
    {
        public Guid idCliente { get => (Guid)Session["IdCliente"]; set => Session["IdCliente"] = value; }
        public List<Cliente> clientes { get => (List<Cliente>)Session["Clientes"]; set => Session["Clientes"] = value; }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!base.Page.IsPostBack)
            {
                Utils.LoadCompanies(mselEmpresas, true);

                ShowSyncButton();
            }
        }
        protected void btnListar_Click(object sender, EventArgs e)
        {
            if (_isRefresh)
                return;

            FindClients("NewFind");
        }
        protected void btnExcluir_Click(object sender, EventArgs e)
        {
            if (_isRefresh)
                return;

            foreach (GridViewRow row in grvClientes.Rows)
            {
                var chk = (HtmlInputCheckBox)row.FindControl("chkSelecionado");

                if (chk.Checked)
                {
                    acModalDeleteClient.SetIdClient(string.Empty);

                    ShowModalDeleteClient(this.Page, Page.GetType());

                    break;
                }
            }

            Tools.ShowMessage(this.Page, Page.GetType(), "Selecione pelo um cliente para excluir!", Tools.MessageType.Warning);
        }
        protected void btnSincronizar_Click(object sender, EventArgs e)
        {
            var cs = new ClientServices();

            var clientes = cs.GetOnlyOneTime();

            var c = new ClienteDAL();

            foreach (var cliente in clientes)
            {
                var md = new CryptographyMD5();

                var cpf_hash = string.IsNullOrEmpty(cliente.Cpf) ? null : md.ReturnMD5(cliente.Cpf);

                cliente.Id_Parceiro = cliente.EmpresaId;
                cliente.Cpf = cpf_hash;
                cliente.Ativo = "S";
                c.Insert(cliente);
            }
        }
        protected void grvClientes_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName.Equals("Page"))
            {
                FindClients(e.CommandArgument.ToString());
            }
            else
            {
                int rowIndex = Convert.ToInt32(e.CommandArgument) - (grvClientes.PageIndex * grvClientes.PageSize);

                idCliente = Guid.Parse(grvClientes.DataKeys[rowIndex].Values[0].ToString());

                if (e.CommandName == "Upd")
                {
                    acModalClient.GetDataClient(idCliente, btnListar);

                    ShowModalUpdateClient(this.Page, Page.GetType());
                }
                if (e.CommandName == "Del")
                {

                    acModalDeleteClient.SetIdClient(idCliente.ToString());

                    ShowModalDeleteClient(this.Page, Page.GetType());
                }
            }
        }
        protected void grvClientes_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            var allowDel = UserDataAccess.IsAdmin ? true : UserDataAccess.HasFunction("funcClientesDel");

            if (e.Row.RowType == DataControlRowType.DataRow && 1==2)
            {
                var email = grvClientes.DataKeys[e.Row.RowIndex].Values[1] != null ? grvClientes.DataKeys[e.Row.RowIndex].Values[1].ToString() : string.Empty;

                allowDel = !(HasOrder(email));

                var ibtnDelete = (ImageButton)e.Row.FindControl("ibtnDelete");
                ibtnDelete.Enabled = allowDel;
                ibtnDelete.ImageUrl = allowDel ? "~/Img/delete.png" : "~/Img/delete_gray.png";

                var chk = (HtmlInputCheckBox)e.Row.FindControl("chkSelecionado");
                chk.Disabled = !allowDel;
                if (!allowDel)
                {
                    chk.Attributes["class"] = string.Empty;
                }
            }

            if (!UserDataAccess.IsAdmin && (e.Row.RowType == DataControlRowType.DataRow || e.Row.RowType == DataControlRowType.Header))
            {
                e.Row.Cells[7].Visible = false;
            }
        }
        protected void grvClientes_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            grvClientes.PageIndex = e.NewPageIndex;
            grvClientes.DataSource = clientes;
            grvClientes.DataBind();
        }
        private void ShowSyncButton()
        {
            btnSincronizar.Visible = false; //(UserDataAccess.IdUsuario == 1);
        }
        public void DeleteSelectedClients()
        {
            try
            {
                foreach (GridViewRow row in grvClientes.Rows)
                {
                    var chk = (HtmlInputCheckBox)row.FindControl("chkSelecionado");

                    if (chk.Checked)
                    {
                        var id = Guid.Parse(grvClientes.DataKeys[row.RowIndex].Values[0].ToString());
                        if (UserDataAccess.IsAdmin)
                        {
                            DeleteClient(id);
                        }
                        else
                        {
                            UpdateClient(id);
                        }
                    }
                }

                Tools.ShowMessage(this.Page, Page.GetType(), "Cliente(s) excluído(s)!", Tools.MessageType.Info);

                FindClients("NewFind");
            }
            catch (Exception ex)
            {
                Tools.ShowMessage(this.Page, Page.GetType(), "Erro: " + ex.Message, Tools.MessageType.Error);
            }
        }
        public void DeleteClient(Guid id)
        {
            var cs = new ClientServices();
            cs.DelClient(id);

            var cli = new ClienteDAL();
            var cliente = cli.GetByIdCliente(id);
            cliente.Ativo = "N";
            cli.Update(cliente);

            Utils.Log(EntityType.CL, id, "D");
        }
        public void UpdateClient(Guid id)
        {
            var cs = new ClientServices();

            var cliente = cs.GetById(id);

            var dal = new EmpresaDAL();
            var empresa = dal.GetByCnpj("00000000000000");

            cliente.EmpresaAnterior = cliente.Empresa;
            cliente.Empresa = empresa.Nome_Fantasia;
            cliente.EmpresaId = empresa.Id_Parceiro;
            cs.UpdClient(cliente);

            var cli = new ClienteDAL();
            cliente.Id_Parceiro = empresa.Id_Parceiro;
            cli.Update(cliente);

            Utils.Log(EntityType.CL, cliente.Id_Cliente, "U");
        }
        public void FindClients(string function)
        {
            var dal = new ClienteDAL();

            var id = GetCompanyId();

            if (function.Equals("NewFind"))
            {
                if (!string.IsNullOrEmpty(txtProcurarPor.Text))
                {
                    var procurarPor = txtProcurarPor.Text;

                    if (mselProcurarPor.Value.Equals("CPF") && !string.IsNullOrEmpty(txtProcurarPor.Text))
                    {
                        var md = new CryptographyMD5();

                        procurarPor = md.ReturnMD5(txtProcurarPor.Text);
                    }

                    clientes = dal.GetByCompanyAndCustom(id, mselProcurarPor.Value, procurarPor);
                }
                else 
                {
                    clientes = dal.GetByCompany(id);
                }

                clientes = clientes.OrderBy(c => c.Nome + c.Sobrenome).ToList();
            }

            int paginaDe = 0;
            int paginaAte = 0;

            SetPagination(ref paginaDe, ref paginaAte, function);

            var listRemover = new List<int>();

            for (int i = paginaDe; i < paginaAte; i++)
            {
                if (i == clientes.Count)
                    break;

                if (string.IsNullOrEmpty(clientes[i].Departamento)) 
                {
                    var c = GetClientData(clientes[i].Id_Cliente);

                    if (c != null)
                    {
                        clientes[i] = c;
                    }
                    else
                    {
                        clientes[i].Cpf = string.Empty;
                        clientes[i].Departamento = "Não Encontrado";
                        clientes[i].Cargo = "Masterdata(VTEX)";
                    }
                }
            }

            RestartDivPosition(this.Page, Page.GetType());

            if (function.Equals("NewFind"))
            {
                grvClientes.DataSource = clientes;
                grvClientes.DataBind();
            }
        }
        private void SetPagination(ref int paginaDe, ref int paginaAte, string function)
        {
            var paginaAtual = Convert.ToInt32(hdnPaginaAtual.Value);

            var qtdeCliente = clientes.Count();
            hdnQtdeCliente.Value = qtdeCliente.ToString();

            var qtdePagina = (qtdeCliente / 10);
            qtdePagina = (qtdeCliente % 10) == 0 ? qtdePagina : qtdePagina + 1;
            hdnQtdePaginas.Value = qtdePagina.ToString();

            paginaAtual = function.Equals("NewFind") ? 1 : Convert.ToInt32(function);

            /*switch (function)
            {
                case "First":
                    paginaAtual = 1;
                    break;
                case "Prev":
                    paginaAtual = (paginaAtual == 1) ? 1 : paginaAtual - 1;
                    break;
                case "Next":
                    paginaAtual = (paginaAtual == qtdePagina) ? qtdePagina : paginaAtual + 1;
                    break;
                case "Last":
                    paginaAtual = qtdePagina;
                    break;
                default:
                    paginaAtual = 1;
                    break;
            }
            */

            hdnPaginaAtual.Value = paginaAtual.ToString();

            paginaDe = ((paginaAtual - 1) * 10);
            paginaAte = (paginaAtual * 10);
        }
        private Guid GetCompanyId()
        {
            var id = new Guid();

            if (mselEmpresas.Value != "0")
            {
                var dal = new EmpresaDAL();

                var empresa = dal.GetById(Convert.ToInt32(mselEmpresas.Value));

                id = empresa.Id_Parceiro;
            }

            return id;
        }
        private Cliente GetClientData(Guid id)
        {
            var cs = new ClientServices();
            var cliente = cs.GetById(id);
            return cliente;
        }
        private bool HasOrder(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            var os = new OrderServices();

            var ret = os.GetByEmail(email);

            //ret.list = ret.list.Where(o => o.)

            if (ret.Pedidos.Count > 0)
                return true;

            return false;
        }
        private void ShowModalUpdateClient(Page page, Type type)
        {
            var msg = string.Format("javascript: ShowModalClient();");

            ScriptManager.RegisterStartupScript(page, type, "script", msg, true);
        }
        private void ShowModalDeleteClient(Page page, Type type)
        {
            var msg = string.Format("javascript: ShowModalDeleteClient();");

            ScriptManager.RegisterStartupScript(page, type, "script", msg, true);
        }
        private void RestartDivPosition(Page page, Type type)
        {
            var msg = string.Format("javascript: RestartDivPosition();");

            ScriptManager.RegisterStartupScript(page, type, "script", msg, true);
        }
    }
}