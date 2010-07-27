﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Data.EntityClient;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Runtime.Serialization;

[assembly: EdmSchemaAttribute()]

namespace Jad_Bot
{
    #region Contexts
    
    /// <summary>
    /// No Metadata Documentation available.
    /// </summary>
    public partial class UtilityBotDBContainer : ObjectContext
    {
        #region Constructors
    
        /// <summary>
        /// Initializes a new UtilityBotDBContainer object using the connection string found in the 'UtilityBotDBContainer' section of the application configuration file.
        /// </summary>
        public UtilityBotDBContainer() : base("name=UtilityBotDBContainer", "UtilityBotDBContainer")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            OnContextCreated();
        }
    
        /// <summary>
        /// Initialize a new UtilityBotDBContainer object.
        /// </summary>
        public UtilityBotDBContainer(string connectionString) : base(connectionString, "UtilityBotDBContainer")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            OnContextCreated();
        }
    
        /// <summary>
        /// Initialize a new UtilityBotDBContainer object.
        /// </summary>
        public UtilityBotDBContainer(EntityConnection connection) : base(connection, "UtilityBotDBContainer")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            OnContextCreated();
        }
    
        #endregion
    
        #region Partial Methods
    
        partial void OnContextCreated();
    
        #endregion
    
        #region ObjectSet Properties
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        public ObjectSet<Account> Accounts
        {
            get
            {
                if ((_Accounts == null))
                {
                    _Accounts = base.CreateObjectSet<Account>("Accounts");
                }
                return _Accounts;
            }
        }
        private ObjectSet<Account> _Accounts;
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        public ObjectSet<Messages> Messages
        {
            get
            {
                if ((_Messages == null))
                {
                    _Messages = base.CreateObjectSet<Messages>("Messages");
                }
                return _Messages;
            }
        }
        private ObjectSet<Messages> _Messages;

        #endregion
        #region AddTo Methods
    
        /// <summary>
        /// Deprecated Method for adding a new object to the Accounts EntitySet. Consider using the .Add method of the associated ObjectSet&lt;T&gt; property instead.
        /// </summary>
        public void AddToAccounts(Account account)
        {
            base.AddObject("Accounts", account);
        }
    
        /// <summary>
        /// Deprecated Method for adding a new object to the Messages EntitySet. Consider using the .Add method of the associated ObjectSet&lt;T&gt; property instead.
        /// </summary>
        public void AddToMessages(Messages messages)
        {
            base.AddObject("Messages", messages);
        }

        #endregion
    }
    

    #endregion
    
    #region Entities
    
    /// <summary>
    /// No Metadata Documentation available.
    /// </summary>
    [EdmEntityTypeAttribute(NamespaceName="Jad_Bot", Name="Account")]
    [Serializable()]
    [DataContractAttribute(IsReference=true)]
    public partial class Account : EntityObject
    {
        #region Factory Method
    
        /// <summary>
        /// Create a new Account object.
        /// </summary>
        /// <param name="username">Initial value of the Username property.</param>
        /// <param name="password">Initial value of the Password property.</param>
        /// <param name="userLevel">Initial value of the UserLevel property.</param>
        public static Account CreateAccount(global::System.String username, global::System.String password, global::System.String userLevel)
        {
            Account account = new Account();
            account.Username = username;
            account.Password = password;
            account.UserLevel = userLevel;
            return account;
        }

        #endregion
        #region Primitive Properties
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=true, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.String Username
        {
            get
            {
                return _Username;
            }
            set
            {
                if (_Username != value)
                {
                    OnUsernameChanging(value);
                    ReportPropertyChanging("Username");
                    _Username = StructuralObject.SetValidValue(value, false);
                    ReportPropertyChanged("Username");
                    OnUsernameChanged();
                }
            }
        }
        private global::System.String _Username;
        partial void OnUsernameChanging(global::System.String value);
        partial void OnUsernameChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.String Password
        {
            get
            {
                return _Password;
            }
            set
            {
                OnPasswordChanging(value);
                ReportPropertyChanging("Password");
                _Password = StructuralObject.SetValidValue(value, false);
                ReportPropertyChanged("Password");
                OnPasswordChanged();
            }
        }
        private global::System.String _Password;
        partial void OnPasswordChanging(global::System.String value);
        partial void OnPasswordChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.String UserLevel
        {
            get
            {
                return _UserLevel;
            }
            set
            {
                OnUserLevelChanging(value);
                ReportPropertyChanging("UserLevel");
                _UserLevel = StructuralObject.SetValidValue(value, false);
                ReportPropertyChanged("UserLevel");
                OnUserLevelChanged();
            }
        }
        private global::System.String _UserLevel;
        partial void OnUserLevelChanging(global::System.String value);
        partial void OnUserLevelChanged();

        #endregion
    
    }
    
    /// <summary>
    /// No Metadata Documentation available.
    /// </summary>
    [EdmEntityTypeAttribute(NamespaceName="Jad_Bot", Name="Messages")]
    [Serializable()]
    [DataContractAttribute(IsReference=true)]
    public partial class Messages : EntityObject
    {
        #region Factory Method
    
        /// <summary>
        /// Create a new Messages object.
        /// </summary>
        /// <param name="dateLeft">Initial value of the DateLeft property.</param>
        /// <param name="ircNick">Initial value of the IrcNick property.</param>
        /// <param name="message">Initial value of the Message property.</param>
        /// <param name="id">Initial value of the Id property.</param>
        public static Messages CreateMessages(global::System.String dateLeft, global::System.String ircNick, global::System.String message, global::System.String id)
        {
            Messages messages = new Messages();
            messages.DateLeft = dateLeft;
            messages.IrcNick = ircNick;
            messages.Message = message;
            messages.Id = id;
            return messages;
        }

        #endregion
        #region Primitive Properties
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.String DateLeft
        {
            get
            {
                return _DateLeft;
            }
            set
            {
                OnDateLeftChanging(value);
                ReportPropertyChanging("DateLeft");
                _DateLeft = StructuralObject.SetValidValue(value, false);
                ReportPropertyChanged("DateLeft");
                OnDateLeftChanged();
            }
        }
        private global::System.String _DateLeft;
        partial void OnDateLeftChanging(global::System.String value);
        partial void OnDateLeftChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.String IrcNick
        {
            get
            {
                return _IrcNick;
            }
            set
            {
                OnIrcNickChanging(value);
                ReportPropertyChanging("IrcNick");
                _IrcNick = StructuralObject.SetValidValue(value, false);
                ReportPropertyChanged("IrcNick");
                OnIrcNickChanged();
            }
        }
        private global::System.String _IrcNick;
        partial void OnIrcNickChanging(global::System.String value);
        partial void OnIrcNickChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.String Message
        {
            get
            {
                return _Message;
            }
            set
            {
                OnMessageChanging(value);
                ReportPropertyChanging("Message");
                _Message = StructuralObject.SetValidValue(value, false);
                ReportPropertyChanged("Message");
                OnMessageChanged();
            }
        }
        private global::System.String _Message;
        partial void OnMessageChanging(global::System.String value);
        partial void OnMessageChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=true, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.String Id
        {
            get
            {
                return _Id;
            }
            set
            {
                if (_Id != value)
                {
                    OnIdChanging(value);
                    ReportPropertyChanging("Id");
                    _Id = StructuralObject.SetValidValue(value, false);
                    ReportPropertyChanged("Id");
                    OnIdChanged();
                }
            }
        }
        private global::System.String _Id;
        partial void OnIdChanging(global::System.String value);
        partial void OnIdChanged();

        #endregion
    
    }

    #endregion
    
}
