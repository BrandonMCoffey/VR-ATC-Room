using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RiptideRemoteAvatar : MonoBehaviour
{
    [SerializeField] private GameObject _remoteAvatarPrefab;
    
    [SerializeField, ReadOnly] private GameObject _avatar;

    public void BuildAvatar()
    {
        DestroyAvatar();
        if (_remoteAvatarPrefab && !_avatar)
        {
            _avatar = Instantiate(_remoteAvatarPrefab, transform);
        }
    }

    public void DestroyAvatar()
    {
        if (_avatar)
            Destroy(_avatar.gameObject);
    }

    public void MoveAvatarTransform(Vector3 pos, Quaternion rot)
    {
        if (!_avatar) return;
        _avatar.transform.SetPositionAndRotation(pos, rot);
    }

    public void MoveAvatarBones(string boneRotations)
    {
        if (!_avatar) return;
        var arr = boneRotations.Split("|");
        int i = 0;
        //foreach (var bone in _avatar.AvatarRig.Transforms)
        //{
        //    bone.eulerAngles = new Vector3(Convert(arr[i++]), Convert(arr[i++]), Convert(arr[i++]));
        //}
    }

    private static float Convert(string input) => float.Parse(input);
}
