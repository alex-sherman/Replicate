using Replicate.MetaData;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Replicate.RPC {
    [ReplicateType]
    public struct RPCMethodInfo {
        public MethodKey Key;
        public TypeId Request;
        public TypeId Response;
    }
    [ReplicateType]
    public interface IReflectionService {
        Task<ModelDescription> Model();
        Task<IEnumerable<RPCMethodInfo>> Services();
    }
    public class ReflectionService : IReflectionService {
        IRPCServer Server;
        public ReflectionService(IRPCServer server) {
            Server = server;
        }
        public Task<ModelDescription> Model() => Task.FromResult(Server.Model.GetDescription());

        public Task<IEnumerable<RPCMethodInfo>> Services()
            => Task.FromResult(Server.Methods.Select(m => {
                var contract = Server.Contract(m);
                return new RPCMethodInfo() {
                    Key = m,
                    Request = Server.Model.GetId(contract.RequestType),
                    Response = Server.Model.GetId(contract.ResponseType),
                };
            }));
    }
}
