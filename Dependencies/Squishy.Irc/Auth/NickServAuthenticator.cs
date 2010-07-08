using Squishy.Irc.Protocol;

namespace Squishy.Irc.Auth
{
    public class NickServAuthenticator : AsyncIrcAuthenticator
	{
		private string m_AuthOpcode;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="opcode">usually 307 or 320</param>
    	public NickServAuthenticator(string opcode)
		{
			m_AuthOpcode = opcode;
		}

    	public override string ServiceName
        {
            get { return "NickServ"; }
        }

    	public override string AuthOpcode
    	{
    		get { return m_AuthOpcode; }
    	}

    	public override void ResolveAuth(IrcUser user, IrcUserAuthResolvedHandler authResolvedHandler)
        {
            user.IrcClient.CommandHandler.Whois(user.Nick);
            base.ResolveAuth(user, authResolvedHandler);
        }

        protected override string ResolveAuth(IrcUser user, IrcPacket packet)
        {
            packet.Content.SkipWord();

            var nick = packet.Content.NextWord();
            if (nick == user.Nick)
            {
                user.AuthName = nick;
                return nick;
            }
            return null;
        }
    }
}
