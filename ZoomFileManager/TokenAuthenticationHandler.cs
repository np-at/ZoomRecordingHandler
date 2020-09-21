﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Security.Claims;

namespace ZoomFileManager
{
    public partial class Startup
    {
        public class TokenAuthenticationHandler : AuthenticationHandler<TokenAuthenticationOptions>
        {
            public IServiceProvider ServiceProvider { get; set; }

            public TokenAuthenticationHandler(IOptionsMonitor<TokenAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IServiceProvider serviceProvider)
                : base(options, logger, encoder, clock)
            {
                ServiceProvider = serviceProvider;
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var headers = Request.Headers;
                var token = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(token))
                {
                    return Task.FromResult(AuthenticateResult.Fail("Token is null"));
                }

                bool isValidToken = false; // check token here

                if (!isValidToken)
                {
                    return Task.FromResult(AuthenticateResult.Fail($"Balancer not authorize token : for token={token}"));
                }

                var claims = new[] { new Claim("token", token) };
                var identity = new ClaimsIdentity(claims, nameof(TokenAuthenticationHandler));
                var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), this.Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
        }

    }
}