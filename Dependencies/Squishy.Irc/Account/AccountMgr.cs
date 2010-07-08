using System;
using Squishy.Irc.Commands;
using System.IO;

namespace Squishy.Irc.Account
{
    public class AccountMgr
    {
        public enum AccountLevel
        {
            Guest = 1,
            User = 2,
            Admin = 3
        }
    }
}