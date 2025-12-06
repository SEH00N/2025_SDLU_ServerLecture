using CSGameServer;

public class ClientPacketHandlerData : IPacketHandlerData
{
    public ChatUI ChatUI { get; set; }

    public ClientPacketHandlerData(ChatUI chatUI)
    {
        ChatUI = chatUI;
    }
}