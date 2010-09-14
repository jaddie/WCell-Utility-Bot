using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jad_Bot.Utilities;
using Squishy.Irc.Commands;

namespace Jad_Bot.IrcCommands
{
    class AccountCommands
    {
        public class LoginCommand : Command
        {
            public LoginCommand()
                : base("Login")
            {
                Description = "login to your account";
                Usage = "login accname pw";
            }

            public override void Process(CmdTrigger trigger)
            {
                var username = trigger.Args.NextWord();
                var password = trigger.Args.NextWord();
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    trigger.Reply("Error invalid input please try again!");
                    return;
                }
                Login(trigger, username, password);
            }
            public static void Login(CmdTrigger trigger, string username, string password)
            {
                using (var db = new UtilityBotDBContainer())
                {
                    var authed = false;
                    foreach (var account in db.Accounts)
                    {
                        if (account.Username == username && account.Password == password)
                        {
                            switch (account.UserLevel)
                            {
                                case "guest":
                                    {
                                        authed = true;
                                        trigger.Reply(string.Format("Logged in as {0} with level {1}", account.Username, account.UserLevel));
                                    }
                                    break;
                                case "user":
                                    {
                                        authed = true;
                                        trigger.Reply(string.Format("Logged in as {0} with level {1}", account.Username, account.UserLevel));
                                    }
                                    break;
                                case "admin":
                                    {
                                        authed = true;
                                        trigger.Reply(string.Format("Logged in as {0} with level {1}", account.Username, account.UserLevel));
                                    }
                                    break;
                            }
                        }
                    }
                    if (!authed)
                        trigger.Reply("Account data invalid! Please try again!");
                }
            }
        }
        public class CreateAccountCommand : Command
        {
            public CreateAccountCommand()
                : base("createaccount", "ca")
            {
                Description = "Create a account";
                Usage = "createaccount accname pw role";
            }

            public override void Process(CmdTrigger trigger)
            {
                var username = trigger.Args.NextWord();
                var password = trigger.Args.NextWord();
                var userlevel = trigger.Args.NextWord();
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(userlevel))
                {
                    trigger.Reply("Error invalid input please try again!, remember the input order is username password userlevel \n userlevel options: guest user admin");
                }
                else
                {
                    using (var db = new UtilityBotDBContainer())
                    {
                        if (Enumerable.Any(Queryable.Where(db.Accounts, account => account.Username == username)))
                        {
                            trigger.Reply("That account already exists!");
                            return;
                        }
                    }
                    AddAccount(trigger, username, password, userlevel);
                    trigger.Reply("Account created");
                }
            }
            public static void AddAccount(CmdTrigger trigger, string username, string password, string userlevel)
            {
                using (var db = new UtilityBotDBContainer())
                {
                    var account = new Account { Username = username, Password = password, UserLevel = userlevel };
                    db.Accounts.AddObject(account);
                    db.SaveChanges();
                }
            }
        }
        public class DeleteAccountCommand : Command
        {
            public DeleteAccountCommand()
                : base("deleteaccount", "da")
            {
                Usage = "deleteaccount username";
                Description = "Removes the account as specified";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    var username = trigger.Args.NextWord();
                    if (string.IsNullOrEmpty(username))
                    {
                        trigger.Reply("Please specify username!");
                    }
                    else
                    {
                        using (var db = new UtilityBotDBContainer())
                        {
                            foreach (var account in Queryable.Where(db.Accounts, account => account.Username == username))
                            {
                                db.DeleteObject(account);
                                db.SaveChanges();
                                trigger.Reply("Account deleted!");
                                return;
                            }
                            trigger.Reply("Account not found!");
                        }
                    }
                }
                catch (Exception e)
                {
                    WriteErrorSystem.WriteError(e);
                }
            }
        }
        public class ChangeUserLevelCommand : Command
        {
            public ChangeUserLevelCommand()
                : base("ChangeUserLevel")
            {
                Description = "Changes the user level of a user";
                Usage = "ChangeUserLevel nick userlevel";
            }

            public override void Process(CmdTrigger trigger)
            {
                var username = trigger.Args.NextWord();
                var userlevel = trigger.Args.NextWord().ToLower();
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(userlevel))
                {
                    trigger.Reply("Invalid input please try again!");
                }
                else
                {
                    if (!ValidateUserLevel(userlevel))
                    {
                        trigger.Reply("Invalid userlevel specified, options are guest,user,admin");
                        return;
                    }
                    using (var db = new UtilityBotDBContainer())
                    {
                        foreach (var account in Queryable.Where(db.Accounts, account => account.Username == username))
                        {
                            account.UserLevel = userlevel;
                            trigger.Reply("Account level changed to " + userlevel);
                            db.SaveChanges();
                            return;
                        }
                    }
                }
            }
        }
        public static bool ValidateUserLevel(string userlevel)
        {
            return "admin user guest".Contains(userlevel.ToLower());
        }
    }
}
