using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace RenewalWebsite.Services
{
    // This class is used by the application to send Email and SMS
    // when you turn on two-factor authentication in ASP.NET Identity.
    // For more details see this link https://go.microsoft.com/fwlink/?LinkID=532713
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        private IOptions<EmailSettings> _emailSettings;
        private readonly IHostingEnvironment _hostingEnvironment;
        public AuthMessageSender(IOptions<EmailSettings> emailSettings, IHostingEnvironment hostingEnvironment)
        {
            this._emailSettings = emailSettings;
            this._hostingEnvironment = hostingEnvironment;
        }

        public Task SendEmailAsync(string email, string subject, string message, string FullName)
        {
            try
            {
                // Plug in your email service here to send an email.
                MailMessage mail = new MailMessage();
                mail.To.Add(email);
                mail.From = new MailAddress(_emailSettings.Value.FromEmail);
                mail.Subject = subject;
                mail.IsBodyHtml = true;
                string contentRootPath = _hostingEnvironment.ContentRootPath;                
                string Body = File.ReadAllText(contentRootPath + "\\MailTemplate\\password_reset.html");
                Body = Body.Replace("{{name}}", FullName);
                Body = Body.Replace("{{action_url}}", message);
                //"<!DOCTYPE html><html lang='en' class='no-js'><head></head><body>" + message + "</body></html>";
                mail.Body = Body;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = _emailSettings.Value.Host; //Or Your SMTP Server Address
                smtp.Port = _emailSettings.Value.Port;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential(_emailSettings.Value.FromEmail, _emailSettings.Value.Password);

                //Or your Smtp Email ID and Password
                smtp.EnableSsl = _emailSettings.Value.EnableSsl;
                smtp.Send(mail);
                return Task.FromResult(0);
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        public Task SendSmsAsync(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }
}
