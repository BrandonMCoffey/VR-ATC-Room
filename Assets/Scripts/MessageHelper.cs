using UnityEngine;
using RiptideNetworking;

public static class MessageHelper
{
    // Vector2
    public static Message Add(this Message message, Vector2 value) => AddVector2(message, value);
    public static Message AddVector2(this Message message, Vector2 value) => message.AddFloat(value.x).AddFloat(value.y);
    public static Vector2 GetVector2(this Message message) => new Vector2(message.GetFloat(), message.GetFloat());
    
    // Vector3
    public static Message Add(this Message message, Vector3 value) => AddVector3(message, value);
    public static Message AddVector3(this Message message, Vector3 value) => message.AddFloat(value.x).AddFloat(value.y).AddFloat(value.z);
    public static Vector3 GetVector3(this Message message) => new Vector3(message.GetFloat(), message.GetFloat(), message.GetFloat());
    
    // Quaternion
    public static Message Add(this Message message, Quaternion value) => AddQuaternion(message, value);
    public static Message AddQuaternion(this Message message, Quaternion value) => message.AddFloat(value.x).AddFloat(value.y).AddFloat(value.z).AddFloat(value.w);
    public static Quaternion GetQuaternion(this Message message) => new Quaternion(message.GetFloat(), message.GetFloat(), message.GetFloat(), message.GetFloat());
    

    public static void SendStringMessage(string data, ClientToServerId messageType, MessageSendMode sendMode)
    {
        var message = Message.Create(sendMode, messageType);
        message.Add(data);
        NetworkManager.Instance.Client.Send(message);
    }
    
    public static void SendTransformMessage(Transform data, ClientToServerId messageType, MessageSendMode sendMode)
    {
        var message = Message.Create(sendMode, messageType);
        message.Add(data.position);
        message.Add(data.rotation);
        //message.AddVector3(data.localScale);
        NetworkManager.Instance.Client.Send(message);
    }
    
    public static void SendNetworkedRigidbodyMessage(int id, Transform data, ClientToServerId messageType, MessageSendMode sendMode)
    {
        var message = Message.Create(sendMode, messageType);
        message.Add(id);
        message.Add(data.position);
        message.Add(data.rotation);
        NetworkManager.Instance.Client.Send(message);
    }
}