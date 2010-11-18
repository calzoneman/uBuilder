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
                            case PhysType.Door:
                                UpdateDoor(block);
                                break;
                            case PhysType.DoorOpen:
                                UpdateDoorOpen(block);
                                break;
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

        #region Implementations
        public void UpdateDoor(AdvancedPhysicsTile tile)
        {
            if (world.GetTile(tile.x, tile.y, tile.z) != tile.block_type) return;
            byte type = tile.block_type;

            if (world.GetTile(tile.x + 1, tile.y, tile.z) == type)
            {
                Queue(tile.x + 1, tile.y, tile.z, type);
            }

            if (world.GetTile(tile.x - 1, tile.y, tile.z) == type)
            {
                Queue(tile.x - 1, tile.y, tile.z, type);
            }

            if (world.GetTile(tile.x, tile.y + 1, tile.z) == type)
            {
                Queue(tile.x, tile.y + 1, tile.z, type);
            }

            if (world.GetTile(tile.x, tile.y - 1, tile.z) == type)
            {
                Queue(tile.x, tile.y - 1, tile.z, type);
            }

            if (world.GetTile(tile.x, tile.y, tile.z + 1) == type)
            {
                Queue(tile.x, tile.y, tile.z + 1, type);
            }

            if (world.GetTile(tile.x, tile.y, tile.z - 1) == type)
            {
                Queue(tile.x, tile.y, tile.z - 1, type);
            }

            world.SetTileNoPhysics(tile.x, tile.y, tile.z, Blocks.doorOpen);
            Queue(tile.x, tile.y, tile.z, Blocks.doorOpen, PhysType.DoorOpen, new object[] { (byte)type });
        }

        public void UpdateDoorOpen(AdvancedPhysicsTile tile)
        {
            if (world.GetTile(tile.x, tile.y, tile.z) != tile.block_type) return;
            world.SetTileNoPhysics(tile.x, tile.y, tile.z, (byte)tile.meta[0]);
        }
        #endregion

        public bool Queue(int x, int y, int z, byte type)
        {
            switch(type)
            {
                case Blocks.door:
                    return Queue(x, y, z, type, PhysType.Door, new object[] { (byte)Blocks.door });
                default:
                    return false;
            }
        }

        public bool Queue(int x, int y, int z, byte type, PhysType phys_type, object[] meta)
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

        public static int PhysicsTime(PhysType type)
        {
            switch (type)
            {
                case PhysType.Door:
                    return 50;
                case PhysType.DoorOpen:
                    return 5000;
                default:
                    return 0;
            }
        }
    }

    public class AdvancedPhysicsTile
    {
        public int x, y, z;
        public byte block_type;
        public PhysType phys_type;
        public DateTime startTime;
        public object[] meta;

        public AdvancedPhysicsTile(int x, int y, int z, byte block_type, PhysType phys_type, object[] meta)
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
        None,
        Door,
        DoorOpen,
    }
}
