using Replicate.MetaData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Replicate.RPC
{

    [ReplicateType]
    public interface IReflectionService
    {
        Task<ModelDescription> Model();
        Task<IEnumerable<MethodKey>> Services();
    }
    public class ReflectionService : IReflectionService
    {
        IRPCServer Server;
        public ReflectionService(IRPCServer server)
        {
            Server = server;
        }
        public Task<ModelDescription> Model() => Task.FromResult(Server.Model.GetDescription());

        public Task<IEnumerable<MethodKey>> Services() => Task.FromResult(Server.Methods);
    }
}
