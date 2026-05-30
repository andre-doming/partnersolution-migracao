using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SolutionsTools.UI
{
    /// <summary>
    /// Summary description for FileUploadHandler
    /// </summary>
    public class FileUploadHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            try
            {
                if (context.Request.Files.Count > 0)
                {
                    HttpFileCollection files = context.Request.Files;
                    for (int i = 0; i < files.Count; i++)
                    {
                        HttpPostedFile file = files[i];

                        var ext = System.IO.Path.GetExtension(file.FileName);

                        var newFileName = DateTime.Now.Ticks.ToString() + ext;

                        var filePath = context.Server.MapPath("~/uploads/" + newFileName);

                        file.SaveAs(filePath);

                        context.Response.Write(newFileName);
                    }
                }
                else
                {
                    context.Response.Write("Error: No File(s) sent!");
                }
            }
            catch (Exception ex)
            {
                context.Response.Write("Error: " + ex.Message);
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