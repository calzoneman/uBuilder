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
    public class MessageBlockCheck
    {
        public static void Init(Player p)
        {
            //Enable the messageblock handler for player position changes
            p.OnMovement += new Player.PositionChangeHandler(Check);
        }

        public static void Check(Player p, short[] oldPos, byte[] oldRot, short[] newPos, byte[] newRot)
        {
            
            short x = (short)(newPos[0] >> 5);
            short y = (short)((newPos[1] >> 5) - 2);
            short z = (short)(newPos[2] >> 5);

            //Don't spam them if they haven't moved
            if (oldPos[0] >> 5 == x && (oldPos[1] >> 5) - 2 == y && oldPos[2] >> 5 == z) return;
            
            if (p.world.messageBlocks.ContainsKey(p.world.CoordsToIndex(x, y, z)))
            {
                p.SendMessage(0x0, "&e" + p.world.messageBlocks[p.world.CoordsToIndex(x, y, z)]);
            }
        }
    }
}
