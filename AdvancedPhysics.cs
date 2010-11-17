using System;
using System.Collections.Generic;
using System.Text;

namespace uBuilder
{
    public class AdvancedPhysics
    {
        public World world;
        private Queue<AdvancedPhysicsTile> updateQueue;
        private object queueLock = new object();

        public AdvancedPhysics(World _world)
        {
            this.world = _world;
            this.updateQueue = new Queue<AdvancedPhysicsTile>();
        }

        public void Update()
        {
            if (updateQueue.Count == 0) { return; }
            lock(queueLock)
            {
                int n = updateQueue.Count;
                for (int i = 0; i < n; i++)
                {
                    AdvancedPhysicsTile block = updateQueue.Dequeue();
                    if (((TimeSpan)(DateTime.Now - block.startTime)).TotalMilliseconds >= PhysicsTime(block.phys_type))
                    {
                        switch (block.phys_type)
                        {
                            default:
                                
                                break;
                        }
                    }
                    else
                    {
                        updateQueue.Enqueue(block);
                    }
                }
            }
        }

        public bool Queue(int x, int y, int z, byte type, byte phys_type, object[] meta)
        {
            try
            {
                lock(queueLock)
                {
                    this.updateQueue.Enqueue(new AdvancedPhysicsTile(x, y, z, type, phys_type, meta));
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static int PhysicsTime(byte type)
        {
            switch (type)
            {
                default:
                    return 0;
            }
        }
    }

    public class AdvancedPhysicsTile
    {
        public int x, y, z;
        public byte block_type;
        public byte phys_type;
        public DateTime startTime;
        public object[] meta;

        public AdvancedPhysicsTile(int x, int y, int z, byte block_type, byte phys_type, object[] meta)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.block_type = block_type;
            this.phys_type = phys_type;
            this.meta = meta;
            this.startTime = DateTime.Now;
        }
    }

    public enum PhysType
    {
        Bouncer = 0
    }
}
