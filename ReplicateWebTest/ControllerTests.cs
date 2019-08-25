using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Replicate;
using Replicate.MetaData;
using Replicate.Web;
using System.Threading.Tasks;

namespace ReplicateWebTest
{
    public class CustomAuthAttribute : AuthRequiredAttribute
    {
        public override void ThrowIfUnverified()
        {
            throw new HTTPError("Unauthorized", 401);
        }
    }
    [ReplicateType]
    public interface IService
    {
        [CustomAuth]
        Task Auth();
    }
    public class Service : IService
    {
        public Task Auth()
        {
            return Task.FromResult(true);
        }
    }
    public class ControllerTests
    {
        [Test]
        public void UsesCustomAuth()
        {
            var services = new MockServiceProvider();
            var model = new ReplicationModel();
            model.LoadTypes(typeof(Service).Assembly);
            var server = new WebRPCServer(model);
            server.RegisterSingleton<IService>(new Service());
            services.Resolve(typeof(WebRPCServer), () => server);
            using (var controller = new ReplicateController(services))
            {
                var error = Assert.ThrowsAsync<HTTPError>(() => BaseTest.PostRaw<None>(controller, "auth", default));
                Assert.AreEqual(error.Status, 401);
            }
        }
    }
}