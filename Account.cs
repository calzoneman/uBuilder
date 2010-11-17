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
    public class Account
    {
        public string name;
        public string lastseenip;
        public string lastjoined;

        public int visitcount;
        public int blocksCreated;
        public int blocksDestroyed;
        public int messagesSent;

        public double blockRatio;

        public Account(Player p)
        {
            this.name = p.username;
            this.lastseenip = p.ip;
            this.lastjoined = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString();
            this.visitcount = 1;
            this.blocksCreated = 0;
            this.blocksDestroyed = 0;
            this.messagesSent = 0;
            this.blockRatio = 0.0d;
        }

        public Account(string name)
        {
            this.name = name;
        }

        public void SetIP(string ip)
        {
            this.lastseenip = ip;
        }

        public void Visit()
        {
            this.visitcount++;
            this.lastjoined = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString();
        }

        public void SetBlockStats(bool type, int amt)
        {
            if (type) // True = setting created blocks
            {
                this.blocksCreated = amt;
            }
            else
            {
                this.blocksDestroyed = amt;
            }
            if (blocksDestroyed == 0) return;
            this.blockRatio = (double)this.blocksCreated / this.blocksDestroyed;
        }

        public void PlaceBlock()
        {
            this.blocksCreated++;
            if (this.blocksDestroyed == 0) return;
            this.blockRatio = (double)this.blocksCreated / this.blocksDestroyed;
        }

        public void DeleteBlock()
        {
            this.blocksDestroyed++;
            this.blockRatio = (double)this.blocksCreated / this.blocksDestroyed;
        }
    }
}
