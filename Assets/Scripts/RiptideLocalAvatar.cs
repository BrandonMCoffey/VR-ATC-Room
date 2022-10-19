using System.Text;
using UnityEngine;

// TODO: Follow https://www.ultimatexr.io/guides/scripting-how-do-i#networking

public class RiptideLocalAvatar : MonoBehaviour
{
    [SerializeField] private GameObject _localAvatarPrefab;
    
    [SerializeField, ReadOnly] private GameObject _avatar;
    [SerializeField, ReadOnly] private Transform _dummyForward;
    [SerializeField, ReadOnly] private bool _active;


    private Transform DummyForward {
        get
        {
            if (_dummyForward) return _dummyForward;
            //_dummyForward = _avatar.transform.Find("Dummy Forward");
            return _dummyForward;
        }
    }

    #region Unity Functions

    private void Awake()
    {
        NetworkManager.OnConnected += SendAvatarConnection;
    }

    private void Start()
    {
        ReloadAvatar();
    }

    private void FixedUpdate()
    {
        SendInput();
    }

    #endregion

    #region Loading and Unloading Avatars

    private void ReloadAvatar()
    {
        DestroyAvatar();
        BuildAvatar();
    }

    private void DestroyAvatar()
    {
        if (_avatar)
            Destroy(_avatar.gameObject);
    }

    private void BuildAvatar()
    {
        if (_localAvatarPrefab && !_avatar)
        {
            _avatar = Instantiate(_localAvatarPrefab, transform);
        }
    }
    
    #endregion
    
    private void SendAvatarConnection()
    { 
        NetworkManager.LocalPlayerAvatarSpawned();
        NetworkManager.OnConnected -= SendAvatarConnection;
    }

    private void SendInput()
    {
        SendAvatarPosition();
        SendAvatarBones();
    }

    private void SendAvatarPosition()
    {
        NetworkManager.LocalPlayerSendAvatarTransform(DummyForward);
    }

    private void SendAvatarBones()
    {
        var stringBuilder = new StringBuilder();
        //foreach (var bones in _avatar.AvatarRig.Transforms)
        //{
        //    var eulerAngles = bones.eulerAngles;
        //    stringBuilder.Append(eulerAngles.x.ToString("F2"));
        //    stringBuilder.Append("|");
        //    stringBuilder.Append(eulerAngles.y.ToString("F2"));
        //    stringBuilder.Append("|");
        //    stringBuilder.Append(eulerAngles.z.ToString("F2"));
        //    stringBuilder.Append("|");
        //}
        NetworkManager.LocalPlayerSendAvatarBones(stringBuilder.ToString());
    }
}
