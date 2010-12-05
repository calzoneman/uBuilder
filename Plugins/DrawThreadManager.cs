using System;
using System.Collections.Generic;
using System.Text;

namespace uBuilder
{
    public class DrawThreadManager
    {
        public static Dictionary<Player, System.Threading.Thread> cuboid_threads = new Dictionary<Player, System.Threading.Thread>();
        public static Dictionary<Player, System.Threading.Thread> shape_threads = new Dictionary<Player, System.Threading.Thread>();
        public static System.Timers.Timer threadStoppedChecker = new System.Timers.Timer(1000.0f);

        public static void Init()
        {
            threadStoppedChecker.Elapsed += new System.Timers.ElapsedEventHandler(threadStoppedChecker_Elapsed);
            threadStoppedChecker.Start();
        }

        static void threadStoppedChecker_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            List<Player> keysToRemove = new List<Player>();
            foreach (KeyValuePair<Player, System.Threading.Thread> thr in cuboid_threads)
            {
                if (!thr.Value.IsAlive)//(thr.Value.ThreadState != System.Threading.ThreadState.Running && thr.Value.ThreadState != System.Threading.ThreadState.Unstarted)
                {
                    keysToRemove.Add(thr.Key);

                }
            }

            foreach (KeyValuePair<Player, System.Threading.Thread> thr in shape_threads)
            {
                if (!thr.Value.IsAlive)//(thr.Value.ThreadState != System.Threading.ThreadState.Running && thr.Value.ThreadState != System.Threading.ThreadState.Unstarted)
                {
                    keysToRemove.Add(thr.Key);
                }
            }

            foreach (Player key in keysToRemove)
            {
                if (cuboid_threads.ContainsKey(key)) { cuboid_threads.Remove(key); }
                if (shape_threads.ContainsKey(key)) { shape_threads.Remove(key); }
            }
        }

        public static bool Active_Thread(Player p)
        {
            if (cuboid_threads.ContainsKey(p) || shape_threads.ContainsKey(p)) return true;
            return false;
        }

        public static bool Terminate_Draw(Player p, byte type)
        {
            if (type == 0) // Cuboid
            {
                if (cuboid_threads.ContainsKey(p))
                {
                    cuboid_threads[p].Abort();
                    cuboid_threads.Remove(p);
                    p.cParams.cuboidLock = false;
                    p.SendMessage(0xFF, "Cuboid aborted.");
                    return true;
                }
                else
                {
                    return false;
                }
            }

            else if (type == 1) // ShapeCommand
            {
                if (shape_threads.ContainsKey(p))
                {
                    shape_threads[p].Abort();
                    shape_threads.Remove(p);
                    p.sArgs.shapeLock = false;
                    p.SendMessage(0xFF, "Shape aborted.");
                    return true;
                }
                else
                {
                    return false;
                }
            }

            else
            {
                return false;
            }
        }
    }
}
