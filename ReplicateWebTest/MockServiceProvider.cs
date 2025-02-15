using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Replicate;
using Replicate.MetaData;
using Replicate.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReplicateWebTest {
    public class MockServiceProvider : List<ServiceDescriptor>, IServiceProvider, IServiceCollection {
        class Env : IHostingEnvironment {
            public string EnvironmentName { get; set; } = "Development";
            public string ApplicationName { get; set; }
            public string WebRootPath { get; set; }
            public IFileProvider WebRootFileProvider { get; set; }
            public string ContentRootPath { get; set; }
            public IFileProvider ContentRootFileProvider { get; set; }
        }

        public class Configuration : IConfiguration {
            public Dictionary<string, string> Values = new Dictionary<string, string>();
            public string this[string key] { get => Values[key]; set => Values[key] = value; }

            public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();

            public IChangeToken GetReloadToken() {
                throw new NotImplementedException();
            }

            public IConfigurationSection GetSection(string key) {
                return null;
            }
        }
        class ScopeFactory : IServiceScopeFactory, IServiceScope {
            public ScopeFactory(IServiceProvider provider) => ServiceProvider = provider;
            public IServiceProvider ServiceProvider { get; set; }
            public IServiceScope CreateScope() => this;
            public void Dispose() { }
        }

        public JSONSerializer Serializer;
        public ConfigurationBuilder Config = new ConfigurationBuilder();
        Dictionary<Type, Func<object>> getters = new Dictionary<Type, Func<object>>();

        public MockServiceProvider() {
            var model = new ReplicationModel(false) { DictionaryAsObject = true };
            Serializer = new JSONSerializer(model);
            Add(new ServiceDescriptor(typeof(IReplicateSerializer), Serializer));
            Add(new ServiceDescriptor(typeof(IConfiguration), Config.Build()));
            Add(new ServiceDescriptor(typeof(IServiceScopeFactory), new ScopeFactory(this)));
        }

        public void Resolve(Type type, Func<object> getter) {
            getters[type] = getter;
        }

        public object GetService(Type serviceType) {
            if (getters.TryGetValue(serviceType, out var getter)) return getter();
            if (serviceType.IsSameGeneric(typeof(ILogger<>)))
                return Activator.CreateInstance(typeof(Logger<>).MakeGenericType(serviceType.GetGenericArguments()[0]));
            var descriptor = this.FirstOrDefault(s => s.ServiceType == serviceType);
            if (descriptor == null) return null;
            if (descriptor.ImplementationInstance != null)
                return descriptor.ImplementationInstance;
            if (descriptor.ImplementationType != null)
                return ActivatorUtilities.CreateInstance(this, descriptor.ImplementationType);
            return descriptor.ImplementationFactory(this);
        }
    }
}
