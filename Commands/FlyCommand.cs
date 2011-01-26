using System;
using System.Collections.Generic;
using System.Text;

namespace uBuilder
{
    public class FlyCommand
    {
        public static void Fly(Player p, string message) 
        {
            if (!p.flying)
            {
                p.SendMessage(0xFF, "Fly mode is now &aenabled");
                p.flying = true;
                p.OnMovement += new Player.PositionChangeHandler(FlyMove);
            }
            else
            {
                p.SendMessage(0xFF, "Fly mode is now &cdisabled");
                p.flying = false;
                p.OnMovement -= new Player.PositionChangeHandler(FlyMove);
                foreach(Block b in p.flyBlocks)
                {
                    p.SendBlock(b.x, b.y, b.z, p.world.GetTile(b.x, b.y, b.z));
                }
            }
        }

        public static void Help(Player p)
        {
            p.SendMessage(0xFF, "/fly - Toggle flying by drawing glass platforms under you");
        }

        public static void FlyMove(Player p, short[] oldPos, byte[] oldRot, short[] newPos, byte[] newRot)
        {
            if (p.flyBlocks == null) { p.flyBlocks = new List<Block>(); }
            if (Math.Abs((newPos[0] >> 5) - p.lastFlyPos[0]) >= 1 || Math.Abs((newPos[2] >> 5) - p.lastFlyPos[2]) >= 1 || Math.Abs(p.lastFlyPos[1] - ((newPos[1] >> 5) - 2)) >= 1)
            {
                int oy = (p.lastFlyPos[1] < p.world.height ? p.lastFlyPos[1] : p.world.height - 1);
                int ny = ((newPos[1] >> 5) - 2 < p.world.height ? (newPos[1] >> 5) - 2 : p.world.height - 1);

                List<Block> newPlatform = new List<Block>();

                for (int x = ((newPos[0] >> 5) - 3); x < ((newPos[0] >> 5) + 3); x++)
                {
                    for (int z = ((newPos[2] >> 5) - 3); z < ((newPos[2] >> 5) + 3); z++)
                    {
                        newPlatform.Add(new Block((short)x, (short)ny, (short)z, Blocks.glass));
                    }
                }

                List<Block> pNewBlocks = new List<Block>(p.flyBlocks);

                foreach (Block b in p.flyBlocks)
                {
                    if (!newPlatform.Contains(b))
                    {
                        pNewBlocks.Remove(b);
                        p.SendBlock(b.x, b.y, b.z, p.world.GetTile(b.x, b.y, b.z));
                    }
                }

                foreach (Block b in newPlatform)
                {
                    if (!pNewBlocks.Contains(b))
                    {
                        pNewBlocks.Add(b);
                        p.SendBlock(b.x, b.y, b.z, Blocks.glass);
                    }
                }

                p.flyBlocks = pNewBlocks;

                /*for (int x = p.lastFlyPos[0] - 3; x < p.lastFlyPos[0] + 3; x++)
                {
                    for (int z = p.lastFlyPos[2] - 3; z < p.lastFlyPos[2] + 3; z++)
                    {
                        p.SendBlock((short)x, (short)oy, (short)z, p.world.GetTile(x, oy, z));
                    }
                }
                */
                p.lastFlyPos = new int[] { newPos[0] >> 5, (newPos[1] >> 5) - 2, newPos[2] >> 5 };
            }
        }
    }
}
