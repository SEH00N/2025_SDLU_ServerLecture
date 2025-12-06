using CSGameServer;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance = null;
    public static GameManager Instance => instance;

    [SerializeField] 
    private ChatUI chatUI = null;

    private void Awake()
    {
        if (instance != null)
        {
            DestroyImmediate(instance.gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PacketManager.On<MessagePacket>(i => new MessagePacketHandler(i));
        PacketManager.Initialize(new ClientPacketHandlerData(chatUI));

        GetComponent<NetworkManager>().Initialize();

        NetworkManager.Instance.Connect("127.0.0.1", 9696);
    }   
}