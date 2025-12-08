using CSGameServer;
using UnityEngine;

public class MyEntity : MonoBehaviour
{
    private Vector2 lastInput = Vector2.zero;

    private void Update()
    {
        Vector2 input = new Vector2();
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
        input.Normalize();

        if(lastInput.x != input.x || lastInput.y != input.y)
        {
            lastInput = input;
            C2S_MoveInputPacket moveInputPacket = new C2S_MoveInputPacket() { MoveInput = new VectorData(lastInput.x, lastInput.y) };
            NetworkManager.Instance.Send(moveInputPacket);
        }
    }
}