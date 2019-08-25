using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Replicate.MetaData;
using Replicate.Serialization;
using Replicate.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplicateWebTest
{
    public class MockServiceProvider : IServiceProvider
    {
        class Env : IHostingEnvironment
        {
            public string EnvironmentName { get; set; } = "Development";
            public string ApplicationName { get; set; }
            public string WebRootPath { get; set; }
            public IFileProvider WebRootFileProvider { get; set; }
            public string ContentRootPath { get; set; }
            public IFileProvider ContentRootFileProvider { get; set; }
        }

        public class Configuration : IConfiguration
        {
            public Dictionary<string, string> Values = new Dictionary<string, string>();
            public string this[string key] { get => Values[key]; set => Values[key] = value; }

            public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();

            public IChangeToken GetReloadToken()
            {
                throw new NotImplementedException();
            }

            public IConfigurationSection GetSection(string key)
            {
                return null;
            }
        }
        class ScopeFactory : IServiceScopeFactory, IServiceScope
        {
            public ScopeFactory(IServiceProvider provider) => ServiceProvider = provider;
            public IServiceProvider ServiceProvider { get; set; }
            public IServiceScope CreateScope() => this;
            public void Dispose() { }
        }

        public JSONSerializer Serializer;
        public ConfigurationBuilder Config = new ConfigurationBuilder();
        Dictionary<Type, Func<object>> getters = new Dictionary<Type, Func<object>>();

        public MockServiceProvider()
        {
            var model = new ReplicationModel() { DictionaryAsObject = true };
            Serializer = new JSONSerializer(model);
        }

        public void Resolve(Type type, Func<object> getter)
        {
            getters[type] = getter;
        }

        public object GetService(Type serviceType)
        {
            if (getters.TryGetValue(serviceType, out var getter)) return getter();
            if (serviceType == typeof(IReplicateSerializer) || serviceType == typeof(JSONSerializer))
                return Serializer;
            if (serviceType == typeof(IConfiguration))
                return Config.Build();
            if (serviceType == typeof(WebRPCServer))
                return new WebRPCServer(Serializer.Model);
            if (serviceType == typeof(IServiceScopeFactory))
                return new ScopeFactory(this);
            return null;
        }
    }
}
