﻿using Replicate.RPC;
using Replicate.Serialization;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Replicate {
    public class SocketListener : IDisposable {
        private CancellationTokenSource cancel = new CancellationTokenSource();
        public SocketListener(IRPCServer server, int port, IReplicateSerializer serializer)
            : this(server, null, port, serializer) { }
        public Task Task { get; private set; }
        private Socket socket;
        public int Port => (socket?.LocalEndPoint as IPEndPoint)?.Port ?? 0;
        public SocketListener(IRPCServer server, string host, int port, IReplicateSerializer serializer) {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(host == null ? IPAddress.Any : IPAddress.Parse(host), port));
            socket.Listen(16);
            Task = Task.Run(() => {
                while (true) {
                    try {
                        var client = socket.Accept();
                        client.Blocking = false;
                        new SocketChannel(client, serializer) { Server = server }.Start();
                    } catch { return; }
                }
            }, cancellationToken: cancel.Token);
        }

        public void Close() {
            socket.Close();
            cancel.Cancel();
        }

        void IDisposable.Dispose() => Close();
    }
}
