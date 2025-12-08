using System;
using System.Collections.Generic;
using CSGameServer;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance = null;
    public static GameManager Instance => instance;

    [SerializeField]
    private GameObject entityPrefab = null;
    public GameObject EntityPrefab => entityPrefab;

    private Dictionary<string, GameObject> clientEntities = null;
    
    private void Awake()
    {
        if (instance != null)
        {
            DestroyImmediate(instance.gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        clientEntities = new Dictionary<string, GameObject>();
    }

    private void Start()
    {
        PacketManager.On<MessagePacket>(i => new MessagePacketHandler(i));
        PacketManager.On<S2C_EnterWorldResponsePacket>(i => new S2C_EnterWorldResponsePacketHandler(i));
        PacketManager.On<S2C_EntityPositionUpdatePacket>(i => new S2C_EntityPositionUpdatePacketHandler(i));
        PacketManager.On<S2C_InformWorldEnterPacket>(i => new S2C_InformWorldEnterPacketHandler(i));
        PacketManager.On<S2C_InformWorldExitPacket>(i => new S2C_InformWorldExitPacketHandler(i));
        PacketManager.Initialize(new ClientPacketHandlerData());

        GetComponent<NetworkManager>().Initialize();

        NetworkManager.Instance.OnSessionConnectedEvent += HandleSessionConntected;
        NetworkManager.Instance.Connect("127.0.0.1", 9696);
    }

    private async void HandleSessionConntected()
    {
        await System.Threading.Tasks.Task.Delay(1000);

        C2S_EnterWorldRequestPacket enterWorldRequestPacket = new C2S_EnterWorldRequestPacket();
        NetworkManager.Instance.Send(enterWorldRequestPacket);
    }

    public void AddEntity(string clientID, GameObject entity)
    {
        if (string.IsNullOrEmpty(clientID))
            return;

        clientEntities[clientID] = entity;
    }

    public void RemoveEntity(string clientID)
    {
        if (string.IsNullOrEmpty(clientID))
            return;

        clientEntities.Remove(clientID);
    }

    public bool TryGetEntity(string clientID, out GameObject entity)
    {
        entity = null;
        if (string.IsNullOrEmpty(clientID))
            return false;

        return clientEntities.TryGetValue(clientID, out entity);
    }
}