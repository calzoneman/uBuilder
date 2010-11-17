/**
 * uBuilder - A lightweight custom Minecraft Classic server written in C#
 * Copyright 2010 Calvin "calzoneman" Montgomery
 * 
 * Licensed under the Creative Commons Attribution-ShareAlike 3.0 Unported License
 * (see http://creativecommons.org/licenses/by-sa/3.0/, or LICENSE.txt for a full license
 */


using System;
using System.Collections.Generic;
using System.Text;

namespace uBuilder
{
    public class TeleportBlockCommand
    {
        #region TeleportBlock
        public static void TeleportBlock(Player p, string message)
        {
            p.SendMessage(0xFF, "Change a block to make the first entrance");
            p.OnBlockchange += new Player.BlockHandler(OnFirstBlock);
        }

        public static void OnFirstBlock(Player p, int x, int y, int z, byte type)
        {
            p.world.SetTile(x, y, z, Blocks.teleportBlock);
            p.SendMessage(0xFF, "Change a block to make the link");
            p.tParams.x1 = x;
            p.tParams.y1 = y;
            p.tParams.z1 = z;
            p.OnBlockchange -= new Player.BlockHandler(OnFirstBlock);
            p.OnBlockchange += new Player.BlockHandler(OnSecondBlock);
        }

        public static void OnSecondBlock(Player p, int x, int y, int z, byte type)
        {
            p.world.SetTile(x, y, z, Blocks.teleportBlock);
            int index1 = p.world.CoordsToIndex((short)p.tParams.x1, (short)p.tParams.y1, (short)p.tParams.z1);
            int index2 = p.world.CoordsToIndex((short)x, (short)y, (short)z);
            if (p.world.teleportBlocks.ContainsKey(index1) || p.world.teleportBlocks.ContainsKey(index2))
            {
                p.SendMessage(0xFF, "A link already exists for that block!");
            }
            else
            {
                p.world.teleportBlocks.Add(index2, index1);
                p.world.teleportBlocks.Add(index1, index2);
                p.world.Save();
                p.SendMessage(0xFF, "Linked.");
            }
            p.OnBlockchange -= new Player.BlockHandler(OnSecondBlock);
        }
        #endregion

        #region TeleportBlockDelete
        public static void TeleportBlockDelete(Player p, string message)
        {
            p.SendMessage(0xFF, "Remove a block to delete the teleport associated with it");
            p.OnBlockchange += new Player.BlockHandler(BlockDeleted);
        }

        public static void BlockDeleted(Player p, int x, int y, int z, byte type)
        {
            p.world.SetTile(x, y, z, Blocks.air);
            int index = p.world.CoordsToIndex((short)x, (short)y, (short)z);
            if (p.world.teleportBlocks.ContainsKey(index))
            {
                int index2 = p.world.teleportBlocks[index];
                if (p.world.teleportBlocks.ContainsKey(index2))
                {
                    short[] coords2 = p.world.IndexToCoords(index2);
                    p.world.SetTile(coords2[0], coords2[1], coords2[2], Blocks.air);
                    p.world.teleportBlocks.Remove(index2);
                    p.SendMessage(0xFF, "Unlinked.");
                }
                else
                {
                    p.SendMessage(0xFF, "Teleport block link could not be found");
                }
                p.world.teleportBlocks.Remove(index);
                p.world.Save();
                p.SendMessage(0xFF, "Removed teleport block");
            }
            else
            {
                p.SendMessage(0xFF, "That is not a teleport block!");
            }
            p.OnBlockchange -= new Player.BlockHandler(BlockDeleted);
        }
        #endregion

        public static void Help(Player p, string cmd)
        {
            switch (cmd)
            {
                case "tpblock":
                    p.SendMessage(0xFF, "/tpblock - Create a set of teleport blocks");
                    break;
                case "tpdel":
                    p.SendMessage(0xFF, "/tpdel - Remove a teleport block");
                    break;
                default:
                    break;
            }
        }
    }

    public struct TeleportBlockParams
    {
        public int x1, y1, z1;
    }
}
