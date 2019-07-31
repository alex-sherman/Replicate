Replicate
=========

A simple abstraction for network programming in C#. Replicate provides a simple yet powerful RPC framework that operates
on top of a simple, customizable network abstraction. This is what an echo server/client looks like:

```c#

[ReplicateType]
public interface IEchoService
{
    Task<string> Echo(string message);
}
public class EchoService : IEchoService
{
    public async Task<string> Echo(string message)
    {
        await Task.Delay(100); // Do some work
        return message + " DONE";
    }
}
public async Task EchoExample()
{
    // Server side
    var server = new RPCServer();
    server.RegisterSingleton<IEchoService>(new EchoService());
    SocketChannel.Listen(server, 55555, new BinarySerializer());
    // Client side
    var clientChannel = SocketChannel.Connect("127.0.0.1", 55555, new BinarySerializer());
    var echoService = clientChannel.CreateProxy<IEchoService>();
    Assert.AreEqual("Hello! DONE", await echoService.Echo("Hello!"));
}
```

## Everything is code

Replicate translates networking complexity into programming concepts. Instead of writing/implementing APIs, you use
interfaces. Instead of send/receiving messages, or making requests, you call functions.

When writing a server application, the first step is to create an interface like `IEchoService` above. Then the server
needs an implementation of this interface, in this case `EchoService`. Finally create an `RPCServer`, register the
service, and begin listening.

The client side needs only to connect to the same host/port and create a proxy for the service. All of the methods
in this proxy are magically implemented with client stubs that call out to the network and wait for a response.