using Unity.Netcode;
using UnityEngine;

public class SimpleLogic : NetworkBehaviour
{
    public float rotationSpeed = 50f;
    private NetworkVariable<float> currentYRotation = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private float _serverAuthRotationY = 0f;

    public override void OnNetworkSpawn()
    {
        currentYRotation.OnValueChanged += OnRotationChangedClientRpc;
        transform.rotation = Quaternion.Euler(0, currentYRotation.Value, 0);
        if (IsServer)
        {
            _serverAuthRotationY = transform.eulerAngles.y;
            currentYRotation.Value = _serverAuthRotationY;
        }
    }

    void Update()
    {
        if (IsServer)
        {
            _serverAuthRotationY = (_serverAuthRotationY + rotationSpeed * Time.deltaTime) % 360f;
            currentYRotation.Value = _serverAuthRotationY;
        }
    }

    [ClientRpc]
    private void OnRotationChangedClientRpc(float previousValue, float newValue)
    {
        transform.rotation = Quaternion.Euler(0, newValue, 0);
    }

    public override void OnNetworkDespawn()
    {
        if (currentYRotation != null)
        {
            currentYRotation.OnValueChanged -= OnRotationChangedClientRpc;
        }
    }
}