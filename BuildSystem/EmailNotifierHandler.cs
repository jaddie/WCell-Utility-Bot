using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OpenPOP.POP3;
using Squishy.Network;
namespace Jad_Bot.BuildSystem
{
    public class EmailNotifierHandler
    {
        public static void CheckMail()
        {
            try
            {
                POPClient pop3 = new POPClient();
                pop3.Connect(JadBot.EmailHost, 110, false);
                pop3.Authenticate(JadBot.EmailUserName, JadBot.EmailPassword);
                for (int i = 0; i < pop3.GetMessageCount(); i++)
                {
                    var msg = pop3.GetMessage(i);
                    if (msg != null && msg.MessageBody.Capacity > 0 && !string.IsNullOrEmpty(msg.MessageBody[0]) && msg.Headers.From.ToString() == "buildserver@wcell.org")
                    {
                        string msgbody = msg.MessageBody[0].Replace("\n", " ");
                        msgbody = msgbody.Replace("\r", " ");
                        var msgsplit = new StringStream(msgbody);
                        msgbody = msgsplit.NextWord("=====");
                        JadBot.Irc.CommandHandler.Msg("#wcell", msgbody);
                    }
                }
                pop3.DeleteAllMessages();
                pop3.Disconnect();
            }
            catch (Exception e)
            {
                Utilities.UtilityMethods.Print(e.Message + e.Data + e.Source + e.StackTrace, true);
            }
        }
    }
}
