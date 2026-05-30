using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Services;
using System.Web.UI;

namespace SolutionsTools.UI
{
    public partial class ImportarNew : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                Utils.LoadCompanies(mselEmpresas, false, true);
            }
        }

        protected void btnProcessar_Click(object sender, EventArgs e)
        {
            //var dir = Server.MapPath("~/Uploads/");

            //var myReader = new System.IO.StreamReader(dir + hdnFileName.Value, Encoding.GetEncoding("iso-8859-1"));

            //var output = myReader.ReadToEnd();

            
        }
    }
}