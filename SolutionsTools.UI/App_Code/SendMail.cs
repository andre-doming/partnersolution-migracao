using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Web;

namespace SolutionsTools.UI
{
    public static class SendMail
    {
        public static void Send(string login, string email, string subject, string body) 
        {
            var enderecoSmtp = ConfigurationManager.AppSettings["EnderecoSmtp"];
            var portaSmtp = Convert.ToInt32(ConfigurationManager.AppSettings["PortaSmtp"]);
            var senhaSmtp = ConfigurationManager.AppSettings["SenhaSmtp"];
            var useSLL = ConfigurationManager.AppSettings["UseSLL"].Equals("true");
            var emailRemetente = ConfigurationManager.AppSettings["EmailRemetente"];

            try
            {
                var mail = new MailMessage();
                mail.To.Add(email);
                mail.From = new MailAddress(emailRemetente);
                mail.Subject = subject;
                mail.Body = body; 
                mail.IsBodyHtml = true;
                var smtp = new SmtpClient();
                smtp.Host = enderecoSmtp;
                smtp.Port = portaSmtp;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential(emailRemetente, senhaSmtp);
                smtp.EnableSsl = useSLL;

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}