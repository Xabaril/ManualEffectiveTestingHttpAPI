using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace FunctionalTests.Seedwork
{
    public class MyTestsAuthenticationHandler
        : AuthenticationHandler<MyTestOptions>
    {
        public MyTestsAuthenticationHandler(IOptionsMonitor<MyTestOptions> options, 
            ILoggerFactory logger,
            UrlEncoder encoder, 
            ISystemClock clock) 
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name,"HttpAPITesting"),
                new Claim("Permission","Read")
            };

            var identity = new ClaimsIdentity(
               claims: claims,
               authenticationType: Scheme.Name,
               nameType: ClaimTypes.Name,
               roleType: ClaimTypes.Role);

            var ticket = new AuthenticationTicket(
                new ClaimsPrincipal(identity),
                new AuthenticationProperties(),
                Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public class MyTestOptions
        : AuthenticationSchemeOptions
    {
    }
}
