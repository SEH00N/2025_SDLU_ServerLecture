namespace CSGameServer
{
    public abstract class GameObject
    {
        public abstract string ID { get; }
        public bool Enabled = true;

        protected virtual void OnAwake() { }
        public void Awake()
        {
            OnAwake();
        }

        protected virtual void OnStart() { }
        public void Start()
        {
            OnStart();
        }

        protected virtual void OnDestroy() { }
        public void Destroy()
        {
            OnDestroy();
        }
    }
}