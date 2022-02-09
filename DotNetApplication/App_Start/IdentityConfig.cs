using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using DotNetApplication.Models;
using SendGrid;
using System.Net;
using System.Configuration;
using System.Diagnostics;
using System.Text;

namespace DotNetApplication
{
    public class EmailService : IIdentityMessageService
    {
        public async Task SendAsync(IdentityMessage message)
        {
            await configSendGridasync(message);
        }

        // Use NuGet to install SendGrid (Basic C# client lib) 
        private async Task configSendGridasync(IdentityMessage message)
        {
            var myMessage = new SendGridMessage();
            myMessage.AddTo(message.Destination);
            myMessage.From = new System.Net.Mail.MailAddress(
                                "przem1005@wp.pl", "DotNetApp.");
            myMessage.Subject = message.Subject;
            myMessage.Text = message.Body;
            myMessage.Html = message.Body;

            var credentials = new NetworkCredential(
                       ConfigurationManager.AppSettings["mailAccount"],
                       ConfigurationManager.AppSettings["mailPassword"]
                       );

            // Create a Web transport for sending email.
            var transportWeb = new SendGrid.Web("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");
            try
            {
                await transportWeb.DeliverAsync(myMessage);
            }
            catch (Exceptions.InvalidApiRequestException ex)
            {
                var details = new StringBuilder();

                details.Append("ResponseStatusCode: " + ex.ResponseStatusCode + ".   ");
                for (int i = 0; i < ex.Errors.Count(); i++)
                {
                    details.Append(" -- Error #" + i.ToString() + " : " + ex.Errors[i]);
                }

                throw new ApplicationException(details.ToString(), ex);
            }
        }
    }

    public class SmsService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            // Dołącz tutaj usługę wiadomości SMS, aby wysłać wiadomość SMS.
            return Task.FromResult(0);
        }
    }

    // Skonfiguruj menedżera użytkowników aplikacji używanego w tej aplikacji. Interfejs UserManager jest zdefiniowany w produkcie ASP.NET Identity i jest używany przez aplikację.
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store)
            : base(store)
        {
        }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context) 
        {
            var manager = new ApplicationUserManager(new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>()));
            // Konfiguruj logikę weryfikacji nazw użytkowników
            manager.UserValidator = new UserValidator<ApplicationUser>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Konfiguruj logikę weryfikacji haseł
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
            };

            // Konfiguruj ustawienia domyślne blokady użytkownika
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            // Zarejestruj dostawców uwierzytelniania dwuetapowego. W przypadku tej aplikacji kod weryfikujący użytkownika jest uzyskiwany przez telefon i pocztą e-mail
            // Możesz zapisać własnego dostawcę i dołączyć go tutaj.
            manager.RegisterTwoFactorProvider("Kod — telefon", new PhoneNumberTokenProvider<ApplicationUser>
            {
                MessageFormat = "Twój kod zabezpieczający: {0}"
            });
            manager.RegisterTwoFactorProvider("Kod — e-mail", new EmailTokenProvider<ApplicationUser>
            {
                Subject = "Kod zabezpieczeń",
                BodyFormat = "Twój kod zabezpieczający: {0}"
            });
            manager.EmailService = new EmailService();
            manager.SmsService = new SmsService();
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider = 
                    new DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider.Create("ASP.NET Identity"));
            }
            return manager;
        }
    }

    // Skonfiguruj menedżera logowania aplikacji używanego w tej aplikacji.
    public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
    {
        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
        }

        public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
        {
            return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
        }
    }
}
