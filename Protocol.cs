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
    class Protocol
    {
        public const int version = 7;
        public static int[] incomingPacketLengths = { 131, 0, 0, 0, 0, 9, 0, 0, 10, 0, 0, 0, 0, 66 };
    }
}
