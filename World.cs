/**
 * uBuilder - A lightweight custom Minecraft Classic server written in C#
 * Copyright 2010 Calvin "calzoneman" Montgomery
 * 
 * Licensed under the Creative Commons Attribution-ShareAlike 3.0 Unported License
 * (see http://creativecommons.org/licenses/by-sa/3.0/, or LICENSE.txt for a full license
 */

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Text;

namespace uBuilder
{
    public class World
    {
        public short width, height, depth;
        public short spawnx, spawny, spawnz;
        public byte srotx, sroty; //Spawn rotation
        public byte[] blocks;
        public string name;
        private string filename;
        public Dictionary<int, string> messageBlocks;
        public Dictionary<int, int> teleportBlocks;

        public bool blockChanged = false;
        public DateTime lastSaveTime = DateTime.Now;

        public World(string filename)
        {
            try
            {
                if (filename.Substring(filename.LastIndexOf('.') + 1, 3).Equals("umo"))
                {
                    LoadOld(filename);
                }
                else
                {
                    Load(filename);
                }
            }
            catch
            {
                Program.server.logger.log("Error while loading map.");
            }
        }

        public World(short width, short height, short depth)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;
            this.name = "default";
            this.filename = "default.umw";
            this.blocks = WorldGenerator.GenerateFlatgrass(width, height, depth);
            this.spawnx = (short)(this.width / 2);
            this.spawny = (short)(this.height / 2 + 2);
            this.spawnz = (short)(this.depth / 2);
            this.messageBlocks = new Dictionary<int, string>();
        }

        public World(string filename, short width, short height, short depth)
        {
            this.width = width;
            this.height = height;
            this.depth = depth;
            this.name = filename.Substring(0, filename.LastIndexOf('.'));
            this.filename = filename;
            this.blocks = WorldGenerator.GenerateFlatgrass(width, height, depth);
            this.spawnx = (short)(this.width / 2);
            this.spawny = (short)(this.height / 2 + 2);
            this.spawnz = (short)(this.depth / 2);
            this.messageBlocks = new Dictionary<int, string>();
        }

        public byte GetTile(int x, int y, int z)
        {
            if (x < 0 || y < 0 || z < 0 || x >= this.width || y >= this.height || z >= this.depth)
            {
                return 0xFF;
            }
            return this.blocks[(y * this.depth + z) * this.width + x];
        }

        public bool SetTile(int x, int y, int z, byte type)
        {
            if (x < 0 || y < 0 || z < 0 || x >= this.width || y >= this.height || z >= this.depth || type < 0 || !Blocks.blockNames.ContainsValue(type))
            {
                return false;
            }
            //Handle basic physics tiles
            if (Blocks.BasicPhysics(type))
            {
                if (Blocks.AffectedBySponges(type) && Program.server.physics.FindSponge(x, y, z))
                {
                    return false;
                }
                Program.server.physics.Queue(x, y, z, type);
            }
            //Handle sponge deletion
            if (GetTile(x, y, z) == Blocks.sponge && type == 0)
            {
                Program.server.physics.DeleteSponge(x, y, z);
            }
            this.blocks[(y * this.depth + z) * this.width + x] = type;
            Player.GlobalBlockchange((short)x, (short)y, (short)z, Blocks.ConvertType(type));
            blockChanged = true;
            return true;
        }

        public bool PlayerSetTile(int x, int y, int z, byte type, PermissionSet permissions)
        {
            byte mapBlock = GetTile(x, y, z);

            if (type > 49)
            {
                if (type == Blocks.unflood && !permissions.CanBuildLiquids) return false;
                if (type == Blocks.door || type == Blocks.irondoor || type == Blocks.darkgreydoor && !permissions.CanEditDoors) return false;
            }
            return true;
        }

        public int CoordsToIndex(short x, short y, short z)
        {
            return (y * this.depth + z) * this.width + x;
        }

        public short[] IndexToCoords(int input)
        {
            short x, y, z;
            y = (short)(input / width / depth); input -= y * width * depth;
            z = (short)(input / width); input -= z * width; x = (short)input;
            return new short[] { x, y, z };
        }

        public bool SetTileNoPhysics(int x, int y, int z, byte type)
        {
            if (x < 0 || y < 0 || z < 0 || x >= this.width || y >= this.height || z >= this.depth || type < 0 || !Blocks.blockNames.ContainsValue(type))
            {
                return false;
            }
            this.blocks[(y * this.depth + z) * this.width + x] = type;
            Player.GlobalBlockchange((short)x, (short)y, (short)z, Blocks.ConvertType(type));
            blockChanged = true;
            return true;
        }

        public void Save()
        {
            if (!blockChanged) return; //Don't save if nothing happened
            try
            {
                byte[] saveblocks = new byte[blocks.Length];
                blocks.CopyTo(saveblocks, 0);
                for (int i = 0; i < saveblocks.Length; i++)
                {
                    switch (saveblocks[i])
                    {
                        case Blocks.unflood:
                            saveblocks[i] = Blocks.ConvertType(saveblocks[i]);
                            break;
                        case Blocks.doorOpen:
                            saveblocks[i] = Blocks.door;
                            break;
                        default:
                            break;
                    }
                }

                GZipStream gzout = new GZipStream(new FileStream("maps/" + filename, FileMode.OpenOrCreate), CompressionMode.Compress);
                gzout.Write(BitConverter.GetBytes(0xebabefac), 0, 4);
                gzout.Write(BitConverter.GetBytes(width), 0, 2);
                gzout.Write(BitConverter.GetBytes(height), 0, 2);
                gzout.Write(BitConverter.GetBytes(depth), 0, 2);
                gzout.Write(BitConverter.GetBytes(spawnx), 0, 2);
                gzout.Write(BitConverter.GetBytes(spawny), 0, 2);
                gzout.Write(BitConverter.GetBytes(spawnz), 0, 2);
                gzout.WriteByte(this.srotx);
                gzout.WriteByte(this.sroty);
                gzout.Write(saveblocks, 0, saveblocks.Length);

                //gzout.BaseStream.Close();
                gzout.Close();

                StreamWriter mBlocksOut = new StreamWriter(File.Open("maps/" + name + ".mb", FileMode.Create));
                foreach (KeyValuePair<int, string> mb in this.messageBlocks)
                {
                    mBlocksOut.WriteLine(mb.Key + " " + mb.Value);
                }
                mBlocksOut.Close();

                StreamWriter tBlocksOut = new StreamWriter(File.Open("maps/" + name + ".tb", FileMode.Create));
                foreach (KeyValuePair<int, int> tb in this.teleportBlocks)
                {
                    tBlocksOut.WriteLine(tb.Key + " " + tb.Value);
                }
                tBlocksOut.Close();

                Program.server.logger.log("Level \"" + this.name + "\" saved");
                lastSaveTime = DateTime.Now;
                blockChanged = false;
            }
            catch (Exception e)
            {
                Program.server.logger.log("Error occurred while saving map", Logger.LogType.Error);
                Program.server.logger.log(e);
            }
        }

        public void Backup()
        {
            if (((TimeSpan)(DateTime.Now - lastSaveTime)).TotalSeconds > 600) return; //Don't back up if the map hasn't changed in 10 minutes
            try
            {
                string name = "backups/" + this.name + "/" + DateTime.Now.ToFileTime().ToString();
                string filename = name + ".umw";
                byte[] saveblocks = new byte[blocks.Length];
                blocks.CopyTo(saveblocks, 0);
                for (int i = 0; i < saveblocks.Length; i++)
                {
                    switch (saveblocks[i])
                    {
                        case Blocks.unflood:
                            saveblocks[i] = Blocks.ConvertType(saveblocks[i]);
                            break;
                        default:
                            break;
                    }
                }

                if (!Directory.Exists("backups/" + this.name)) Directory.CreateDirectory("backups/" + this.name);

                GZipStream gzout = new GZipStream(new FileStream("maps/" + filename, FileMode.OpenOrCreate), CompressionMode.Compress);
                gzout.Write(BitConverter.GetBytes(0xebabefac), 0, 4);
                gzout.Write(BitConverter.GetBytes(width), 0, 2);
                gzout.Write(BitConverter.GetBytes(height), 0, 2);
                gzout.Write(BitConverter.GetBytes(depth), 0, 2);
                gzout.Write(BitConverter.GetBytes(spawnx), 0, 2);
                gzout.Write(BitConverter.GetBytes(spawny), 0, 2);
                gzout.Write(BitConverter.GetBytes(spawnz), 0, 2);
                gzout.WriteByte(this.srotx);
                gzout.WriteByte(this.sroty);
                gzout.Write(saveblocks, 0, saveblocks.Length);

                //gzout.BaseStream.Close();
                gzout.Close();

                StreamWriter mBlocksOut = new StreamWriter(File.Open("maps/" + name + ".mb", FileMode.Create));
                foreach (KeyValuePair<int, string> mb in this.messageBlocks)
                {
                    mBlocksOut.WriteLine(mb.Key + " " + mb.Value);
                }
                mBlocksOut.Close();

                StreamWriter tBlocksOut = new StreamWriter(File.Open("maps/" + name + ".tb", FileMode.Create));
                foreach (KeyValuePair<int, int> tb in this.teleportBlocks)
                {
                    tBlocksOut.WriteLine(tb.Key + " " + tb.Value);
                }
                tBlocksOut.Close();

                Program.server.logger.log("Level \"" + this.name + "\" backed up");
            }
            catch (Exception e)
            {
                Program.server.logger.log("Error occurred while backing up map", Logger.LogType.Error);
                Program.server.logger.log(e);
            }
        }

        public bool LoadOld(string filename)
        {
            try
            {
                this.filename = filename;
                GZipStream gzin = new GZipStream(new FileStream("maps/" + filename, FileMode.Open), CompressionMode.Decompress);

                byte[] magicnumbytes = new byte[4];
                gzin.Read(magicnumbytes, 0, 4);
                if (!(BitConverter.ToUInt32(magicnumbytes, 0) == 0xebabefac))
                {
                    Program.server.logger.log("Wrong magic number in level file: " + BitConverter.ToUInt32(magicnumbytes, 0), Logger.LogType.Error);
                    return false;
                }

                byte[] leveldimensions = new byte[6];
                gzin.Read(leveldimensions, 0, 6);
                this.width = BitConverter.ToInt16(leveldimensions, 0);
                this.height = BitConverter.ToInt16(leveldimensions, 2);
                this.depth = BitConverter.ToInt16(leveldimensions, 4);

                byte[] spawnpoint = new byte[6];
                gzin.Read(spawnpoint, 0, 6);
                this.spawnx = BitConverter.ToInt16(spawnpoint, 0);
                this.spawny = BitConverter.ToInt16(spawnpoint, 2);
                this.spawnz = BitConverter.ToInt16(spawnpoint, 4);

                this.srotx = 0;
                this.sroty = 0;

                this.blocks = new byte[this.width * this.height * this.depth];
                gzin.Read(blocks, 0, this.width * this.height * this.depth);

                //gzin.BaseStream.Close();
                gzin.Close();

                this.name = filename.Substring(0, filename.IndexOf(".umo"));
                this.filename = this.name + ".umw";

                this.messageBlocks = new Dictionary<int, string>();

                Program.server.logger.log("Loaded world from " + filename);
                return true;
            }
            catch(Exception e)
            {
                Program.server.logger.log("Error occurred while loading map", Logger.LogType.Error);
                Program.server.logger.log(e);
                return false;
            }
        }

        public bool Load(string filename)
        {
            try
            {
                this.filename = filename;
                GZipStream gzin = new GZipStream(new FileStream("maps/" + filename, FileMode.Open), CompressionMode.Decompress);

                byte[] magicnumbytes = new byte[4];
                gzin.Read(magicnumbytes, 0, 4);
                if (!(BitConverter.ToUInt32(magicnumbytes, 0) == 0xebabefac))
                {
                    Program.server.logger.log("Wrong magic number in level file: " + BitConverter.ToUInt32(magicnumbytes, 0), Logger.LogType.Error);
                    return false;
                }

                byte[] leveldimensions = new byte[6];
                gzin.Read(leveldimensions, 0, 6);
                this.width = BitConverter.ToInt16(leveldimensions, 0);
                this.height = BitConverter.ToInt16(leveldimensions, 2);
                this.depth = BitConverter.ToInt16(leveldimensions, 4);

                byte[] spawnpoint = new byte[6];
                gzin.Read(spawnpoint, 0, 6);
                this.spawnx = BitConverter.ToInt16(spawnpoint, 0);
                this.spawny = BitConverter.ToInt16(spawnpoint, 2);
                this.spawnz = BitConverter.ToInt16(spawnpoint, 4);

                this.srotx = (byte)gzin.ReadByte();
                this.sroty = (byte)gzin.ReadByte();

                this.blocks = new byte[this.width * this.height * this.depth];
                gzin.Read(blocks, 0, this.width * this.height * this.depth);

                //gzin.BaseStream.Close();
                gzin.Close();

                this.name = filename.Substring(0, filename.IndexOf(".umw"));
                this.messageBlocks = new Dictionary<int, string>();
                this.teleportBlocks = new Dictionary<int, int>();

                if (File.Exists("maps/" + this.name + ".mb"))
                {
                    StreamReader mBlocksIn = new StreamReader(File.Open("maps/" + this.name + ".mb", FileMode.Open));
                    string line = "";
                    while (!mBlocksIn.EndOfStream)
                    {
                        line = mBlocksIn.ReadLine();
                        if (!line.Contains(" ")) break;
                        int index = Int32.Parse(line.Substring(0, line.IndexOf(" ")));
                        string msg = line.Substring(line.IndexOf(" ") + 1);
                        this.messageBlocks.Add(index, msg);
                    }
                    mBlocksIn.Close();
                }

                if (File.Exists("maps/" + this.name + ".tb"))
                {
                    StreamReader tBlocksIn = new StreamReader(File.Open("maps/" + this.name + ".tb", FileMode.Open));
                    string line = "";
                    while (!tBlocksIn.EndOfStream)
                    {
                        line = tBlocksIn.ReadLine();
                        if (!line.Contains(" ")) break;
                        int index = Int32.Parse(line.Substring(0, line.IndexOf(" ")));
                        int value = Int32.Parse(line.Substring(line.IndexOf(" ") + 1));
                        this.teleportBlocks.Add(index, value);
                    }
                    tBlocksIn.Close();
                }

                Program.server.logger.log("Loaded world from " + filename);
                return true;
            }
            catch (Exception e)
            {
                Program.server.logger.log("Error occurred while loading map", Logger.LogType.Error);
                Program.server.logger.log(e);
                return false;
            }
        } 

    }
}
