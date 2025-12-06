using System;

namespace CSGameServer
{
    public abstract class Entity : GameObject
    {
        private string id = string.Empty;
        public override string ID => id;

        private VectorData position = null;
        public VectorData Position => position;

        public Entity()
        {
            id = Guid.NewGuid().ToString();
            position = new VectorData();
        }

        protected virtual void OnPreUpdate(float deltaTime) { }
        public void PreUpdate(float deltaTime)
        {
            OnPreUpdate(deltaTime);
        }

        protected virtual void OnUpdate(float deltaTime) { }
        public void Update(float deltaTime)
        {
            OnUpdate(deltaTime);
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