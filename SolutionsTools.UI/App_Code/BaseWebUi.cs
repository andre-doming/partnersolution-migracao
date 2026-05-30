using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;

public class BaseWebUi : System.Web.UI.Page
{
    protected override void OnInit(EventArgs e)
    {
		ScriptManager src = ScriptManager.GetCurrent(Page);
		if (src != null)
			ScriptManager.RegisterClientScriptBlock(this, typeof(void), "TiraDivAguarde", "if(document.getElementById('divProcessando'))document.getElementById('divProcessando').style.display = 'none';", true);
		else
			ClientScript.RegisterStartupScript(typeof(Page), "TiraDivAguarde", "if(document.getElementById('divProcessando'))document.getElementById('divProcessando').style.display = 'none';", true);

        ClientScript.RegisterOnSubmitStatement(this.GetType(), "zerarfiltro", "if(document.getElementById('divProcessando') && document.getElementById('divProcessando').style.display!='none')return false;");

        ClientScript.RegisterOnSubmitStatement(this.GetType(), "Aguarde", "if (typeof(ValidatorOnSubmit) == 'function' && ValidatorOnSubmit() == false) return false; avisoAguarde();");

        base.OnInit(e);
    }

    public bool _refreshState;
    public bool _isRefresh;

    protected override void LoadViewState(object savedState)
    {
        object[] AllStates = (object[])savedState;
        base.LoadViewState(AllStates[0]);
        _refreshState = bool.Parse(AllStates[1].ToString());
        _isRefresh = _refreshState == bool.Parse(Session["__ISREFRESH"].ToString());
    }

    protected override object SaveViewState()
    {
        Session["__ISREFRESH"] = _refreshState;
        object[] AllStates = new object[2];
        AllStates[0] = base.SaveViewState();
        AllStates[1] = !(_refreshState);
        return AllStates;
    }
}
