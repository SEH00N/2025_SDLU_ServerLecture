using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace CSGameServer
{
    public class World
    {
        private const double SECONDS_IN_MILLISECONDS = 1 / 1000d;

        private Dictionary<string, Entity> entityList = null;
        private Dictionary<string, GameSystem> systemList = null;

        private Dictionary<string, Entity> entityAddQueue = null;
        private Dictionary<string, GameSystem> systemAddQueue = null;

        private Dictionary<string, Entity> entityRemoveQueue = null;
        private Dictionary<string, GameSystem> systemRemoveQueue = null;

        private int tickRate = 30;

        public World(int tickRate)
        {
            this.tickRate = tickRate;

            entityList = new Dictionary<string, Entity>();
            systemList = new Dictionary<string, GameSystem>();

            entityAddQueue = new Dictionary<string, Entity>();
            systemAddQueue = new Dictionary<string, GameSystem>();

            entityRemoveQueue = new Dictionary<string, Entity>();
            systemRemoveQueue = new Dictionary<string, GameSystem>();
        }

        public void StartUpdateLoop()
        {
            Thread thread = new Thread(UpdateLoop) { IsBackground = true };
            thread.Start();
        }
        
        private void UpdateLoop()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            double tick = 1000d / tickRate;
            double nextTick = stopwatch.ElapsedMilliseconds + tick;

            while (true)
            {
                double driftTick = stopwatch.ElapsedMilliseconds - nextTick;
                if (driftTick >= 0)
                {
                    int steps = (int)Math.Floor(driftTick / tick) + 1;
                    double totalDeltaTime = driftTick + tick;
                    float stepDeltaTime = (float)(totalDeltaTime / steps * SECONDS_IN_MILLISECONDS);

                    for (int i = 0; i < steps; ++i)
                        Update(stepDeltaTime);

                    nextTick += steps * tick;
                }

                int delay = (int)Math.Max(0, nextTick - stopwatch.ElapsedMilliseconds);
                Thread.Sleep(delay);
            } 
        }

        private void Update(float deltaTime)
        {
            OnPreUpdate(deltaTime);

            // system pre-update
            foreach (GameSystem system in systemList.Values)
                PublishEvent(system, true, "PreUpdate", system => system.PreUpdate(deltaTime));

            // entity updates
            foreach(Entity entity in entityList.Values)
                PublishEvent(entity, true, "PreUpdate", entity => entity.PreUpdate(deltaTime));
            foreach (Entity entity in entityList.Values)
                PublishEvent(entity, true, "Update", entity => entity.Update(deltaTime));
            foreach (Entity entity in entityList.Values)
                PublishEvent(entity, true, "PostUpdate", entity => entity.PostUpdate(deltaTime));

            // system post-update
            foreach (GameSystem system in systemList.Values)
                PublishEvent(system, true, "PostUpdate", system => system.PostUpdate(deltaTime));

            // entity late-update
            foreach (Entity entity in entityList.Values)
                PublishEvent(entity, true, "LateUpdate", entity => entity.LateUpdate(deltaTime));

            // system late-update
            foreach (GameSystem system in systemList.Values)
                PublishEvent(system, true, "LateUpdate", system => system.LateUpdate(deltaTime));

            OnPostUpdate(deltaTime);
        }

        protected virtual void OnPreUpdate(float deltaTime)
        {
            Dictionary<string, Entity> entityAddQueue = this.entityAddQueue;
            if (this.entityAddQueue.Count > 0)
            {
                this.entityAddQueue = new Dictionary<string, Entity>();
                foreach (Entity entity in entityAddQueue.Values)
                {
                    entityList[entity.ID] = entity;
                    PublishEvent(entity, false, "Awake", entity => entity.Awake());
                }
            }

            Dictionary<string, GameSystem> systemAddQueue = this.systemAddQueue;
            if (this.systemAddQueue.Count > 0)
            {
                this.systemAddQueue = new Dictionary<string, GameSystem>();
                foreach (GameSystem system in systemAddQueue.Values)
                {
                    systemList[system.ID] = system;
                    PublishEvent(system, false, "Awake", system => system.Awake());
                }
            }

            foreach (Entity entity in entityAddQueue.Values)
                PublishEvent(entity, false, "Start", entity => entity.Start());

            foreach (GameSystem system in systemAddQueue.Values)
                PublishEvent(system, false, "Start", system => system.Start());
        }

        protected virtual void OnPostUpdate(float deltaTime)
        {
            Dictionary<string, Entity> entityRemoveQueue = this.entityRemoveQueue;
            if (entityRemoveQueue.Count > 0)
            {
                this.entityRemoveQueue = new Dictionary<string, Entity>();

                foreach (string entityID in entityRemoveQueue.Keys)
                {
                    Entity entity = entityRemoveQueue[entityID];
                    entityList.Remove(entityID);
                    PublishEvent(entity, false, "Destroy", entity => entity.Destroy());
                }
            }

            Dictionary<string, GameSystem> systemRemoveQueue = this.systemRemoveQueue;
            if (systemRemoveQueue.Count > 0)
            {
                this.systemRemoveQueue = new Dictionary<string, GameSystem>();

                foreach (string systemID in systemRemoveQueue.Keys)
                {
                    GameSystem system = systemRemoveQueue[systemID];
                    systemList.Remove(systemID);
                    PublishEvent(system, false, "Destroy", system => system.Destroy());
                }
            }
        }

        public IEnumerable<Entity> GetAllEntities() => entityList.Values;
        public bool TryGetEntity(string entityID, out Entity entity)
        {
            entity = null;
            if (string.IsNullOrEmpty(entityID))
                return false;

            return entityList.TryGetValue(entityID, out entity);
        }

        public void AddEntity(Entity entity)
        {
            if (string.IsNullOrEmpty(entity.ID))
                return;

            if (entityAddQueue.ContainsKey(entity.ID))
                return;

            entityAddQueue[entity.ID] = entity;
        }

        public void RemoveEntity(Entity entity)
        {
            if (string.IsNullOrEmpty(entity.ID))
                return;

            if (entityRemoveQueue.ContainsKey(entity.ID))
                return;

            entityRemoveQueue[entity.ID] = entity;
        }

        public bool TryGetSystem<TSystem>(out TSystem system) where TSystem : GameSystem
        {
            systemList.TryGetValue(typeof(TSystem).Name, out GameSystem result);
            system = result as TSystem;
            return system != null;
        }

        public void AddSystem(GameSystem system)
        {
            if (string.IsNullOrEmpty(system.ID))
                return;

            if (systemAddQueue.ContainsKey(system.ID))
                return;

            systemAddQueue[system.ID] = system;
        }

        public void RemoveSystem(GameSystem system)
        {
            if (string.IsNullOrEmpty(system.ID))
                return;

            if (systemRemoveQueue.ContainsKey(system.ID))
                return;

            systemRemoveQueue[system.ID] = system;
        }

        private void PublishEvent<T>(T gameObject, bool shouldEnabled, string eventInfo, Action<T> callback) where T : GameObject
        {
            try
            {
                if (gameObject == null)
                    return;

                if (shouldEnabled && gameObject.Enabled == false)
                    return;

                callback(gameObject);
            }
            catch(Exception err)
            {
                Console.WriteLine($"Error occurred while publishing event. {gameObject.GetType()}({gameObject.ID})::{eventInfo}.\n{err}");
            }
        }
    }
}
