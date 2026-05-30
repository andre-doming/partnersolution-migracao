using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SolutionsTools.UI
{
    public static class Tools
    {
        public static void ShowMessage(Page page, Type type, string message, MessageType messageType, object sender = null)
        {
            var control = sender == null ? string.Empty : ((WebControl)sender).ClientID;

            var title = "Partner Solution";

            var msg = string.Format("javascript: ShowMessage('{0}','{1}','{2}',{3});", title, message, control, (int)messageType);

            ScriptManager.RegisterStartupScript(page, type, "script", msg, true);
        }
        public enum MessageType {
            Info = 0,
            Warning = 1,
            Error = 2
        }

    }
}