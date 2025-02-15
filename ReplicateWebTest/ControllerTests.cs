using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using Replicate;
using Replicate.Web;
using System.Threading.Tasks;

namespace ReplicateWebTest {
    public class CustomAuthAttribute : RPCMiddlewareAttribute {
        public override Task Run(HttpContext context) {
            throw new HTTPError("Unauthorized", 401);
        }
    }
    [ReplicateType]
    public interface IService {
        [CustomAuth]
        Task Auth();
    }
    public class Service : IService {
        public Task Auth() {
            return Task.FromResult(true);
        }
    }
    public class ControllerTests : BaseTest {
        //[Test]
        public async Task UsesCustomAuth() {
            model.Add(typeof(IService));
            model.Add(typeof(Service));
            var error = await Post<None, ErrorData>(ReplicateHandler(), "auth", default);
        }
    }
}