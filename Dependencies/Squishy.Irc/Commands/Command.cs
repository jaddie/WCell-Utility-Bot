using System;
using Squishy.Irc.Account;

namespace Squishy.Irc.Commands
{
	/// <summary>
	/// Basic Command Class, Inherit your Commands from here. Automatically creates one instance
	/// per IrcClient when the Class is loaded, using the default constructor.
	/// </summary>
	public abstract class Command
	{
		#region Delegates

		public delegate void CommandCallback(CmdTrigger trigger);

		#endregion

		/// <summary>
		/// All Aliases which can trigger the Process method of this Command.
		/// </summary>
		public readonly string[] Aliases;

		private string m_description;
		private bool m_enabled;
		private string m_usage;
        private AccountMgr.AccountLevel m_requiredAccountLevel;

		/*private static Command CreateInstance() {
			System.Reflection.ConstructorInfo ctor = typeof(Command).GetConstructor(Type.EmptyTypes);
			return (Command)ctor.Invoke(new object[0]);
		}*/

		/// <summary>
		/// In the Constructor you deliver the alias names. Calling this ctor automatically sets
		/// the Instance to the newly invoked instance.
		/// </summary>
		protected Command(params string[] aliases)
		{
			m_enabled = true;
            m_requiredAccountLevel = AccountMgr.AccountLevel.Guest;
			Aliases = aliases;
			Usage = "";
			Description = "";
		}

		/// <summary>
		/// Indicates wether or not this command is enabled.
		/// If false, Commands.ReactTo will not trigger this Command'str Process method.
		/// Alternatively you can Add/Remove this Command to/from Commands.CommandsByAlias to control wether or not
		/// certain Commands should or should not be used.
		/// </summary>
		public bool Enabled
		{
			get { return m_enabled; }
			set { m_enabled = value; }
		}

        /// <summary>
        /// the required account level to use the command.
        /// </summary>
        public AccountMgr.AccountLevel RequiredAccountLevel
        {
            get { return m_requiredAccountLevel; }
            set { m_requiredAccountLevel = value; }
        }

		/// <summary>
		/// Describes how to use the command.
		/// </summary>
		public string Usage
		{
			get { return m_usage; }
			set { m_usage = value; }
		}

		/// <summary>
		/// Describes the command itself.
		/// </summary>
		public string Description
		{
			get { return m_description; }
			set { m_description = value; }
		}

		public virtual string Name
		{
			get { return GetType().Name.Replace("Command", ""); }
		}

		public event CommandCallback Executed;

		internal void ExecutedNotify(CmdTrigger trigger)
		{
			if (Executed != null)
			{
				Executed(trigger);
			}
		}

		/// <summary>
		/// Is called when the command is triggered (case-insensitive).
		/// </summary>
		public abstract void Process(CmdTrigger trigger);

		///// <summary>
		///// An array of possible server-side replies to this Command.
		///// If this command is executed, the response from the server will again be replied to the user who initiated this.
		///// </summary>
		////public virtual string[] ExpectedServResponses {
		////    get {
		////        return null;
		////    }
		////}
		

		/// <summary>
		/// Is triggered when the processing throws an Exception.
		/// </summary>
		protected virtual void OnFail(CmdTrigger trigger)
		{
		}

		internal void FailNotify(CmdTrigger trigger, Exception ex)
		{
			OnFail(trigger);
			trigger.Irc.CommandFailNotify(trigger, ex);
		}
	}
}