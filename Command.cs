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
    public class Command
    {
        public delegate void CommandHandler(Player p, string message);
        public static Dictionary<string, Command> commands = new Dictionary<string, Command>();
        public static Dictionary<string, Command> consoleSafe = new Dictionary<string, Command>();

        public static void Init()
        {
            commands.Add("ban", new Command(DisconnectCommand.Ban, Rank.RankLevel("operator")));
            commands.Add("chars", new Command(ColorCommand.Chars, Rank.RankLevel("guest")));
            commands.Add("colors", new Command(ColorCommand.Colors, Rank.RankLevel("player")));

            commands.Add("copy", new Command(CopyCommand.Copy, Rank.RankLevel("operator")));

            commands.Add("cuboid", new Command(BuildCommand.Cuboid, Rank.RankLevel("operator")));
            commands.Add("c", commands["cuboid"]);
            commands.Add("fetch", new Command(TeleportCommand.Fetch, Rank.RankLevel("operator")));
            commands.Add("guest", new Command(ChangeRankCommand.Guest, Rank.RankLevel("operator")));
            commands.Add("help", new Command(HelpCommand.Help, Rank.RankLevel("guest")));
            commands.Add("ipban", new Command(DisconnectCommand.IpBan, Rank.RankLevel("operator")));
            commands.Add("kick", new Command(DisconnectCommand.Kick, Rank.RankLevel("operator")));
            commands.Add("load", new Command(CopyCommand.Load, Rank.RankLevel("operator")));
            commands.Add("mb", new Command(MessageBlockCommand.AddMsgBlock, Rank.RankLevel("operator")));
            commands.Add("mbdel", new Command(MessageBlockCommand.RemoveMsgBlock, Rank.RankLevel("operator")));
            commands.Add("me", new Command(MeCommand.Me, Rank.RankLevel("guest")));
            commands.Add("newworld", new Command(WorldCommand.NewWorld, Rank.RankLevel("owner")));
            commands.Add("operator", new Command(ChangeRankCommand.Operator, Rank.RankLevel("owner")));
            commands.Add("paint", new Command(BuildCommand.Paint, Rank.RankLevel("player")));

            commands.Add("paste", new Command(CopyCommand.Paste, Rank.RankLevel("operator")));

            commands.Add("place", new Command(PlaceCommand.Place, Rank.RankLevel("player")));
            commands.Add("player", new Command(ChangeRankCommand.Player, Rank.RankLevel("operator")));
            commands.Add("ranks", new Command(HelpCommand.Ranks, Rank.RankLevel("guest")));
            commands.Add("replace", new Command(BuildCommand.Replace, Rank.RankLevel("operator")));
            commands.Add("r", commands["replace"]);
            commands.Add("replacenot", new Command(BuildCommand.ReplaceNot, Rank.RankLevel("operator")));
            commands.Add("rn", commands["replacenot"]);
            commands.Add("rotate", new Command(CopyCommand.Rotate90, Rank.RankLevel("operator")));
            commands.Add("save", new Command(CopyCommand.Save, Rank.RankLevel("operator")));
            commands.Add("say", new Command(SayCommand.Say, Rank.RankLevel("operator")));
            commands.Add("setspawn", new Command(WorldCommand.SetSpawn, Rank.RankLevel("operator")));
            commands.Add("spawn", new Command(WorldCommand.Spawn, Rank.RankLevel("guest")));
            commands.Add("tpblock", new Command(TeleportBlockCommand.TeleportBlock, Rank.RankLevel("operator")));
            commands.Add("tpdel", new Command(TeleportBlockCommand.TeleportBlockDelete, Rank.RankLevel("operator")));
            commands.Add("tp", new Command(TeleportCommand.Tp, Rank.RankLevel("player")));
            commands.Add("unban", new Command(DisconnectCommand.Unban, Rank.RankLevel("operator")));
            commands.Add("unipban", new Command(DisconnectCommand.UnbanIp, Rank.RankLevel("operator")));
            commands.Add("where", new Command(WhereCommand.Where, Rank.RankLevel("player")));
            commands.Add("who", new Command(WhoCommand.Who, Rank.RankLevel("player")));

            consoleSafe.Add("ban",      commands["ban"]);
            consoleSafe.Add("chars",    commands["chars"]);
            consoleSafe.Add("colors",   commands["colors"]);
            consoleSafe.Add("guest",    commands["guest"]);
            consoleSafe.Add("help",     commands["help"]);
            consoleSafe.Add("ipban",    commands["ipban"]);
            consoleSafe.Add("kick",     commands["kick"]);
            consoleSafe.Add("me",       commands["me"]);
            consoleSafe.Add("newworld", commands["newworld"]);
            consoleSafe.Add("owner",    new Command(ChangeRankCommand.Owner, Rank.RankLevel("owner")));
            consoleSafe.Add("operator", commands["operator"]);
            consoleSafe.Add("player",   commands["player"]);
            consoleSafe.Add("ranks",    commands["ranks"]);
            consoleSafe.Add("say",      commands["say"]);
            consoleSafe.Add("unban",    commands["unban"]);
            consoleSafe.Add("unipban",  commands["unipban"]);
            consoleSafe.Add("where",    commands["where"]);
            consoleSafe.Add("who",      commands["who"]);
        }

        public static void HandleCommand(Player p, string cmd, string msg)
        {
            if(commands.ContainsKey(cmd))
            {
                if(p.rank < commands[cmd].minRank)
                {
                    p.SendMessage(0xFF, "You can't use that command!");
                    return;
                }
                Program.server.logger.log(p.username + " uses /" + cmd);
                commands[cmd].handler(p, msg);
                
            }
            else
            {
                p.SendMessage(0xFF, "No such command &c/" + cmd);
            }
        }

        public static void HelpMessage(Player p, string cmd)
        {
            switch (cmd)
            {
                case "chars":
                case "colors":
                    ColorCommand.Help(p, cmd);
                    break;
                case "commands":
                    HelpCommand.Commands(p, cmd);
                    break;
                case "copy":
                case "paste":
                case "save":
                case "load":
                case "rotate":
                    CopyCommand.Help(p, cmd);
                    break;
                case "fetch":
                case "tp":
                    TeleportCommand.Help(p, cmd);
                    break;
                case "guest":
                case "player":
                case "operator":
                    ChangeRankCommand.Help(p, cmd);
                    break;
                case "help":
                    p.SendMessage(0xFF, "/help - Displays a help menu");
                    break;
                case "ranks":
                    p.SendMessage(0xFF, "/ranks - Displays information regarding ranks");
                    break;
                case "kick":
                case "ban":
                case "ipban":
                case "unban":
                case "unipban":
                    DisconnectCommand.Help(p, cmd);
                    break;
                case "mb":
                case "mbdel":
                    MessageBlockCommand.Help(p, cmd);
                    break;
                case "newworld":
                case "setspawn":
                case "spawn":
                    WorldCommand.Help(p, "newworld");
                    break;
                case "place":
                    PlaceCommand.Help(p);
                    break;
                case "paint":
                case "cuboid":
                case "c":
                case "replace":
                case "r":
                case "replacenot":
                case "rn":
                    BuildCommand.Help(p, cmd);
                    break;
                case "say":
                    SayCommand.Help(p);
                    break;
                case "where":
                    WhereCommand.Help(p);
                    break;
                case "who":
                    WhoCommand.Help(p);
                    break;
                case "tpblock":
                case "tpdel":
                    TeleportBlockCommand.Help(p, cmd);
                    break;
                default:
                    break;
            }
        }

        public CommandHandler handler;
        public byte minRank;

        public Command(CommandHandler handler, byte minRank)
        {
            this.handler = handler;
            this.minRank = minRank;
        }

    }
}
