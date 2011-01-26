using System;
using System.Collections.Generic;
using System.Text;

namespace uBuilder
{
    public class PermissionSet
    {
        //Static members
        public static PermissionSet Banned = new PermissionSet(false, true, false, false, false, false);
        public static PermissionSet Guest = new PermissionSet(true, true, false, false, false, false);
        public static PermissionSet Player = new PermissionSet(true, true, false, false, false, false);
        public static PermissionSet Operator = new PermissionSet(true, true, true, true, true, true);
        public static PermissionSet Owner = new PermissionSet(true, true, true, true, true, true);

        //Instance members
        public bool CanBuild;
        public bool CanChat;
        public bool CanEditAdminium;
        public bool CanBuildLiquids;
        public bool CanEditDoors;
        public bool CanHack;

        public PermissionSet(bool _build, bool _chat, bool _adminium, bool _liquids, bool _doors, bool _hacks)
        {
            this.CanBuild = _build;
            this.CanChat = _chat;
            this.CanEditAdminium = _adminium;
            this.CanBuildLiquids = _liquids;
            this.CanEditDoors = _doors;
            this.CanHack = _hacks;
        }

        public static PermissionSet operator |(PermissionSet source, PermissionSet mask)
        {
            PermissionSet temp = source;
            temp.CanBuild           |= mask.CanBuild;
            temp.CanChat            |= mask.CanChat;
            temp.CanEditAdminium    |= mask.CanEditAdminium;
            temp.CanBuildLiquids    |= mask.CanBuildLiquids;
            temp.CanEditDoors       |= mask.CanEditDoors;
            temp.CanHack            |= mask.CanHack;
            return temp;
        }

        public static PermissionSet operator &(PermissionSet source, PermissionSet mask)
        {
            PermissionSet temp = source;
            temp.CanBuild           &= mask.CanBuild;
            temp.CanChat            &= mask.CanChat;
            temp.CanEditAdminium    &= mask.CanEditAdminium;
            temp.CanBuildLiquids    &= mask.CanBuildLiquids;
            temp.CanEditDoors       &= mask.CanEditDoors;
            temp.CanHack            &= mask.CanHack;
            return temp;
        }
    }
}
