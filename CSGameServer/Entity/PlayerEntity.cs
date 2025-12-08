namespace CSGameServer
{
    public class PlayerEntity : Entity
    {
        private VectorData inputDirection = null;
        private float moveSpeed = 5f;

        public PlayerEntity(ClientSessionHandle clientSessionHandle)
        {
            inputDirection = new VectorData();
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            Position.x += inputDirection.x * moveSpeed * deltaTime;
            Position.y += inputDirection.y * moveSpeed * deltaTime;
        }
        
        public void SetInput(VectorData input)
        {
            inputDirection = input;
        }
    }
}