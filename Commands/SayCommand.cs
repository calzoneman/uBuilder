﻿/**
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
    public class SayCommand
    {
        public static void Say(Player p, string message)
        {
            StringBuilder finalMsg = new StringBuilder();
            message = message.Trim();
            message = Player.ParseSpecialChar(message);
            for (int i = 0; i < message.Length; i++)
            {
                char ch = message[i];
                if (ch == '%' && i + 1 < message.Length && "0123456789abcdef".Contains(message[i + 1].ToString()) && i + 2 < message.Length)
                {
                    ch = '&';
                }
                finalMsg.Append(ch);
            }
            finalMsg.Append("&e");
            Player.GlobalMessage(finalMsg.ToString());
        }

        public static void Help(Player p)
        {
            p.SendMessage(0xFF, "/say message - Sends message as a global announcement");
            p.SendMessage(0xFF, "-> use %0-9a-f for color codes (see /colors)");
            p.SendMessage(0xFF, "-> use \\code for special chars (see /chars)");
        }
    }
}
