using SolutionsTools.DAL;
using SolutionsTools.Services;
using System;
using System.Web.UI;

namespace SolutionsTools.UI
{
    public partial class acClientes : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                VerifyUpdateControls();
            }
        }
        private void VerifyUpdateControls()
        {
            var allowUpd = false;

            allowUpd = UserDataAccess.IsAdmin;
            txtNome.ReadOnly = !allowUpd;
            txtSobrenome.ReadOnly = !allowUpd;
            txtCPF.ReadOnly = !allowUpd;
            txtEmail.ReadOnly = !allowUpd;
            txtSexo.ReadOnly = !allowUpd;
            txtDtNasc.ReadOnly = !allowUpd;

            allowUpd = UserDataAccess.IsAdmin ? true : UserDataAccess.HasFunction("funcClientesUpd");
            txtCargo.ReadOnly = !allowUpd;
            txtDepartamento.ReadOnly = !allowUpd;
            rbtAprovado1.Enabled = allowUpd;
            rbtAprovado2.Enabled = allowUpd;
            btnSave.Visible = allowUpd;
        }
        public void GetDataClient(Guid id, object sender)
        {
            try
            {
                var cs = new ClientServices();

                var cliente = cs.GetById(id);

                FillClientFields(cliente);

            }
            catch (Exception ex)
            {
                Tools.ShowMessage(this.Page, Page.GetType(), "Erro: " + ex.Message, Tools.MessageType.Error);
            }

        }
        private void FillClientFields(Cliente client)
        {
            hdnId.Value = client.Id_Cliente.ToString();
            txtNome.Text = client.Nome;
            txtSobrenome.Text = client.Sobrenome;
            txtCPF.Text = client.Cpf;
            txtEmail.Text = client.Email;
            txtSexo.Text = client.Sexo;
            txtDtNasc.Text = client.Dt_Nascimento != null ? Convert.ToDateTime(client.Dt_Nascimento).ToString("dd/MM/yyyy") : string.Empty;
            txtCargo.Text = client.Cargo;
            txtDepartamento.Text = client.Departamento;
            txtEmpresa.Text = client.Empresa;
            rbtAprovado1.Checked = client.Aprovado ? true : false;
            rbtAprovado2.Checked = client.Aprovado ? false : true;
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            var page = (BaseWebUi)Parent.Page;

            if (page._isRefresh)
                return;

            try
            {
                var id = hdnId.Value.Equals("0") ?  new Guid() : Guid.Parse(hdnId.Value); 

                var cs = new ClientServices();

                var cliente = cs.GetById(id);

                SaveClient(cliente);

                var msg = id.Equals(0) ? "inserido" : "atualizado";

                var cliPage = (Clientes)Parent.Page;

                Tools.ShowMessage(this.Page, Page.GetType(), string.Format("Cliente {0}!", msg), Tools.MessageType.Info);

                cliPage.FindClients("NewFind");

            }
            catch (Exception ex)
            {
                Tools.ShowMessage(this.Page, Page.GetType(), "Erro: " + ex.Message, Tools.MessageType.Error);
            }
        }

        private void SaveClient(Cliente client)
        {
            var dtNasc = new DateTime();

            if (UserDataAccess.IsAdmin)
            {
                client.Nome = txtNome.Text;
                client.Sobrenome = txtSobrenome.Text;
                client.Cpf = txtCPF.Text;
                client.Email = txtEmail.Text;
                client.Sexo = txtSexo.Text;

                if (DateTime.TryParse(txtDtNasc.Text, out dtNasc))
                    client.Dt_Nascimento = dtNasc.ToString("dd/MM/yyyy");
            }

            client.Cargo = txtCargo.Text;
            client.Departamento = txtDepartamento.Text;
            client.Aprovado = rbtAprovado1.Checked;

            var cs = new ClientServices();

            cs.UpdClient(client);

            var dal = new ClienteDAL();

            var md = new CryptographyMD5();

            var cpf_hash = string.IsNullOrEmpty(client.Cpf) ? null : md.ReturnMD5(client.Cpf);

            dal.UpdateByGuid(client, cpf_hash);

            Utils.Log(EntityType.CL, client.Id_Cliente, "U");
        }
    }
}