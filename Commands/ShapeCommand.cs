using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace uBuilder
{
    public class ShapeCommand
    {
        #region Command Processors
        public static void Circle(Player p, string message)
        {
            if (message.Trim().Equals(""))
            {
                Help(p, "circle");
                return;
            }

            //Syntax: /circle radius hollow/solid [type]
            string[] args = message.Trim().Split(' ');
            if (args.Length < 2 || args.Length > 3) { Help(p, "circle"); return; }

            //Parse the radius
            short radius = 0;
            try
            {
                radius = Int16.Parse(args[0]);
            }
            catch
            {
                p.SendMessage(0xFF, "Unable to parse `" + args[0] + "` as a radius");
                return;
            }
            p.sArgs.radius = radius;

            //Is it hollow or solid?
            switch (args[1].ToLower())
            {
                case "h":
                case "hollow":
                    p.sArgs.solid = false;
                    break;
                case "s":
                case "solid":
                    p.sArgs.solid = true;
                    break;
                default:
                    p.SendMessage(0xFF, "Unknown fill type `" + args[1] + "`");
                    return;
            }

            //If a type is provided, parse it
            if (args.Length == 3)
            {
                string type = args[2].Trim().ToLower();
                if (Blocks.blockNames.ContainsKey(type))
                {
                    p.sArgs.type = Blocks.blockNames[type];
                }
                else
                {
                    p.sArgs.type = 0xFF;
                }
            }
            else
            {
                p.sArgs.type = 0xFF;
            }

            p.sArgs.mode = 0; //Circle

            p.SendMessage(0xFF, "Draw two points forming the radius of the circle");
            p.OnBlockchange += new Player.BlockHandler(RadialShapeHandler);
            p.cuboiding = true;

        }

        public static void Sphere(Player p, string message)
        {
            if (message.Trim().Equals(""))
            {
                Help(p, "sphere");
                return;
            }

            //Syntax: /circle radius hollow/solid [type]
            string[] args = message.Trim().Split(' ');
            if (args.Length < 2 || args.Length > 3) { Help(p, "sphere"); return; }

            //Parse the radius
            short radius = 0;
            try
            {
                radius = Int16.Parse(args[0]);
            }
            catch
            {
                p.SendMessage(0xFF, "Unable to parse `" + args[0] + "` as a radius");
                return;
            }
            p.sArgs.radius = radius;

            //Is it hollow or solid?
            switch (args[1].ToLower())
            {
                case "h":
                case "hollow":
                    p.sArgs.solid = false;
                    break;
                case "s":
                case "solid":
                    p.sArgs.solid = true;
                    break;
                default:
                    p.SendMessage(0xFF, "Unknown fill type `" + args[1] + "`");
                    return;
            }

            //If a type is provided, parse it
            if (args.Length == 3)
            {
                string type = args[2].Trim().ToLower();
                if (Blocks.blockNames.ContainsKey(type))
                {
                    p.sArgs.type = Blocks.blockNames[type];
                }
                else
                {
                    p.sArgs.type = 0xFF;
                }
            }
            else
            {
                p.sArgs.type = 0xFF;
            }

            p.sArgs.mode = 1; //Sphere

            p.SendMessage(0xFF, "Change a block as the center of the sphere");
            p.OnBlockchange += new Player.BlockHandler(RadialShapeHandler);
            p.cuboiding = true;

        }

        public static void Line(Player p, string message)
        {
            if (!message.Trim().Equals(""))
            {
                string bname = message.Trim();
                if (Blocks.blockNames.ContainsKey(bname))
                {
                    p.sArgs.type = Blocks.blockNames[bname];
                }
                else
                {
                    p.SendMessage(0xFF, "Unknown block type `" + bname + "`; defaulting to whatever you place first");
                    p.sArgs.type = 0xFF;
                }
            }
            else
            {
                p.SendMessage(0xFF, "No blocktype specified; defaulting to whatever you place first");
                p.sArgs.type = 0xFF;
            }
            p.SendMessage(0xFF, "Change the endpoints to draw a line.");
            p.OnBlockchange += new Player.BlockHandler(LinePointOneHandler);
            p.cuboiding = true;
        }
        #endregion

        #region Shape Handlers
        public static void RadialShapeHandler(Player p, int x, int y, int z, byte type)
        {
            byte t = type;
            if(p.sArgs.type != 0xFF) t = p.sArgs.type;
            p.cuboiding = false;
            if (p.sArgs.shapeLock) { p.SendMessage(0xFF, "Another draw is in progress, please wait until it finishes."); p.OnBlockchange -= new Player.BlockHandler(RadialShapeHandler);  return; }
            //Lock it so the player doesn't dun goofed
            p.sArgs.shapeLock = true;
            switch (p.sArgs.mode)
            {
                case 0:
                    DrawCircle(p, x, y, z, t);
                    break;
                case 1:
                    if (Math.Pow(p.sArgs.radius, 3) * 4.1887902f > 100000 && p.rank < Rank.RankLevel("owner"))
                    {
                        p.SendMessage(0xFF, "That sphere is too large for you to build.");
                        p.sArgs.shapeLock = false;
                        break;
                    }
                    DrawSphere(p, x, y, z, t);
                    break;
                default:
                    break;
            }
            p.OnBlockchange -= new Player.BlockHandler(RadialShapeHandler);
        }

        public static void LinePointOneHandler(Player p, int x, int y, int z, byte type)
        {
            byte t = type;
            if (p.sArgs.type == 0xFF) p.sArgs.type = t;

            p.sArgs.vertices = new int[3] { x, y, z };

            p.SendBlock((short)x, (short)y, (short)z, p.world.GetTile(x, y, z));
            p.OnBlockchange -= new Player.BlockHandler(LinePointOneHandler);
            p.OnBlockchange += new Player.BlockHandler(LinePointTwoHandler);
        }

        public static void LinePointTwoHandler(Player p, int x, int y, int z, byte type)
        {
            p.cuboiding = false;
            if (p.sArgs.shapeLock) { p.SendMessage(0xFF, "Another draw is in progress, please wait until it finishes."); p.OnBlockchange -= new Player.BlockHandler(LinePointTwoHandler); return; }
            p.sArgs.shapeLock = true;
            switch (p.sArgs.mode)
            {
                case 0:
                    DrawCircle(p, x, y, z, p.sArgs.type);
                    break;
                case 2:
                    DrawLine(p, x, y, z, p.sArgs.vertices[0], p.sArgs.vertices[1], p.sArgs.vertices[2], p.sArgs.type);
                    break;
                default:
                    break;
            }
            p.OnBlockchange -= new Player.BlockHandler(LinePointTwoHandler);
        }
        #endregion

        #region Shape Drawers
        public static void DrawCircle(Player p, int x, int y, int z, byte type)
        {
            /*int xo = p.sArgs.vertices[0], yo = p.sArgs.vertices[1], zo = p.sArgs.vertices[2];

            int dx = x - xo, dz = z - zo;
            short radius = (short)Math.Sqrt(dx * dx + dz * dz);

            int dy = y - yo, y_low = Math.Min(y, yo);
            float m_y = (float)dy / (4 * radius * (radius + 1) + 1);
            float ny = (float)y_low;

            float ang = (float)Math.Atan((double)dz / dx);

            bool outer_loop = false;
            if (dx > dz) outer_loop = true;*/
            short radius = p.sArgs.radius;
            Thread drawThread = new Thread((ThreadStart)delegate
                {
                    World dWorld = p.world;
                    Account user = Program.server.accounts[p.username.ToLower()];
                    /*for (int nx = xo - radius; nx < xo + radius + 1; nx++)
                    {
                        for (int nz = zo - radius; nz < zo + radius + 1; nz++)
                        {
                            if (dWorld.GetTile(nx, (int)ny, nz) != type)
                            {
                                if ((Math.Pow((xo - nx), 2) + Math.Pow((zo - nz), 2)) <= radius * radius)
                                {
                                    if ((Math.Pow((xo - nx), 2) + Math.Pow((zo - nz), 2)) <= (radius - 1) * (radius - 1) && !p.sArgs.solid)
                                    {
                                        p.AuthenticateAndSetBlock(nx, (int)ny, nz, Blocks.air);
                                        Thread.Sleep(1);
                                    }
                                    else
                                    {
                                        p.AuthenticateAndSetBlock(nx, (int)ny, nz, type);
                                        Thread.Sleep(1);
                                    }
                                }
                            }
                            if (Math.Abs(Math.Atan((double)(nz - zo) / (double)(nx - xo)) - ang) < 2) ny += m_y;
                        }
                    }*/
                    for (int nx = x - radius; nx < x + radius + 1; nx++)
                    {
                        for (int nz = z - radius; nz < z + radius + 1; nz++)
                        {
                            if (dWorld.GetTile(nx, y, nz) != type)
                            {
                                if ((Math.Pow((x - nx), 2) + Math.Pow((z - nz), 2)) <= radius * radius)
                                {
                                    if ((Math.Pow((x - nx), 2) + Math.Pow((z - nz), 2)) <= (radius - 1) * (radius - 1) && !p.sArgs.solid)
                                    {
                                        p.AuthenticateAndSetBlock(nx, y, nz, Blocks.air);
                                        Thread.Sleep(1);
                                    }
                                    else
                                    {
                                        p.AuthenticateAndSetBlock(nx, y, nz, type);
                                        Thread.Sleep(1);
                                    }
                                }
                            }
                        }
                    }
                    if (!p.sArgs.solid) p.SendBlock((short)x, (short)y, (short)z, Blocks.air);
                    p.sArgs.shapeLock = false;
                    p.SendMessage(0xFF, "Drawing Complete.");
                });
            drawThread.Start();
            DrawThreadManager.shape_threads.Add(p, drawThread);
        }

        public static void DrawSphere(Player p, int x, int y, int z, byte type)
        {
            short radius = p.sArgs.radius;
            Thread drawThread = new Thread((ThreadStart)delegate
            {
                World dWorld = p.world;
                Account user = Program.server.accounts[p.username.ToLower()];
                for (int nx = x - radius; nx < x + radius + 1; nx++)
                {
                    for (int nz = z - radius; nz < z + radius + 1; nz++)
                    {
                        for (int ny = y - radius; ny < y + radius + 1; ny++)
                        {
                            if (dWorld.GetTile(nx, ny, nz) != type)
                            {
                                if ((Math.Pow((x - nx), 2) + Math.Pow((z - nz), 2) + Math.Pow((y - ny), 2)) <= Math.Pow(radius, 2))
                                {
                                    if ((Math.Pow((x - nx), 2) + Math.Pow((z - nz), 2) + Math.Pow((y - ny), 2)) <= Math.Pow((radius - 1), 2) && !p.sArgs.solid)
                                    {
                                        p.AuthenticateAndSetBlock(nx, ny, nz, Blocks.air);
                                        Thread.Sleep(1);
                                    }
                                    else
                                    {
                                        p.AuthenticateAndSetBlock(nx, ny, nz, type);
                                        Thread.Sleep(1);
                                    }
                                }
                            }
                        }

                    }
                }
                if (!p.sArgs.solid) p.SendBlock((short)x, (short)y, (short)z, Blocks.air);
                p.sArgs.shapeLock = false;
                p.SendMessage(0xFF, "Drawing Complete.");
            });
            drawThread.Start();
            DrawThreadManager.shape_threads.Add(p, drawThread);

        }

        public static void DrawLine(Player p, int x1, int y1, int z1, int x2, int y2, int z2, byte type)
        {
            int dx = x2 - x1, dy = y2 - y1, dz = z2 - z1;

            int length = (int)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            float m_x = (float)dx / length;
            float m_y = (float)dy / length;
            float m_z = (float)dz / length;

            Thread drawThread = new Thread((ThreadStart)delegate
                {
                    int x = x1, y = y1, z = z1;
                    for (int i = 0; i < length + 1; i++)
                    {
                        x = (int)Math.Round(i * m_x + x1);
                        y = (int)Math.Round(i * m_y + y1);
                        z = (int)Math.Round(i * m_z + z1);
                        p.AuthenticateAndSetBlock(x, y, z, type);
                        //Program.server.logger.log(x + ", " + y + ", " + z + ", " + type, Logger.LogType.Debug);
                    }
                    p.sArgs.shapeLock = false;
                    p.SendMessage(0xFF, "Drawing complete.");
                });
            drawThread.Start();
            DrawThreadManager.shape_threads.Add(p, drawThread);
        }
        #endregion

        public static void Help(Player p, string cmd)
        {
            switch (cmd)
            {
                case "circle":
                    p.SendMessage(0xFF, "/circle radius fill - Creates a circle of the given radius and fill");
                    p.SendMessage(0xFF, "/circle radius fill type - Same as /circle, but overrides block type");
                    p.SendMessage(0xFF, "Valid fills:&c solid s hollow h");
                    break;
                case "sphere":
                    p.SendMessage(0xFF, "/sphere radius fill - Creates a sphere of the given radius and fill");
                    p.SendMessage(0xFF, "/sphere radius fill type - Same as /sphere, but overrides block type");
                    p.SendMessage(0xFF, "Valid fills:&c solid s hollow h");
                    break;
                case "line":
                    p.SendMessage(0xFF, "/line - Draw a line between two endpoints");
                    p.SendMessage(0xFF, "/line type - Same as /line but with the specified blocktype");
                    break;
                default:
                    break;
            }
        }

        public static void Swap(ref int i1, ref int i2)
        {
            int temp = i1;
            i1 = i2;
            i2 = temp;
        }

    }

    public struct ShapeArgs
    {
        public byte mode; // 0 = circle, 1 = sphere, 2 = line
        public byte type; // Type to place
        public bool solid;
        public short radius;
        public bool shapeLock;
        public int[] vertices;
    }

}
