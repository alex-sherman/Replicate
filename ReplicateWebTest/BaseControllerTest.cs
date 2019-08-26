﻿using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Replicate.MetaData;
using Replicate.Serialization;
using Replicate.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateWebTest
{
    public class BaseTest
    {
        protected JSONSerializer serializer;
        protected ReplicationModel model;
        protected MockServiceProvider services;
        [SetUp]
        public virtual void Setup()
        {
            services = new MockServiceProvider();
            model = services.Serializer.Model;
            serializer = services.Serializer;
            services.AddRouting();
            services.AddReplicate(serializer);
        }
        public RequestDelegate ReplicateHandler()
        {
            var builder = new ApplicationBuilder(services);
            builder.UseEndpointRouting();
            builder.UseErrorHandling(services.GetRequiredService<IReplicateSerializer>());
            builder.UseEndpoint();
            return builder.Build();
        }
        public static HttpContext MakeContext(string url, string body)
        {
            HttpContext httpContextMock = new DefaultHttpContext();
            httpContextMock.Request.Path = url;
            var bytes = Encoding.UTF8.GetBytes(body);
            (httpContextMock.Request.Body = new MemoryStream()).Write(bytes, 0, bytes.Length);
            httpContextMock.Request.Body.Position = 0;
            return httpContextMock;
        }
        public async Task<string> Post(RequestDelegate del, string url, string body)
        {
            var context = MakeContext(url, body);
            await del(context);
            context.Response.Body.Position = 0;
            return context.Response.Body.ReadAllString();
        }
        public async Task<T> Post<T, U>(RequestDelegate del, string url, U request)
        {
            return serializer.Deserialize<T>(await Post(del, url, serializer.Serialize(request).ReadAllString()));
        }
    }
}
