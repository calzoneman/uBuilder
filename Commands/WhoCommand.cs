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
    public class WhoCommand
    {
        public static void Who(Player p, string message)
        {
            Player pl = Player.FindPlayer(p, message.Trim(), true);
            if (pl != null)
            {
                StringBuilder msg = new StringBuilder();
                msg.Append(pl.GetFormattedName());
                msg.Append("&e is ranked " + Rank.GetColor(pl.rank) + Rank.RankName(pl.rank));
                msg.Append("&e and is connected from IP &b" + pl.ip);
                p.SendMessage(0x00, msg.ToString());
                Account a = Program.server.accounts[pl.username.ToLower()];
                p.SendMessage(0xFF, "Visit count: &c" + a.visitcount);
                p.SendMessage(0xFF, "Blocks Created: &c" + a.blocksCreated + "&e Destroyed: &c" + a.blocksDestroyed + "&e Ratio: &c" + Math.Round(a.blockRatio, 3));
                p.SendMessage(0xFF, "Message count: &c" + a.messagesSent);

            }
            else if (Program.server.accounts.ContainsKey(message.Trim()) && Program.server.playerRanksDict.ContainsKey(message.Trim()))
            {
                StringBuilder msg = new StringBuilder();
                msg.Append(message.Trim().ToLower());
                msg.Append("&e is ranked " + Rank.GetColor(Program.server.playerRanksDict[message.Trim().ToLower()]) + Rank.RankName(Program.server.playerRanksDict[message.Trim().ToLower()]));
                msg.Append("&e last connected from IP &b" + Program.server.accounts[message.Trim().ToLower()].lastseenip);
                p.SendMessage(0x00, msg.ToString());
                Account a = Program.server.accounts[message.Trim().ToLower()];
                p.SendMessage(0xFF, "Visit count: &c" + a.visitcount);
                p.SendMessage(0xFF, "Blocks Created: &c" + a.blocksCreated + "&e Destroyed: &c" + a.blocksDestroyed + "&e Ratio: &c" + Math.Round(a.blockRatio, 3));
                p.SendMessage(0xFF, "Message count: &c" + a.messagesSent);
            }
            else
            {
                p.SendMessage(0xFF, "Command failed (could not find player)");
            }
        }

        public static void Help(Player p)
        {
            p.SendMessage(0xFF, "/who player - Displays information about player");
        }
    }
}
