using System;
using System.Collections.Generic;
using System.Linq;
using RiptideNetworking;
using RiptideNetworking.Utils;
using UnityEngine;

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
    updateAvatarTransform, // Received avatar transform every frame from clients. Sends moveAvatarTransform to all other clients
    updateAvatarBones, // Received avatar rig bone rotations every frame from clients. Sends moveAvatarBones to all other clients
    updateRigidbodyTransform, // Received rigidbody transform every frame from the simulated rigidbody. Sends to all other clients
}

public class NetworkManager : MonoBehaviour
{
    public static Dictionary<ushort, RiptideRemoteAvatar> RemoteAvatars { get; } = new Dictionary<ushort, RiptideRemoteAvatar>();
    public static Action OnConnected = delegate { };
    
    public Client Client { get; private set; }

    [SerializeField] private string _ip;
    [SerializeField] private ushort _port;
    
    [SerializeField] private RiptideRemoteAvatar _remoteAvatarPrefab;
    [SerializeField, ReadOnly] private List<NetworkedRigidbody> _networkedRigidbodies;

    #region Singleton

    private static NetworkManager _instance;
    public static NetworkManager Instance
    {
        get => _instance;
        private set
        {
            if (_instance == null)
                _instance = value;
            else if (_instance != value)
            {
                Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying object!");
                Destroy(value);
            }
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    #endregion

    #region Client Events

    private void Start()
    {
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        Client = new Client();
        Client.Connected += DidConnect;
        Client.ConnectionFailed += FailedToConnect;
        Client.ClientDisconnected += PlayerLeft;
        Client.Disconnected += DidDisconnect;
        
        Connect();
        SetNetworkRigidbodyIds();
    }

    private void FixedUpdate()
    {
        Client.Tick();
    }

    private void OnApplicationQuit()
    {
        Client.Disconnect();

        Client.Connected -= DidConnect;
        Client.ConnectionFailed -= FailedToConnect;
        Client.ClientDisconnected -= PlayerLeft;
        Client.Disconnected -= DidDisconnect;
    }

    private void Connect()
    {
        Client.Connect($"{_ip.Trim()}:{_port}");
    }

    private void DidConnect(object sender, EventArgs e)
    {
        OnConnected?.Invoke();
        Debug.Log($"Connected to Server: {_ip.Trim()}:{_port}", gameObject);
    }

    private void FailedToConnect(object sender, EventArgs e)
    {
        Debug.LogError("Failed to Connect to Server", gameObject);
    }

    private static void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        if (RemoteAvatars.TryGetValue(e.Id, out var player))
        {
            player.DestroyAvatar();
            Destroy(player.gameObject);
        }
    }

    private static void DidDisconnect(object sender, EventArgs e)
    {
        // Basically this is only called when the application closes, so all avatars will be destroyed anyways
        // foreach (var player in _remoteAvatars.Values)
        //     player.AvatarDisconnected();
    }
    
    #endregion

    private static void CreateRemoteAvatar(ushort id, string avatar)
    {
        Debug.Log($"Spawn Avatar ({avatar})");
        if (id == Instance.Client.Id)
            return;

        if (RemoteAvatars.ContainsKey(id))
        {
            RemoteAvatars[id].BuildAvatar();
            return;
        }

        var remoteAvatar = Instantiate(Instance._remoteAvatarPrefab, Vector3.zero, Quaternion.identity);
        remoteAvatar.BuildAvatar();
        RemoteAvatars.Add(id, remoteAvatar);
    }

    [Button]
    private void SetNetworkedRigidbodies()
    {
        _networkedRigidbodies = FindObjectsOfType<NetworkedRigidbody>().ToList();
        SetNetworkRigidbodyIds();
    }

    private void SetNetworkRigidbodyIds()
    {
        int id = 0;
        foreach (var rb in _networkedRigidbodies)
        {
            rb._networkId = id++;
        }
    }
    
    #region Messages
    
    // Avatar Spawning
    public static void LocalPlayerAvatarSpawned()
    {
        MessageHelper.SendStringMessage("", ClientToServerId.localAvatarSpawned, MessageSendMode.reliable);
    }
    [MessageHandler((ushort)ServerToClientId.spawnRemoteAvatar)]
    private static void SpawnRemotePlayer(Message message)
    {
        var fromClientId = message.GetUShort();
        var data = message.GetString();
        Debug.Log($"Receive Avatar Selection from User {fromClientId} (Spawn Avatar \"{data}\")");
        CreateRemoteAvatar(fromClientId, data);
    }

    // Avatar Transform
    public static void LocalPlayerSendAvatarTransform(Transform transform)
    {
        MessageHelper.SendTransformMessage(transform, ClientToServerId.updateAvatarTransform, MessageSendMode.unreliable);
    }
    [MessageHandler((ushort)ServerToClientId.moveAvatarTransform)]
    private static void MoveRemoteAvatarTransform(Message message)
    {
        var playerId = message.GetUShort();
        var pos = message.GetVector3();
        var rot = message.GetQuaternion();
        if (RemoteAvatars.TryGetValue(playerId, out var remotePlayer))
            remotePlayer.MoveAvatarTransform(pos, rot);
    }
    
    // Avatar Rig Bones
    public static void LocalPlayerSendAvatarBones(string boneRotations)
    {
        MessageHelper.SendStringMessage(boneRotations, ClientToServerId.updateAvatarBones, MessageSendMode.unreliable);
    }
    [MessageHandler((ushort)ServerToClientId.moveAvatarBones)]
    private static void MoveRemoteAvatarBones(Message message)
    {
        var playerId = message.GetUShort();
        var data = message.GetString();
        if (RemoteAvatars.TryGetValue(playerId, out var remotePlayer))
            remotePlayer.MoveAvatarBones(data);
    }

    public static void LocalRigidbodySendTransform(int objId, Transform transform)
    {
        MessageHelper.SendNetworkedRigidbodyMessage(objId, transform, ClientToServerId.updateRigidbodyTransform, MessageSendMode.unreliable);
    }
    [MessageHandler((ushort)ServerToClientId.moveRigidbodyTransform)]
    private static void MoveRemoteRigidbodyTransform(Message message)
    {
        var objId = message.GetInt();
        var pos = message.GetVector3();
        var rot = message.GetQuaternion();
        Instance._networkedRigidbodies[objId].UpdateTransform(pos, rot);
    }

    #endregion
}