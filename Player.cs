﻿/**
 * uBuilder - A lightweight custom Minecraft Classic server written in C#
 * Copyright 2010 Calvin "calzoneman" Montgomery
 * 
 * Licensed under the Creative Commons Attribution-ShareAlike 3.0 Unported License
 * (see http://creativecommons.org/licenses/by-sa/3.0/, or LICENSE.txt for a full license
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;



namespace uBuilder
{
    public class Player
    {
        public short x, y, z;
        public byte rotx, roty;
        public string username;
        public string prefix = "";
        public string ip;
        public bool loggedIn = false;
        public int loginTmr = 0;
        public byte id;
        public ushort rank = 0x01; //Guest
        public bool disconnected = false;
        //public Dictionary<string, object> buildMeta = new Dictionary<string, object>();
        public Bindings binding = Bindings.None;
        public bool painting = false, cuboiding = false;
        public byte holding = 1;
        public CuboidParameters cParams;
        public TeleportBlockParams tParams;
        public ShapeArgs sArgs;
        public short[] lastTeleport = new short[] { 0, 0, 0 };
        public int[] lastFlyPos = new int[] { 0, 0, 0 };
        public string messageBlockText = "";
        public Block[] copyClipboard = null;

        public World world;

        object queueLock = new object();

        public static Dictionary<string, char> specialChars;

        public TcpClient plyClient;
        public BinaryReader inputReader;
        public BinaryWriter outputWriter;
        public Queue<Packet> outQueue;
        public Queue<Packet> blockQueue;

        public Thread IOThread;

        //Events
        //Login
        public delegate void LoginHandler(Player p);
        public event LoginHandler OnLogin = null;
        public void ResetLoginHandler() { OnLogin = null; }

        //Blockchange
        public delegate void BlockHandler(Player p, int x, int y, int z, byte type);
        public event BlockHandler OnBlockchange = null;
        public void ResetBlockHandler() { OnBlockchange = null; }

        //Movement
        public delegate void PositionChangeHandler(Player p, short[] oldPos, byte[] oldRot, short[] newPos, byte[] newRot);
        public event PositionChangeHandler OnMovement = null;
        public void ResetPositionChangeHandler() { OnMovement = null; }

        //Chat
        public delegate void ChatHandler(Player p, string msg);
        public event ChatHandler OnChat = null;
        public void ResetChatHandler() { OnChat = null; }

        public Player(TcpClient client, string ip, byte id)
        {
            try
            {
                this.username = "player";
                this.plyClient = client;
                this.x = 0;
                this.y = 0;
                this.z = 0;
                this.rotx = 0;
                this.roty = 0;
                this.prefix = "";
                this.id = id;
                this.ip = ip;

                this.world = null;

                this.outQueue = new Queue<Packet>();
                this.blockQueue = new Queue<Packet>();
                this.IOThread = new Thread(PlayerIO);
                this.outputWriter = new BinaryWriter(client.GetStream());
                this.inputReader = new BinaryReader(client.GetStream());

                this.IOThread.IsBackground = true;
                this.IOThread.Start();
            }
            catch
            {
            }
        }

        public void PlayerIO()
        {
            try
            {
                Login();
            }
            catch (IOException) { Disconnect(true); }
            catch (SocketException) { Disconnect(true); }
            catch (ObjectDisposedException) { Disconnect(true); }
            catch (Exception e) { Program.server.logger.log(e); Disconnect(true); }

            DateTime pingTime = DateTime.Now;
            while (!disconnected)
            {
                try
                {
                    //Send whatever remains in the queue
                    lock (queueLock)
                    {
                        //Process generic packets
                        while (outQueue.Count > 0)
                        {
                            Packet p = outQueue.Dequeue();
                            if (this.world == null && !"01234".Contains(p.raw[0].ToString())) //Process all login/map packets first
                            {
                                outQueue.Enqueue(p);
                            }
                            else 
                            {
                                this.outputWriter.Write(p.raw);
                            }
                        }
                        //Process blockchanges (separation should reduce lag)
                        if (this.world != null)
                        {
                            while (blockQueue.Count > 0)
                            {
                                Packet p = blockQueue.Dequeue();
                                this.outputWriter.Write(p.raw);
                            }
                        }
                    }
                    if (((TimeSpan)(DateTime.Now - pingTime)).TotalSeconds > 2)
                    {
                        this.outputWriter.Write((byte)ServerPacket.Ping); //Ping
                        pingTime = DateTime.Now;
                    }

                    //Accept input
                    while (plyClient.GetStream().DataAvailable)
                    {
                        byte opcode = this.inputReader.ReadByte();
                        switch ((ClientPacket)opcode)
                        {
                            case ClientPacket.Login:
                                if (loggedIn)
                                {
                                    Program.server.logger.log("Player " + username + " has already logged in!", Logger.LogType.Warning);
                                    Kick("Already logged in", false);
                                }
                                break;

                            case ClientPacket.Blockchange:
                                PlayerBlockchange();
                                break;
                            case ClientPacket.MoveRotate:
                                PositionChange();
                                break;
                            case ClientPacket.Message:
                                PlayerMessage();
                                break;
                            default:
                                Program.server.logger.log("Unhandled packet type \"" + opcode + "\"", Logger.LogType.Warning);
                                Kick("Unknown packet type", false);
                                break;
                        }
                    }
                    //Clean up
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    Thread.Sleep(10);

                }

                catch (IOException) { Disconnect(false); }
                catch (SocketException) { Disconnect(false); }
                catch (ObjectDisposedException) { Disconnect(false); }
                catch (Exception e) { Program.server.logger.log(e); Disconnect(false); }
                                

            }
        }

        public string GetFormattedName()
        {
            return Rank.GetColor(this.rank) + this.prefix + this.username;
        }


        #region Received Data
        public void Login()
        {
            byte opLogin = this.inputReader.ReadByte();
            if (opLogin != (byte)ClientPacket.Login)
            {
                Program.server.logger.log("Wrong login opcode received from " + ip, Logger.LogType.Warning);
                Kick("Wrong Login Opcode", true);
                return;
            }
            byte plyProtocol = this.inputReader.ReadByte();
            if (plyProtocol != Protocol.version)  //Shouldn't happen
            {
                Program.server.logger.log("Wrong protocol version received from " + ip, Logger.LogType.Warning);
                Kick("Wrong Protocol Version", true);
                return;
            }
            //Read username
            this.username = Encoding.ASCII.GetString(this.inputReader.ReadBytes(64)).Trim();

            //Verify the name
            if (Program.server.verify_names)
            {
                string mppass = Encoding.ASCII.GetString(this.inputReader.ReadBytes(64)).Trim();
                while (mppass.Length < 32) { mppass = "0" + mppass; }
                MD5 hasher = new MD5CryptoServiceProvider();
                byte[] cmpHash = hasher.ComputeHash(Encoding.ASCII.GetBytes(Program.server.salt + username));
                for (int i = 0; i < 16; i += 2)
                {
                    if (mppass[i] + "" + mppass[i + 1] != cmpHash[i / 2].ToString("x2"))
                    {
                        Kick("Name verification failed!", true);
                    }
                }
            }

            //Unused byte
            this.inputReader.ReadByte();

            if (Program.server.ipbanned.Contains(ip))
            {
                Kick("You're IP Banned!", true);
                return;
            }

            //Check Rank
            if (Program.server.playerRanksDict.ContainsKey(username.ToLower()))
            {
                this.rank = Program.server.playerRanksDict[username.ToLower()];
            }
            else
            {
                this.rank = Rank.RankLevel("guest");
                Program.server.saveRanks();
            }

            if (rank == Rank.RankLevel("none"))
            {
                Kick("You're banned!", true);
                return;
            }

            //Send a response
            this.outputWriter.Write((byte)ServerPacket.Login);
            this.outputWriter.Write((byte)Protocol.version); // Protocol version
            this.outputWriter.Write(Encoding.ASCII.GetBytes(Program.server.serverName.PadRight(64).Substring(0, 64))); // name
            this.outputWriter.Write(Encoding.ASCII.GetBytes(Program.server.motd.PadRight(64).Substring(0, 64))); //motd

            if (rank >= Rank.RankLevel("operator")) { this.outputWriter.Write((byte)0x64); } //Can break adminium
            else { this.outputWriter.Write((byte)0x00); } //Cannot break adminium

            Program.server.logger.log(ip + " logged in as " + username);

            //Find an empty slot for them
            bool emptySlot = false;
            for (int i = 0; i < Program.server.playerlist.Length - 1; i++)
            {
                if (Program.server.playerlist[i] == null)
                {
                    emptySlot = true;
                    break;
                }
            }
            if (!emptySlot) //Server is full :(
            {
                Kick("Server is full!", true);
                return;
            }

            //We are logged in now
            loggedIn = true;
            Program.server.plyCount++;

            if (Program.server.accounts.ContainsKey(username.ToLower()))
            {
                Program.server.accounts[username.ToLower()].Visit();
                Program.server.accounts[username.ToLower()].SetIP(ip);
            }
            else
            {
                Program.server.accounts.Add(username.ToLower(), new Account(this));
                Program.server.SavePlayerStats();
            }

            //Init any player-specific plugins
            //ExamplePlugins.Init(this);
            MessageBlockCheck.Init(this);
            TeleportBlockCheck.Init(this);
            //OnMovement += new PositionChangeHandler(FlyCommand.FlyMove);

            //If they are ranked operator or admin, give them a snazzy prefix
            if (rank >= Rank.RankLevel("operator")) { prefix = "+"; }
            if (rank >= Rank.RankLevel("owner")) { prefix = "@"; }

            //Send the map
            this.SendPacket(new Packet(new byte[1] { (byte)ServerPacket.MapBegin }));
            SendMap(Program.server.world);

            //Announce the player's arrival
            string loginMessage = Rank.GetColor(rank).ToString();
            if(!prefix.Equals(""))
            {
                loginMessage += prefix;
            }
            loginMessage += username + "&e joined the game";
            GlobalMessage(loginMessage);
            if (this.OnLogin != null) //Call the OnLogin Event
            {
                OnLogin(this);
            }
            
        }

        public void PrintPlayerlist()
        {
            for(int i = 0; i < Program.server.playerlist.Length; i++)
            {
                string name = "";
                if(Program.server.playerlist[i] == null)
                {
                    name = "null";
                }
                else
                {
                    name = Program.server.playerlist[i].username;
                }
                Console.WriteLine(i + "|" + name);
            }
        }



        public void PositionChange()
        {
            short[] oldPos = new short[3] { x, y, z };
            byte[] oldRot = new byte[2] { rotx, roty };
            this.inputReader.ReadByte();
            this.x = IPAddress.NetworkToHostOrder(this.inputReader.ReadInt16());
            this.y = IPAddress.NetworkToHostOrder(this.inputReader.ReadInt16());
            this.z = IPAddress.NetworkToHostOrder(this.inputReader.ReadInt16());
            this.rotx = this.inputReader.ReadByte();
            this.roty = this.inputReader.ReadByte();
            foreach (Player pl in Program.server.playerlist)
            {
                if (pl != null && pl.loggedIn && pl != this)
                {
                    pl.SendPlayerPositionChange(this);
                }
            }

            if (this.OnMovement != null)
            {
                OnMovement(this, oldPos, oldRot, new short[] { x, y, z }, new byte[] { rotx, roty });
            }
        }

        public void PlayerMessage()
        {
            this.inputReader.ReadByte();
            string rawmsg = Encoding.ASCII.GetString(this.inputReader.ReadBytes(64)).Trim();
            rawmsg = ParseSpecialChar(rawmsg);
            
            if (OnChat != null)
            {
                OnChat(this, rawmsg);
                return;
            }
            //Test for commands
            if (!rawmsg.Trim().Equals("") && rawmsg.Trim()[0] == '/')
            {
                string cmd = "", args = "";
                if (rawmsg.Contains(" "))
                {
                    cmd = rawmsg.Trim().Substring(1, rawmsg.IndexOf(' ') - 1);
                    args = rawmsg.Trim().Substring(rawmsg.IndexOf(' ')).Trim();
                }
                else { cmd = rawmsg.Substring(1); }
                Command.HandleCommand(this, cmd, args);
                return;
            }

            //Test for PMs
            if (!rawmsg.Trim().Equals("") && rawmsg.Trim()[0] == '@' && rawmsg.Trim()[1] != '@' && rawmsg.Trim().Contains(" "))
            {
                string tname = rawmsg.Trim().Substring(1, rawmsg.IndexOf(" ") - 1);
                Player target = FindPlayer(this, tname, false);
                if (target != null)
                {
                    target.SendMessage(0x00, Rank.GetColor(this.rank) + "(" + this.prefix + this.username + ")&e " + (char)26 + "&f " + rawmsg.Substring(rawmsg.IndexOf(" ") + 1));
                    this.SendMessage(0x00, Rank.GetColor(target.rank) + "(" + target.prefix + target.username + ")&e " + (char)27 + "&f " + rawmsg.Substring(rawmsg.IndexOf(" ") + 1));
                    Program.server.accounts[username.ToLower()].messagesSent++;
                }
                return;
            }

            string message = "";
            message = Rank.GetColor(rank) + "<" + prefix + username + "> &f" + rawmsg;
            if (rank >= Rank.RankLevel("player") && !message.Contains("@@")) { message = ParseColors(message); }
            Program.server.logger.log(message, Logger.LogType.Chat);
            Program.server.accounts[username.ToLower()].messagesSent++;
            foreach (Player p in Program.server.playerlist)
            {
                if (p != null && p.loggedIn)
                {
                    p.SendMessage(id, message);
                }
            }

        }

        public void PlayerBlockchange()
        {
            short x = IPAddress.HostToNetworkOrder(this.inputReader.ReadInt16());
            short y = IPAddress.HostToNetworkOrder(this.inputReader.ReadInt16());
            short z = IPAddress.HostToNetworkOrder(this.inputReader.ReadInt16());
            byte action = this.inputReader.ReadByte();
            byte type = this.inputReader.ReadByte();
            if (this.rank == 0) { return; }

            byte mapBlock = world.GetTile(x, y, z);

            if (mapBlock == Blocks.door || mapBlock == Blocks.irondoor || mapBlock == Blocks.darkgreydoor)
            {
                if (action == 0)
                {
                    if (OnBlockchange == null)
                    {
                        Program.server.advPhysics.Queue(x, y, z, mapBlock, PhysType.Door, new object[] { Blocks.DoorOpenType(mapBlock) });
                        return;
                    }
                }
                else return;
            }


            if (action == 0)
            {
                if (!painting && !cuboiding)
                {
                    type = 0;
                }
            }
            AuthenticateAndSetBlock(x, y, z, type);
        }

        public void AuthenticateAndSetBlock(int x, int y, int z, byte type)
        {
            byte mapBlock = world.GetTile(x, y, z);
            if (rank == 0 || !loggedIn) return;

            if (type == 1 && this.binding != Bindings.None)
            {
                type = (byte)this.binding;
            }

            if (mapBlock == 7 && rank < Rank.RankLevel("operator"))
            {
                Kick("Attempted to break adminium", false);
                return;
            }
            if (type == 7 && rank < Rank.RankLevel("operator"))
            {
                Kick("Illegal tile type", false);
                return;
            }
            if ((type >= 8 && type <= 11) && rank < Rank.RankLevel("operator") && type != (byte)this.binding)
            {
                Kick("Illegal tile type", false);
                return;
            }

            if (type > 49 && type != (byte)this.binding && !Blocks.blockNames.ContainsValue(type))
            {
                Kick("Illegal tile type", false);
                return;
            }

            if (mapBlock == Blocks.teleportBlock)
            {
                if (this.OnBlockchange != (BlockHandler)TeleportBlockCommand.BlockDeleted)
                {
                    SendMessage(0xFF, "That block is a teleport block.  Use /tpdel to remove it.");
                    SendBlock((short)x, (short)y, (short)z, Blocks.tnt);
                    return;
                }
            }

            if ((mapBlock == Blocks.doorOpen || mapBlock == Blocks.irondoorOpen || mapBlock == Blocks.darkgreydoorOpen) && !(OnBlockchange != null || DrawThreadManager.Active_Thread(this) || (Bindings)binding == Bindings.Air))
            {
                SendMessage(0xFF, "That block cannot be changed.");
                SendBlock((short)x, (short)y, (short)z, Blocks.air);
                return;
            }

            if (mapBlock == Blocks.door || mapBlock == Blocks.irondoor || mapBlock == Blocks.darkgreydoor)
            {
                if (type == 0)
                {
                    if (OnBlockchange == null && !DrawThreadManager.Active_Thread(this))
                    {
                        Program.server.advPhysics.Queue(x, y, z, mapBlock, PhysType.Door, new object[] { Blocks.DoorOpenType(mapBlock) });
                        return;
                    }
                }
                else if(!(OnBlockchange != null || DrawThreadManager.Active_Thread(this))) return;
            }

            if (type != 0 && type != mapBlock)
            {
                Program.server.accounts[username.ToLower()].PlaceBlock();
            }
            else if(type != mapBlock)
            {
                Program.server.accounts[username.ToLower()].DeleteBlock();
            }

            if (this.OnBlockchange != null)
            {
                OnBlockchange(this, x, y, z, type);
                return;
            }

            if (type != mapBlock)
            {
                if (!world.SetTile(x, y, z, type)) SendBlock((short)x, (short)y, (short)z, mapBlock);
            }
        }

        #endregion

        #region Sending
        //Marked virtual so ConsolePlayer can override it
        public virtual void SendMessage(byte pid, string message)
        {
            try
            {
                foreach (string line in SplitLines(message))
                {
                    if (!loggedIn) { return; }
                    Packet msgPacket = new Packet(66);
                    msgPacket.Append((byte)ServerPacket.Message);
                    msgPacket.Append(pid);
                    msgPacket.Append(Sanitize(line));
                    this.SendPacket(msgPacket);
                }
            }

            catch (IOException) { }
            catch (SocketException) { }
            catch (Exception e) { Program.server.logger.log(e); }
        }

        public void SendBlock(short x, short y, short z, byte type)
        {
            Packet block = new Packet(8);
            block.Append((byte)ServerPacket.Blockchange);
            block.Append(x);
            block.Append(y);
            block.Append(z);
            block.Append(Blocks.ConvertType(type));
            this.SendPacket(block);
        }

        public void Kick(string reason, bool silent)  //Disconnect someone
        {
            try
            {
                if (!loggedIn) { silent = true; }
                if (!this.plyClient.Connected) //Oops
                {
                    Program.server.logger.log("Player " + username + " has already disconnected.", Logger.LogType.Warning);
                    return;
                }
                //Send kick (0x0e + kick message)
                this.outputWriter.Write((byte)ServerPacket.Kick);
                this.outputWriter.Write(reason);
                
                this.plyClient.Close();
                Program.server.logger.log("Player " + username + " kicked (" + reason + ")");
                if (!silent)
                {
                    GlobalMessage("Player " + GetFormattedName() + "&e kicked (" + reason + ")");
                }
                Disconnect(silent);
            }
            catch
            {
                Disconnect(true);
            }
        }

        /*public void SendMap(ref byte[] leveldata, short width, short height, short depth)
        {
            try
            {
                byte[] buffer = new byte[leveldata.Length + 4];
                BitConverter.GetBytes(IPAddress.HostToNetworkOrder(leveldata.Length)).CopyTo(buffer, 0);
                for (int i = 0; i < leveldata.Length; ++i)
                {
                    buffer[4 + i] = leveldata[i];
                }
                buffer = GZip(buffer);
                int number = (int)Math.Ceiling(((double)buffer.Length) / 1024);
                for (int i = 1; buffer.Length > 0; ++i)
                {
                    Packet chunk = new Packet(1028);
                    short length = (short)Math.Min(buffer.Length, 1024);
                    chunk.Append((byte)ServerPacket.MapChunk);
                    chunk.Append(length);
                    chunk.Append(byteArraySlice(ref buffer, 0, length));
                    for (short j = length; j < 1024; j++)
                    {
                        chunk.Append((byte)0);
                    }
                    byte[] tempbuffer = new byte[buffer.Length - length];
                    Buffer.BlockCopy(buffer, length, tempbuffer, 0, buffer.Length - length);
                    buffer = tempbuffer;
                    chunk.Append((byte)((i * 100.0) / number));
                    this.SendPacket(chunk);
                    System.Threading.Thread.Sleep(1);
                }
                Packet mapFinal = new Packet(7);
                mapFinal.Append((byte)ServerPacket.MapFinal);
                mapFinal.Append((short)width);
                mapFinal.Append((short)depth);
                mapFinal.Append((short)height);
                this.SendPacket(mapFinal);

                //Spawn player
                this.SpawnPlayer(this, true);
                this.SendSpawn(new short[3] { 8 * 32 + 16, 64, 8 * 32 + 16 }, new byte[2] { 0, 0 });

                //Spawn other players
                foreach (Player p in Program.server.playerlist)
                {
                    if (p != null && p.loggedIn && p != this)
                    {
                        this.SpawnPlayer(p, false);
                    }
                }

                //Spawn self
                GlobalSpawnPlayer(this);
            }
            catch (IOException) { }
            catch (SocketException) { }
            catch (Exception e) { Program.server.logger.log(e); }

        } */

        public void SendMap(World w)
        {
            try
            {
                byte[] buffer = new byte[w.blocks.Length + 4];
                BitConverter.GetBytes(IPAddress.HostToNetworkOrder(w.blocks.Length)).CopyTo(buffer, 0);
                for (int i = 0; i < w.blocks.Length; ++i)
                {
                    buffer[4 + i] = Blocks.ConvertType(w.blocks[i]);
                }
                buffer = GZip(buffer);
                int number = (int)Math.Ceiling(((double)buffer.Length) / 1024);
                for (int i = 1; buffer.Length > 0; ++i)
                {
                    Packet chunk = new Packet(1028);
                    short length = (short)Math.Min(buffer.Length, 1024);
                    chunk.Append((byte)ServerPacket.MapChunk);
                    chunk.Append(length);
                    chunk.Append(byteArraySlice(ref buffer, 0, length));
                    for (short j = length; j < 1024; j++)
                    {
                        chunk.Append((byte)0);
                    }
                    byte[] tempbuffer = new byte[buffer.Length - length];
                    Buffer.BlockCopy(buffer, length, tempbuffer, 0, buffer.Length - length);
                    buffer = tempbuffer;
                    chunk.Append((byte)((i * 100.0) / number));
                    this.SendPacket(chunk);
                    System.Threading.Thread.Sleep(1);
                }
                Packet mapFinal = new Packet(7);
                mapFinal.Append((byte)ServerPacket.MapFinal);
                mapFinal.Append(w.width);
                mapFinal.Append(w.height);
                mapFinal.Append(w.depth);
                this.SendPacket(mapFinal);

                //Spawn player (convert map coordinates to player coordinates and set player pos)
                this.x = (short)(w.spawnx << 5);
                this.y = (short)(w.spawny << 5);
                this.z = (short)(w.spawnz << 5);
                this.rotx = w.srotx;
                this.roty = w.sroty;
                this.SpawnPlayer(this, true);

                //Spawn other players
                foreach (Player p in Program.server.playerlist)
                {
                    if (p != null && p.loggedIn && p != this)
                    {
                        this.SpawnPlayer(p, false);
                    }
                }

                //Spawn self
                GlobalSpawnPlayer(this);

                this.world = w;
            }
            catch (IOException) { }
            catch (SocketException) { }
            catch (Exception e) { Program.server.logger.log(e); }

        }

        public void SpawnPlayer(Player p, bool self)
        {
            try
            {
                Packet spawn = new Packet(74);
                spawn.Append((byte)ServerPacket.SpawnEntity);
                if (self) { spawn.Append((byte)255); }
                else { spawn.Append(p.id); }

                spawn.Append(Rank.GetColor(p.rank) + p.username); //username
                spawn.Append((short)p.x); //x position
                spawn.Append((short)p.y); //y position
                spawn.Append((short)p.z); //z position

                spawn.Append(p.rotx); //x rotation
                spawn.Append(p.roty); //y rotation
                this.SendPacket(spawn);
            }

            catch (IOException) { }
            catch (SocketException) { }
            catch (Exception e) { Program.server.logger.log(e); }
        }

        public void SendSpawn(short[] pos, byte[] rot)
        {
            try
            {
                Packet spawn = new Packet(10);
                //Now move+rotate (teleport)
                spawn.Append((byte)ServerPacket.MoveRotate); //Move+Rotate
                spawn.Append((byte)255); // Self

                spawn.Append(pos[0]); //x position
                spawn.Append(pos[1]); //y position
                spawn.Append(pos[2]); //z position

                spawn.Append(rot[0]); //x rotation
                spawn.Append(rot[1]); //y rotation
                this.SendPacket(spawn);
            }

            catch (IOException) { }
            catch (SocketException) { }
            catch (Exception e) { Program.server.logger.log(e); }
        }

        public void SendPlayerPositionChange(Player p)
        {
            try
            {
                Packet posChange = new Packet(10);
                posChange.Append((byte)ServerPacket.MoveRotate);
                posChange.Append(p.id);

                posChange.Append(p.x);
                posChange.Append(p.y);
                posChange.Append(p.z);

                posChange.Append(p.rotx);
                posChange.Append(p.roty);
                this.SendPacket(posChange);
            }

            catch (IOException) { }
            catch (SocketException) { }
            catch (Exception e) { Program.server.logger.log(e); }
        }

        public void SendPacket(Packet p)
        {
            lock (queueLock)
            {
                if (p.raw[0] == (byte)ServerPacket.Blockchange)
                {
                    this.blockQueue.Enqueue(p);
                }
                else
                {
                    this.outQueue.Enqueue(p);
                }
            }
        }

        #endregion

        #region Global Stuff

        public static void GlobalBlockchange(short x, short y, short z, byte type)
        {
            Packet blockPacket = new Packet(8);
            blockPacket.Append((byte)ServerPacket.Blockchange);
            blockPacket.Append(x);
            blockPacket.Append(y);
            blockPacket.Append(z);
            blockPacket.Append(type);
            foreach (Player p in Program.server.playerlist)
            {
                try
                {
                    if (p != null && p.loggedIn && !p.disconnected)
                    {
                        p.SendPacket(blockPacket);
                    }
                }
                catch
                {
                    p.Disconnect(false);
                }
            }
        }

        public static void GlobalMessage(string message)
        {
            message = "[ " + message + " ]";
            foreach (string line in SplitLines(message))
            {
                foreach (Player p in Program.server.playerlist)
                {
                    try
                    {
                        if (p != null && p.loggedIn && !p.disconnected)
                        {
                            p.SendMessage(0xFF, line);
                        }
                    }
                    catch
                    {
                        Program.server.logger.log("Failed to send Global Message to " + p.username, Logger.LogType.Warning);
                        p.Disconnect(false);
                    }
                }
            }
            Program.server.logger.log("(Global) " + message, Logger.LogType.Chat);
        }

        public static void GlobalSpawnPlayer(Player p)
        {
            foreach (Player pl in Program.server.playerlist)
            {
                try
                {
                    if (pl != null && pl.loggedIn && pl != p)
                    {
                        pl.SpawnPlayer(p, false);
                    }
                }
                catch { }
            }
        }

        #endregion

        #region Disconnecting
        public void Disconnect(bool silent)
        {
            try
            {
                if (this.disconnected) { return; }
                this.loggedIn = false;

                if (!silent)
                {
                    GlobalMessage(GetFormattedName() + "&e disconnected.");
                    foreach (Player pl in Program.server.playerlist)
                    {
                        if (pl != null && pl.loggedIn)
                        {
                            pl.outputWriter.Write((byte)ServerPacket.PlayerDie);
                            pl.outputWriter.Write(this.id);
                        }
                    }
                }
                Program.server.logger.log(username + "(" + ip + ") disconnected.");
                Program.server.playerlist[id] = null;
                Program.server.plyCount--;
                if (this.plyClient.Connected && !this.disconnected) { this.plyClient.Close(); }
                this.disconnected = true;
            }
            catch
            {
            }
        }
        #endregion

        #region Data Handlers

        public static string Sanitize(string input)
        {
            input = input.Trim((char)0x20);
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                if ((int)input[i] >= 128)
                {
                    output.Append(' ');
                }
                else
                {
                    output.Append(input[i]);
                    
                }
            }

            if ((int)(input[input.Length - 1]) < 0x20)
            {
                output.Append((char)39);
            }
            return output.ToString();
        }

        public static string ParseSpecialChar(string input)
        {
            if (input.Contains("@@")) { return input; }

            foreach (KeyValuePair<string, char> rule in specialChars)
            {
                input = input.Replace(rule.Key, String.Empty + rule.Value);
            }

            /*while (input.Contains(@"\#"))
            {
                int index = input.IndexOf(@"\#") + 2;
                if (index < input.Length)
                {
                    try
                    {
                        int num = Int32.Parse(input.Substring(index, input.IndexOf(' ', index) - index));
                        if (num >= 128) { break; }
                        input = input.Remove(index - 2, 2 + num.ToString().Length);
                        input = input.Insert(index - 2, "" + (char)num);
                    }
                    catch
                    {
                        input = input.Remove(index - 2, 3);
                    }
                }
                else
                {
                    input = input.Remove(index - 2, 2);
                }
                
            }*/

            return input;
        }

        public static string ParseColors(string message)
        {
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < message.Length; i++)
            {
                char ch = message[i];
                if (ch == '%' && i + 1 < message.Length && "0123456789abcdef".Contains(message[i + 1].ToString()) && i + 2 < message.Length)
                {
                    ch = '&';
                }
                output.Append(ch);
            }
            return output.ToString();
        }
        
        public static List<string> SplitLines(string message)
        {
            List<string> lines = new List<string>();
            message = Regex.Replace(message, @"(&[0-9a-f])+(&[0-9a-f])", "$2");
            message = Regex.Replace(message, @"(&[0-9a-f])+$", "");
            int limit = 64; string color = "";
            while (message.Length > 0)
            {
                if (lines.Count > 0 && message.Trim()[0] == '&') { message = "> " + message.Trim(); }
                else if (lines.Count > 0) { message = "> " + color + message.Trim(); }
                if (message.Length <= limit) { lines.Add(message); break; }
                for (int i = limit - 1; i > limit - 9; --i)
                {
                    if (message[i] == ' ') 
					{
						lines.Add(message.Substring(0, i)); goto Next; 
					}
                } 
				lines.Add(message.Substring(0, limit));
			Next: message = message.Substring(lines[lines.Count - 1].Length);
				if (lines.Count == 1)
				{
					limit = 60;
				}
                int index = lines[lines.Count - 1].LastIndexOf('&');
				if (index != -1)
				{
					if (index < lines[lines.Count - 1].Length - 1)
					{
						char next = lines[lines.Count - 1][index + 1];
						if ("0123456789abcdef".IndexOf(next) != -1) { color = "&" + next; }
						if (index == lines[lines.Count - 1].Length - 1)
						{
							lines[lines.Count - 1] = lines[lines.Count - 1].
								Substring(0, lines[lines.Count - 1].Length - 2);
						}
					}
					else if (message.Length != 0)
					{
						char next = message[0];
						if ("0123456789abcdef".IndexOf(next) != -1)
						{
							color = "&" + next;
						}
						lines[lines.Count - 1] = lines[lines.Count - 1].
							Substring(0, lines[lines.Count - 1].Length - 1);
						message = message.Substring(1);
					}
				}
            } return lines;
        }

        public static byte[] GZip(byte[] bytes)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            GZipStream gs = new GZipStream(ms, CompressionMode.Compress, true);
            gs.Write(bytes, 0, bytes.Length); 
            gs.Close();
            ms.Position = 0; 
            bytes = new byte[ms.Length];
            ms.Read(bytes, 0, (int)ms.Length); 
            ms.Close();
            return bytes;
        }

        public static byte[] byteArraySlice(ref byte[] b, int start, int length)  //Get a piece of a byte[]
        {
            byte[] ret = new byte[length];
            for (int i = start; i < start + length; i++)
            {
                ret[i - start] = b[i];
            }
            return ret;
        }
        #endregion

        #region Playerlist stuff
        public static Player FindPlayer(Player from, string name, bool autoComplete)
        {
            List<Player> possible = new List<Player>();
            foreach (Player pl in Program.server.playerlist)
            {
                if (pl != null && pl.loggedIn && pl.username.ToLower().Equals(name.ToLower()))
                {
                    return pl;
                }
                else if (pl != null && pl.loggedIn && pl.username.Length > name.Length && pl.username.Substring(0, name.Length).ToLower().Equals(name.ToLower()))
                {
                    from.SendMessage(0xFF, "-> " + pl.GetFormattedName());
                    possible.Add(pl);
                }
            }
            if (possible.Count == 0)
            {
                from.SendMessage(0xFF, "Unable to find \"" + name + "\"");
                return null;
            }
            if (possible.Count == 1) { return possible[0]; }
            if (possible.Count > 1 && autoComplete) { return possible[0]; }

            if (!autoComplete) { from.SendMessage(0xFF, "Autocomplete is disabled for this command"); }
            return null;
        }
        #endregion

    }
}