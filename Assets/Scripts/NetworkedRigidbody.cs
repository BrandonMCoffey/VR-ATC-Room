using UnityEngine;

public class NetworkedRigidbody : MonoBehaviour
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField, ReadOnly] public int _networkId = -1;
    [SerializeField, ReadOnly] private bool _isOwner;
    [SerializeField, ReadOnly] private bool _isPlaced;

    private void OnValidate()
    {
        if (!_rb) _rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (_isOwner && !_isPlaced)
        {
            NetworkManager.LocalRigidbodySendTransform(_networkId, transform);
        }
    }

    private void GrabObject()
    {
        // Take Ownership
        CreateRb();
        _isOwner = true;
        _isPlaced = false;
        _rb.isKinematic = false;
        _rb.useGravity = true;
    }

    private void PlaceObject()
    {
        NetworkManager.LocalRigidbodySendTransform(_networkId, transform);
        if (_rb) Destroy(_rb);
    }

    public void UpdateTransform(Vector3 pos, Quaternion rot)
    {
        // Remote Ownership
        _isOwner = false;
        if (_rb) Destroy(_rb);
        transform.SetParent(null);
        transform.SetPositionAndRotation(pos, rot);
    }

    private void CreateRb()
    {
        if (_rb) return;
        _rb = gameObject.AddComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
}
