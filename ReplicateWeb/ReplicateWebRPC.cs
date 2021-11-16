using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Replicate.Web
{
    public class ReplicateWebRPC
    {
        [ReplicateIgnore]
        public readonly IServiceProvider Services;
        [ReplicateIgnore]
        public HttpContext HttpContext => contextAccessor.HttpContext;
        private readonly IHttpContextAccessor contextAccessor;

        public ReplicateWebRPC(IServiceProvider services)
        {
            Services = services;
            // If this fails, add `services.AddHttpContextAccessor();` in startup
            contextAccessor = Services.GetRequiredService<IHttpContextAccessor>();
        }
    }
}
