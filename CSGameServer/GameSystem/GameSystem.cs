namespace CSGameServer
{
    public abstract class GameSystem : GameObject
    {
        public sealed override string ID => GetType().Name;

        public GameSystem()
        {

        }

        protected virtual void OnPreUpdate(float deltaTime) { }
        public void PreUpdate(float deltaTime)
        {
            OnPreUpdate(deltaTime);
        }

        protected virtual void OnPostUpdate(float deltaTime) { }
        public void PostUpdate(float deltaTime)
        {
            OnPostUpdate(deltaTime);
        }

        protected virtual void OnLateUpdate(float deltaTime) { }
        public void LateUpdate(float deltaTime)
        {
            OnLateUpdate(deltaTime);
        }
    }
}