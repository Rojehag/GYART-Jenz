using UnityEngine;
using Unity.Netcode;

public class NetworkUI : MonoBehaviour
{
    [SerializeField]
    private GameObject networkCanvas;

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        networkCanvas.SetActive(false);
    }
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        networkCanvas.SetActive(false);
    }
}
