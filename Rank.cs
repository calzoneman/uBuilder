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
    public class Rank
    {
        public const ushort MAX_RANK = ushort.MaxValue;
        public const ushort MIN_RANK = ushort.MinValue;
        public static string RankName(ushort permissionLevel)
        {
            if(permissionLevel == 0) return "none";
            else if(permissionLevel < 16) return "guest";
            else if(permissionLevel < 128) return "player";
            else if(permissionLevel < 255) return "operator";
            else if(permissionLevel >= 255) return "owner";
            else return "none";
        }

        public static byte RankLevel(string name)
        {
            switch (name)
            {
                case "none":
                    return 0;
                case "guest":
                    return 1;
                case "player":
                    return 16;
                case "operator":
                    return 128;
                case "owner":
                    return 255;
                default:
                    return 0;
            }
        }

        public static string GetColor(ushort ranklevel)
        {
            if (ranklevel == 0) return "&0";
            else if(ranklevel < 16) return "&7";
            else if(ranklevel < 128) return "&f";
            else if(ranklevel < 255) return "&9";
            else if(ranklevel >= 255) return "&4";
            else return "&7";
        }

        public static string GetColor(string rankName)
        {
            switch (rankName)
            {
                case "none":
                    return "&0";
                case "guest":
                    return "&7";
                case "player":
                    return "&f";
                case "operator":
                    return "&9";
                case "owner":
                    return "&4";
                default:
                    return "&7";
            }
        }
    }
}
