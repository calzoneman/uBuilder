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
    public class BuildCommand
    {

        public static void Paint(Player p, string message)
        {
            if (p.painting)
            {
                p.painting = false;
                p.SendMessage(0xFF, "Paint mode is now &cdisabled");
            }
            else
            {
                p.painting = true;
                p.SendMessage(0xFF, "Paint mode is now &aenabled");
            }
        }

        public static void Cuboid(Player p, string message)
        {
            byte type = 0;
            if (p.cParams.cuboidLock)
            {
                p.SendMessage(0xFF, "Another cuboid is already in progress.  Please wait for it to finish.");
                return;
            }

            if (message.Trim().Equals(""))
            {
                type = 0xFF;
            }
            else
            {
                if (Blocks.blockNames.ContainsKey(message.Trim()))
                {
                    type = Blocks.blockNames[message.Trim()];
                }
                else
                {
                    p.SendMessage(0xFF, "No such blocktype \"" + message.Trim() + "\"");
                    return;
                }
            }
            p.cParams.replace = false;
            p.cParams.replacenot = false;
            p.cParams.type = type;
            p.SendMessage(0xFF, "Change the block of the first corner");
            p.OnBlockchange += new Player.BlockHandler(OnFirstCorner);
        }

        public static void Replace(Player p, string message)
        {
            byte type = 0xFF, replaceType = 0xFF;
            if (p.cParams.cuboidLock)
            {
                p.SendMessage(0xFF, "Another cuboid is already in progress.  Please wait for it to finish.");
                return;
            }

            string[] args = message.Trim().Split(' ');
            if (args.Length != 2)
            {
                Help(p, "replace");
                return;
            }

            if (Blocks.blockNames.ContainsKey(args[0]))
            {
                replaceType = Blocks.blockNames[args[0]];
            }
            else
            {
                p.SendMessage(0xFF, "No such blocktype \"" + args[0] + "\"");
                return;
            }

            if (Blocks.blockNames.ContainsKey(args[1]))
            {
                type = Blocks.blockNames[args[1]];
            }
            else
            {
                p.SendMessage(0xFF, "No such blocktype \"" + args[1] + "\"");
                return;
            }

            p.cParams.replace = true;
            p.cParams.replacenot = false;
            p.cParams.type = type;
            p.cParams.replaceType = replaceType;
            p.SendMessage(0xFF, "Change the block of the first corner");
            p.OnBlockchange += new Player.BlockHandler(OnFirstCorner);
        }

        public static void ReplaceNot(Player p, string message)
        {
            byte type = 0xFF, replaceType = 0xFF;
            if (p.cParams.cuboidLock)
            {
                p.SendMessage(0xFF, "Another cuboid is already in progress.  Please wait for it to finish.");
                return;
            }
            
            string[] args = message.Trim().Split(' ');
            if (args.Length != 2)
            {
                Help(p, "replacenot");
                return;
            }

            if (Blocks.blockNames.ContainsKey(args[0]))
            {
                replaceType = Blocks.blockNames[args[0]];
            }
            else
            {
                p.SendMessage(0xFF, "No such blocktype \"" + args[0] + "\"");
                return;
            }

            if (Blocks.blockNames.ContainsKey(args[1]))
            {
                type = Blocks.blockNames[args[1]];
            }
            else
            {
                p.SendMessage(0xFF, "No such blocktype \"" + args[1] + "\"");
                return;
            }

            p.cParams.replace = false;
            p.cParams.replacenot = true;
            p.cParams.type = type;
            p.cParams.replaceType = replaceType;
            p.SendMessage(0xFF, "Change the block of the first corner");
            p.OnBlockchange += new Player.BlockHandler(OnFirstCorner);
        }

        public static void OnFirstCorner(Player p, int x, int y, int z, byte type)
        {
            if (p.cParams.type == 0xFF) { p.cParams.type = type; }
            p.ResetBlockHandler();
            p.SendBlock((short)x, (short)y, (short)z, p.world.GetTile(x, y, z));
            p.cParams.x1 = x;
            p.cParams.y1 = y;
            p.cParams.z1 = z;
            p.SendMessage(0xFF, "Change the block of the second corner");
            p.OnBlockchange += new Player.BlockHandler(OnSecondCorner);
        }

        public static void OnSecondCorner(Player p, int x2, int y2, int z2, byte type)
        {
            p.ResetBlockHandler();
            p.SendBlock((short)x2, (short)y2, (short)z2, p.world.GetTile(x2, y2, z2));
            int x1 = p.cParams.x1, y1 = p.cParams.y1, z1 = p.cParams.z1;
            p.cParams.cuboidLock = true;

            int xMin = Math.Min(x1, x2);
            int yMin = Math.Min(y1, y2);
            int zMin = Math.Min(z1, z2);

            int xMax = Math.Max(x1, x2);
            int yMax = Math.Max(y1, y2);
            int zMax = Math.Max(z1, z2);

            int size = (xMax + 1 - xMin) * (yMax + 1 - yMin) * (zMax + 1 - zMin);
            if (size > 20000 && p.rank <= Rank.RankLevel("operator"))
            {
                p.SendMessage(0xFF, "You can't make a cuboid that large!");
                return;
            }
            p.SendMessage(0xFF, "Cuboiding &c" + size + "&e blocks");

            System.Threading.Thread cuboidThread = new System.Threading.Thread((System.Threading.ThreadStart)delegate
                {
                    DateTime start = DateTime.Now;
                    for (int nx = xMin; nx <= xMax; nx++)
                    {
                        for (int ny = yMin; ny <= yMax; ny++)
                        {
                            for (int nz = zMin; nz <= zMax; nz++)
                            {
                                if (!p.cParams.replace && !p.cParams.replacenot)
                                {
                                    p.world.SetTile(nx, ny, nz, p.cParams.type);
                                    if (p.cParams.type != 0) Program.server.accounts[p.username.ToLower()].PlaceBlock();
                                    else Program.server.accounts[p.username.ToLower()].DeleteBlock();
                                    System.Threading.Thread.Sleep(1);
                                }
                                else if (p.cParams.replace)
                                {
                                    if (p.world.GetTile(nx, ny, nz) == p.cParams.replaceType)
                                    {
                                        p.world.SetTile(nx, ny, nz, p.cParams.type);
                                        Program.server.accounts[p.username.ToLower()].DeleteBlock();
                                        if(p.cParams.type != 0) Program.server.accounts[p.username.ToLower()].PlaceBlock();
                                        System.Threading.Thread.Sleep(1);
                                    }
                                }
                                else if (p.cParams.replacenot)
                                {
                                    if (p.world.GetTile(nx, ny, nz) != p.cParams.replaceType)
                                    {
                                        p.world.SetTile(nx, ny, nz, p.cParams.type);
                                        Program.server.accounts[p.username.ToLower()].DeleteBlock();
                                        if (p.cParams.type != 0) Program.server.accounts[p.username.ToLower()].PlaceBlock();
                                        System.Threading.Thread.Sleep(1);
                                    }
                                }
                                
                            }
                        }
                    }
                    double time = ((TimeSpan)(DateTime.Now - start)).TotalSeconds;
                    p.SendMessage(0x00, "&c" + size + "&e blocks in &c" + (int)(time * 10.0) / 10.0 + "&e seconds");
                    p.SendMessage(0xFF, "(&c" + (int)((size / time)*10.0) / 10.0 + "&e blocks/sec)");
                    p.cParams.cuboidLock = false;
                });
            cuboidThread.Start();
            
        }



        public static void Help(Player p, string message)
        {
            switch (message)
            {
                case "paint":
                    p.SendMessage(0xFF, "/paint - Toggles paint mode (delete blocks to replace them with what you are holding");
                    break;
                case "c":
                case "cuboid":
                    p.SendMessage(0xFF, "/cuboid - Draw a cuboid between two corners of the type you are holding");
                    p.SendMessage(0xFF, "/cuboid type - Same as /cuboid, but draws blocks of type type");
                    break;
                case "replace":
                case "r":
                    p.SendMessage(0xFF, "/replace type1 type2 - Replaces all of type1 with type2 in the cuboid");
                    break;
                case "rn":
                case "replacenot":
                    p.SendMessage(0xFF, "/replacenot type1 type2 - Replaces all not of type1 with type2 in the cuboid");
                    break;
                default:
                    break;
            }
        }

    }

    public struct CuboidParameters
    {
        public int x1, y1, z1;
        public byte type;
        public byte replaceType;
        public bool replace, replacenot;
        public bool cuboidLock;
    }

}
