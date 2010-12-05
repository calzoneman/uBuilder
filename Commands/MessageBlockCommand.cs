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
    public class MessageBlockCommand
    {
        public static void AddMsgBlock(Player p, string message)
        {
            message = Player.ParseColors(message);
            if (message.Contains("MB_END"))
            {
                p.OnBlockchange += new Player.BlockHandler(BlockPlaced);
                p.messageBlockText = message.Substring(0, message.IndexOf("MB_END"));
                p.SendMessage(0xFF, "Place a block to finalize the message block");
            }
            else
            {
                p.OnChat += new Player.ChatHandler(ChatReceived);
                p.messageBlockText = message;
                p.SendMessage(0xFF, "Message entry will continue until a message contains MB_END");
            }
        }

        public static void RemoveMsgBlock(Player p, string message)
        {
            p.SendMessage(0xFF, "Delete a block to remove the message");
            p.OnBlockchange += new Player.BlockHandler(BlockRemoved);
        }

        public static void ChatReceived(Player p, string message)
        {
            if (message.Contains("MB_END"))
            {
                p.OnChat -= new Player.ChatHandler(ChatReceived);
                p.OnBlockchange += new Player.BlockHandler(BlockPlaced);
                p.messageBlockText += message.Substring(0, message.IndexOf("MB_END"));
                p.SendMessage(0xFF, "Place a block to finalize the message block");
            }
            else
            {
                p.messageBlockText += Player.ParseColors(Player.ParseSpecialChar(message));
            }
        }

        public static void BlockPlaced(Player p, int x, int y, int z, byte type)
        {
            if(type == 0) type = p.world.GetTile(x, y, z);
            if (p.world.messageBlocks.ContainsKey(p.world.CoordsToIndex((short)x, (short)y, (short)z)))
            {
                p.SendMessage(0xFF, "A message block already exists there!");
            }
            else
            {
                p.world.messageBlocks.Add(p.world.CoordsToIndex((short)x, (short)y, (short)z), p.messageBlockText);
                p.SendMessage(0xFF, "Message block created");
            }
            p.world.SetTile(x, y, z, type);
            p.world.Save();
            
            p.OnBlockchange -= new Player.BlockHandler(BlockPlaced);
        }

        public static void BlockRemoved(Player p, int x, int y, int z, byte type)
        {
            p.SendBlock((short)x, (short)y, (short)z, p.world.GetTile(x, y, z));
            p.world.messageBlocks.Remove(p.world.CoordsToIndex((short)x, (short)y, (short)z));
            p.world.Save();
            p.SendMessage(0xFF, "Message block deleted");
            p.OnBlockchange -= new Player.BlockHandler(BlockRemoved);
        }

        public static void Help(Player p, string cmd)
        {
            switch (cmd)
            {
                case "mb":
                    p.SendMessage(0xFF, "/mb message - Create a message block.  Message must end with MB_END, or will keep intercepting chat until it gets MB_END");
                    break;
                case "mbdel":
                    p.SendMessage(0xFF, "/mbdel - Delete a message block");
                    break;
                default:
                    break;
            }
        }
    }
}
