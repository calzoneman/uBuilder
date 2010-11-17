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
    public class TeleportBlockCheck
    {
        public static void Init(Player p)
        {
            p.OnMovement += new Player.PositionChangeHandler(Check);
        }

        public static void Check(Player p, short[] oldPos, byte[] oldRot, short[] newPos, byte[] newRot)
        {
            //If the player has moved a whole block since the last teleport, allow them to be teleported by resetting p.lastTeleport
            if (newPos[0] >> 5 != p.lastTeleport[0] || newPos[1] >> 5 != p.lastTeleport[1] || newPos[2] >> 5 != p.lastTeleport[2]) p.lastTeleport = new short[] { 0, 0, 0 };
            
            //If they're standing in the place they were teleported to, don't do anything
            if (newPos[0] >> 5 == p.lastTeleport[0] && newPos[1] >> 5 == p.lastTeleport[1] && newPos[2] >> 5 == p.lastTeleport[2]) return;

            short x = (short)(newPos[0] >> 5);
            short y = (short)((newPos[1] >> 5) - 2);
            short z = (short)(newPos[2] >> 5);

            if (p.world.teleportBlocks.ContainsKey(p.world.CoordsToIndex(x, y, z)))
            {
                //Get the coordinates from the map index
                short[] rawcoords = p.world.IndexToCoords(p.world.teleportBlocks[p.world.CoordsToIndex(x, y, z)]);
                //Teleport to the center of the block
                p.SendSpawn(new short[] { (short)((rawcoords[0] << 5) + 16), (short)((rawcoords[1] << 5) + 64), (short)((rawcoords[2] << 5) + 16) }, newRot);
                //Set the lastTeleport to the new location
                p.lastTeleport = new short[] { rawcoords[0], (short)(rawcoords[1] + 2), rawcoords[2] };
            }
        }
    }
}
