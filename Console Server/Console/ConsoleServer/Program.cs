using Riptide;
using Riptide.Utils;
using System;
using System.Collections.Generic;
using System.Threading;

// Messages sent from the server (Remote Connections) to the client (Local Connection)
public enum ServerToClientId : ushort
{
    spawnRemoteAvatar = 1, // Tells all clients to instantiate the client's selected avatar (See selectAvatar)
    moveAvatarTransform, // Tells all clients to move specific remote avatar transforms
    moveAvatarBones, // Tells all clients to move specific remote avatar rig bone rotations
    moveRigidbodyTransform, // Tells all clients to move specific remote rigidbody transforms
}

// Messages sent from the client (Local Connection) to the server (Remote Connections)
public enum ClientToServerId : ushort
{
    localAvatarSpawned = 1, // Received selected avatar from client. Sends spawnAvatar to all other clients
    updateAvatarTransform, // Received avatar position every frame from clients. Sends moveAvatarTransform to all other clients
    updateAvatarBones, // Received avatar rig bone rotations every frame from clients. Sends moveAvatarBones to all other clients
    updateRigidbodyTransform, // Received rigidbody transform every frame from the simulated rigidbody. Sends to all other clients
}

internal class Program
{
    private static Server server;
    private static bool isRunning;

    private const ushort Timeout = 10000; // 10s
    private const ushort Port = 7777;

    private static List<ushort> connectedClients;

    #region Main Networking Loop

    private static void Main()
    {
        Console.Title = "Server";

        RiptideLogger.Initialize(Console.WriteLine, true);
        isRunning = true;

        new Thread(Loop).Start();

        Console.WriteLine("Press enter to stop the server at any time.");
        Console.ReadLine();

        isRunning = false;

        Console.ReadLine();
    }

    private static void Loop()
    {
        server = new Server
        {
            TimeoutTime = Timeout
        };
        server.Start(Port, 10);

        connectedClients = new List<ushort>();
        server.ClientConnected += NewPlayerConnected;
        server.ClientDisconnected += PlayerLeft;

        while (isRunning)
        {
            server.Tick();
            Thread.Sleep(10);
        }

        server.ClientConnected -= NewPlayerConnected;
        server.ClientDisconnected -= PlayerLeft;

        server.Stop();
    }

    private static void NewPlayerConnected(object sender, ServerClientConnectedEventArgs e)
    {
        var clientId = e.Client.Id;
        connectedClients.Add(clientId);
        SendStringMessage(clientId, "Cyborg", ServerToClientId.spawnRemoteAvatar, MessageSendMode.reliable);
        Console.WriteLine($"Client connected: ({clientId})");
    }

    private static void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        connectedClients.Remove(e.Id);
        Console.WriteLine($"Client disconnected ({e.Id})");
    }

    #endregion

    // Avatar Spawning
    [MessageHandler((ushort)ClientToServerId.localAvatarSpawned)]
    private static void HandleAvatarSpawning(ushort fromClientId, Message message)
    {
        string data = message.GetString();
        Console.WriteLine($"User ({fromClientId}) Selected Avatar ({data})");
        SendStringMessage(fromClientId, data, ServerToClientId.spawnRemoteAvatar, MessageSendMode.reliable);
    }

    // Avatar Transform
    [MessageHandler((ushort)ClientToServerId.updateAvatarTransform)]
    private static void HandleUpdateAvatarTransform(ushort fromClientId, Message message)
    {
        SendTransformMessage(fromClientId, message.GetFloats(7), ServerToClientId.moveAvatarTransform, MessageSendMode.unreliable);
    }

    // Avatar Rig Bones
    [MessageHandler((ushort)ClientToServerId.updateAvatarBones)]
    private static void HandleUpdateAvatarRigBones(ushort fromClientId, Message message)
    {
        SendStringMessage(fromClientId, message.GetString(), ServerToClientId.moveAvatarBones, MessageSendMode.unreliable);
    }

    // Rigidbody Transform
    [MessageHandler((ushort)ClientToServerId.updateRigidbodyTransform)]
    private static void HandleUpdateRigidbodyTransform(ushort fromClientId, Message message)
    {
        SendRigidbodyMessage(fromClientId, message.GetInt(), message.GetFloats(7), ServerToClientId.moveRigidbodyTransform, MessageSendMode.unreliable);
    }

    // Helper Function
    private static void SendStringMessage(ushort fromClientId, string data, ServerToClientId messageType, MessageSendMode sendMode)
    {
        Message message = Message.Create(sendMode, messageType);
        message.AddUShort(fromClientId);
        message.AddString(data);
        SendMessage(message, fromClientId);
    }

    private static void SendTransformMessage(ushort fromClientId, float[] data, ServerToClientId messageType, MessageSendMode sendMode)
    {
        Message message = Message.Create(sendMode, messageType);
        message.AddUShort(fromClientId);
        message.AddFloats(data, false);
        SendMessage(message, fromClientId);
    }

    private static void SendRigidbodyMessage(ushort fromClientId, int objId, float[] data, ServerToClientId messageType, MessageSendMode sendMode)
    {
        Message message = Message.Create(sendMode, messageType);
        message.AddInt(objId);
        message.AddFloats(data, false);
        SendMessage(message, fromClientId);
    }

    private static void SendMessage(Message message, ushort fromClientId)
    {
        foreach (var client in connectedClients)
        {
            if (client == fromClientId)
                continue;
            server.Send(message, client);
        }
    }
}
