/**
 * uBuilder - A lightweight custom Minecraft Classic server written in C#
 * Copyright 2010 Calvin "calzoneman" Montgomery
 * 
 * Licensed under the Creative Commons Attribution-ShareAlike 3.0 Unported License
 * (see http://creativecommons.org/licenses/by-sa/3.0/, or LICENSE.txt for a full license
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace uBuilder
{
    public class CopyCommand
    {
        #region /copy
        public static void Copy(Player p, string message)
        {
            p.SendMessage(0xFF, "Change the block at the first corner");
            p.OnBlockchange += new Player.BlockHandler(OnFirst);
        }

        public static void OnFirst(Player p, int x, int y, int z, byte type)
        {
            p.SendBlock((short)x, (short)y, (short)z, p.world.GetTile(x, y, z));
            p.cParams.x1 = x;
            p.cParams.y1 = y;
            p.cParams.z1 = z;
            p.OnBlockchange -= new Player.BlockHandler(OnFirst);
            p.OnBlockchange += new Player.BlockHandler(OnSecond);
            p.SendMessage(0xFF, "Change the block at the second corner");
        }

        public static void OnSecond(Player p, int x, int y, int z, byte type)
        {
            p.SendBlock((short)x, (short)y, (short)z, p.world.GetTile(x, y, z));
            p.cParams.cuboidLock = true;
            int x1 = p.cParams.x1, y1 = p.cParams.y1, z1 = p.cParams.z1;
            int xMin = Math.Min(x1, x);
            int yMin = Math.Min(y1, y);
            int zMin = Math.Min(z1, z);

            int xMax = Math.Max(x1, x);
            int yMax = Math.Max(y1, y);
            int zMax = Math.Max(z1, z);

            if (((xMax + 1 - xMin) * (yMax + 1 - yMin) * (zMax + 1 - zMin)) > 100000 && p.rank < Rank.RankLevel("operator"))
            {
                p.SendMessage(0xFF, "You can't copy something that large!");
                return;
            }

            p.copyClipboard = new Block[(xMax + 1 - xMin) * (yMax + 1 - yMin) * (zMax + 1 - zMin)];
            for (int nx = xMin; nx <= xMax; nx++)
            {
                for (int ny = yMin; ny <= yMax; ny++)
                {
                    for (int nz = zMin; nz <= zMax; nz++)
                    {
                        p.copyClipboard[((ny - yMin) * (zMax + 1 - zMin) + (nz - zMin)) * (xMax + 1 - xMin) + (nx - xMin)] = new Block((short)(nx - xMin), (short)(ny - yMin), (short)(nz - zMin), p.world.GetTile(nx, ny, nz));
                    }
                }
            }

            p.SendMessage(0xFF, "Copied &c" + ((xMax + 1 - xMin) * (yMax + 1 - yMin) * (zMax + 1 - zMin)) + "&e blocks");
            p.OnBlockchange -= new Player.BlockHandler(OnSecond);
            p.cParams.cuboidLock = false;
        }
        #endregion

        #region /paste
        public static void Paste(Player p, string message)
        {
            if(p.cParams.cuboidLock)
            {
                p.SendMessage(0xFF, "Please allow any cuboids to finish before executing /paste");
                return;
            }
            if (p.copyClipboard == null)
            {
                p.SendMessage(0xFF, "You haven't copied or loaded anything yet!");
                return;
            }
            p.SendMessage(0xFF, "Change a block to see a ghost of the paste");
            p.OnBlockchange += new Player.BlockHandler(OnPasteBlockchange);
            p.cParams.x1 = -1;
            p.cParams.y1 = -1;
            p.cParams.z1 = -1;
        }

        public static void OnPasteBlockchange(Player p, int x, int y, int z, byte type)
        {
            if (p.cParams.x1 != -1 && p.cParams.y1 != -1 && p.cParams.z1 != -1)
            {
                foreach (Block b in p.copyClipboard)
                {
                    p.SendBlock((short)(b.x + p.cParams.x1), (short)(b.y + p.cParams.y1), (short)(b.z + p.cParams.z1), p.world.GetTile(b.x + p.cParams.x1, b.y + p.cParams.y1, b.z + p.cParams.z1));
                }
            }
            else
            {
                p.SendMessage(0xFF, "Type \"finalize\" to finish the paste, \"cancel\" to cancel it, or \"rotate\" to rotate it.");
                p.OnChat += new Player.ChatHandler(OnPasteChat);
            }

            p.cParams.x1 = x;
            p.cParams.y1 = y;
            p.cParams.z1 = z;

            foreach (Block b in p.copyClipboard)
            {
                byte bType = Blocks.glass;
                if (b.type == Blocks.air) bType = Blocks.air;
                if (b.type == Blocks.water || b.type == Blocks.waterstill) bType = Blocks.waterstill;
                if (b.type == Blocks.lava || b.type == Blocks.lavastill) bType = Blocks.lavastill;
                p.SendBlock((short)(b.x + x), (short)(b.y + y), (short)(b.z + z), bType);
            }


        }

        public static void OnPasteChat(Player p, string msg)
        {
            if (msg.Trim().ToLower().Equals("finalize"))
            {
                p.cParams.cuboidLock = true;
                foreach (Block b in p.copyClipboard)
                {
                    p.world.SetTile(b.x + p.cParams.x1, b.y + p.cParams.y1, b.z + p.cParams.z1, b.type);
                    if (b.type == Blocks.air) Program.server.accounts[p.username.ToLower()].blocksDestroyed++;
                    else Program.server.accounts[p.username.ToLower()].blocksCreated++;
                }
                p.cParams.cuboidLock = false;
                p.OnChat -= new Player.ChatHandler(OnPasteChat);
                p.OnBlockchange -= new Player.BlockHandler(OnPasteBlockchange);
                p.SendMessage(0xFF, "Pasted.");
            }
            else if (msg.Trim().ToLower().Equals("cancel"))
            {
                if (p.cParams.x1 != -1 && p.cParams.y1 != -1 && p.cParams.z1 != -1)
                {
                    foreach (Block b in p.copyClipboard)
                    {
                        p.SendBlock((short)(b.x + p.cParams.x1), (short)(b.y + p.cParams.y1), (short)(b.z + p.cParams.z1), p.world.GetTile(b.x + p.cParams.x1, b.y + p.cParams.y1, b.z + p.cParams.z1));
                    }
                }
                p.OnChat -= new Player.ChatHandler(OnPasteChat);
                p.OnBlockchange -= new Player.BlockHandler(OnPasteBlockchange);
                p.SendMessage(0xFF, "Canceled.");
            }
            else if (msg.Trim().ToLower().Equals("rotate"))
            {
                Rotate90(p, "");
            }
        }
        #endregion

        #region save/load

        public static void Save(Player p, string message)
        {
            if (p.copyClipboard == null)
            {
                p.SendMessage(0xFF, "You haven't copied anything to save!");
                return;
            }
            if (message.Trim().Equals(""))
            {
                p.SendMessage(0xFF, "No filename entered!");
                return;
            }

            if (!Directory.Exists("save")) Directory.CreateDirectory("save");
            string path = "save/" + p.username.ToLower() + "/" + message.Trim().ToLower();
            if (message.Contains("$GLOBAL/"))
                path = "save/" + message.Substring(8).Trim().ToLower();

            else
            {
                if (!Directory.Exists("save/" + p.username.ToLower())) Directory.CreateDirectory("save/" + p.username.ToLower());
                p.SendMessage(0xFF, "Saving to your folder.  You can access it by using /load " + p.username.ToLower() + "/" + message.Trim().ToLower());
            }

            if (File.Exists(path))
            {
                p.SendMessage(0xFF, "That file already exists!");
                return;
            }


            try
            {
                FileStream fOut = new FileStream(path, FileMode.CreateNew);

                fOut.Write(Encoding.UTF8.GetBytes("SAVE"), 0, 4); //Signature
                fOut.Write(BitConverter.GetBytes(p.copyClipboard.Length), 0, 4);

                foreach (Block b in p.copyClipboard)
                {
                    fOut.Write(BitConverter.GetBytes(b.x), 0, 2);
                    fOut.Write(BitConverter.GetBytes(b.y), 0, 2);
                    fOut.Write(BitConverter.GetBytes(b.z), 0, 2);
                    fOut.Write(BitConverter.GetBytes(b.type), 0, 1);
                }

                fOut.Close();
                p.SendMessage(0xFF, "Saved.");
            }
            catch(Exception e)
            {
                Program.server.logger.log(e);
                p.SendMessage(0xFF, "Something went wrong.  Your file probably did not save.");
            }
        }

        public static void Load(Player p, string message)
        {
            message = message.Trim().ToLower();
            if (!File.Exists("save/" + message))
            {
                p.SendMessage(0xFF, "That file doesn't exist!");
                return;
            }

            try
            {
                FileStream fIn = new FileStream("save/" + message, FileMode.Open);

                byte[] signature = new byte[4];
                fIn.Read(signature, 0, 4);
                if (!Encoding.UTF8.GetString(signature).Equals("SAVE"))
                {
                    p.SendMessage(0xFF, "Save file is corrupt.");
                    fIn.Close();
                    return;
                }

                byte[] length = new byte[4];
                fIn.Read(length, 0, 4);
                int number = BitConverter.ToInt32(length, 0);
                p.copyClipboard = new Block[number];

                for (int i = 0; i < number; i++)
                {
                    byte[] blockBytes = new byte[7];
                    fIn.Read(blockBytes, 0, 7);

                    short x = BitConverter.ToInt16(blockBytes, 0);
                    short y = BitConverter.ToInt16(blockBytes, 2);
                    short z = BitConverter.ToInt16(blockBytes, 4);
                    byte type = blockBytes[6];

                    p.copyClipboard[i] = new Block(x, y, z, type);
                }

                fIn.Close();
                p.SendMessage(0xFF, "Loaded save.  Use /paste to put it somewhere");
            }
            catch (Exception e)
            {
                Program.server.logger.log(e);
                p.SendMessage(0xFF, "Something went wrong.  I don't think your save loaded right.");
            }
        }

        #endregion

        #region manipulation
        public static void Rotate90(Player p, string message)
        {
            int width = 0;
            foreach (Block b in p.copyClipboard)
            {
                if (b.x > width) width = b.x;
            }
            if (width == 0)
            {
                p.SendMessage(0xFF, "Invalid copy clipboard");
                return;
            }

            foreach (Block b in p.copyClipboard)
            {
                short temp = b.x;
                b.x = b.z;
                b.z = (short)(width - temp);
            }

            p.SendMessage(0xFF, "Rotated your copy clipboard.");
        }
        #endregion

        public static void Help(Player p, string cmd)
        {
            switch (cmd)
            {
                case "copy":
                    p.SendMessage(0xFF, "/copy - Copies the selected cuboid into your clipboard");
                    break;
                case "paste":
                    p.SendMessage(0xFF, "/paste - Pastes your clipboard");
                    break;
                case "save":
                    p.SendMessage(0xFF, "/save name - Saves a copied cuboid to your folder");
                    p.SendMessage(0xFF, "/save $GLOBAL/name - Saves a cuboid to the global folder");
                    break;
                case "load":
                    p.SendMessage(0xFF, "/load username/save - Loads save from username's folder");
                    p.SendMessage(0xFF, "/load save - Loads save from the Global folder");
                    break;
                case "rotate":
                    p.SendMessage(0xFF, "/rotate - Rotates your copy clipboard 90deg CCW");
                    break;
                default:
                    break;
            }
        }
    }


    public class Block
    {
        public short x, y, z;
        public byte type;

        public Block(short x, short y, short z, byte type)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.type = type;
        }
    }
}
