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
    public class HelpCommand
    {
        public static void Help(Player p, string message)
        {
            if (!message.Trim().Equals(""))
            {
                Command.HelpMessage(p, message.Trim());
                return;
            }
            p.SendMessage(0xFF, "Running uBuilder Rev. &a" + Program.revision.ToString() + "&e " + (char)1 + ".");
            p.SendMessage(0xFF, "---------------------");
            p.SendMessage(0xFF, "Global messages are enclosed in brackets [ ]");
            p.SendMessage(0xFF, "Visit &fhttp://github.com/calzoneman/uBuilder&e for downloads and source code.");
            p.SendMessage(0xFF, "---------------------");
            p.SendMessage(0xFF, "Type &5/help commands&e for information about available commands");
            p.SendMessage(0xFF, "Type &5/help commandname&e for information about a specific command");
            p.SendMessage(0xFF, "Type &5/ranks&e for information on ranks");

        }

        public static void Commands(Player p, string message)
        {
            StringBuilder availableCmds = new StringBuilder();
            availableCmds.Append("Available commands:");
            foreach (KeyValuePair<string, Command> cmd in Command.commands)
            {
                if (cmd.Value.minRank <= p.rank)
                {
                    availableCmds.Append(" ");
                    availableCmds.Append(Rank.GetColor(cmd.Value.minRank));
                    availableCmds.Append(cmd.Key);
                }
            }
            p.SendMessage(0xFF, availableCmds.ToString());
        }

        public static void Ranks(Player p, string message)
        {
            p.SendMessage(0xFF, "Ranks: ");
            p.SendMessage(0, "&4@owner &e(" + Rank.RankLevel("owner") + ")");
            p.SendMessage(0, "&9+operator &e(" + Rank.RankLevel("operator") + ")");
            p.SendMessage(0, "player &e(" + Rank.RankLevel("player") + ")");
            p.SendMessage(0, "&7guest &e(" + Rank.RankLevel("guest") + ")");
            p.SendMessage(0, "&0[:(]banned &e(" + Rank.RankLevel("none") + ")");
        }
    }
}
