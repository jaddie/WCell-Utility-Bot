//using System;
//using System.Collections.Generic;
//using System.Text;

//using Squishy.Irc;
//using Squishy.Irc.Aliasing;

//namespace Squishy.Irc.ACL {
//    public class CommandAccessController {
//        IrcClient irc;
//        /// <summary>
//        /// If no rule applies, allow or disallow?
//        /// </summary>
//        public bool DefaultAccess = false;
//        /// <summary>
//        /// Command -> Accessor-Id -> List of rules
//        /// </summary>
//        IDictionary<Command, IDicitonary<CommandAccessor, List<CommandAccessRule>>> ruleMap;


//        public CommandAccessController(IrcClient irc) {
//            this.irc = irc;

//            ruleMap = new Dictionary<Command, IDicitonary<string, List<CommandAccessRule>>>();
//        }

//        public void AddRule(CommandAccessRule rule) {
//            foreach (Command cmd in rule.Commands) {
//                IDicitonary<string, List<CommandAccessRule>> rules;
//                if (!ruleMap.TryGetValue(cmd, out rules)) {
//                    ruleMap.Add(cmd, rules = new Dicitonary<string, List<CommandAccessRule>>());
//                }
//                foreach (CommandAccessor accessor in rule.Accessors) {
//                    List<CommandAccessRule> list;
//                    if (!rules.TryGetValue(accessor.Id, out list)) {
//                        rules.Add(accessor.Id, list = new List<CommandAccessRule>());
//                    }
//                    list.Add(rule);
//                }
//            }
//        }

//        public bool Check(string id) {
//            return Check(id, null);
//        }

//        public bool Check(string id, UserPrivSet privs) {
//            IDicitonary<string, List<CommandAccessRule>> rules;
//            if (ruleMap.TryGetValue(cmd, out rules)) {
//                List<CommandAccessRule> list;
//                if (rules.TryGetValue(id, out list)) {
//                    foreach (CommandAccessRule rule in list) {
//                        return rule.Allow &&
//                                (privs == null || (privs.Set & rule.GetPrivs(id)));
//                    }
//                }
//            }
//        }

//        /// <summary>
//        /// Checks wether there are contradictionary rules.
//        /// </summary>
//        public void CheckList() {
//            foreach (IDicitonary<string, List<CommandAccessRule>> rules in ruleMap.Values) {
//                foreach (List<CommandAccessRule> list in rules.Values) {
//                    foreach (CommandAccessRule rule in list) {
//                        return rule.Allow &&
//                                (privs == null || (privs.Set & rule.GetPrivs(id)));
//                    }
//                }
//            }
//        }

//        /// <summary>
//        /// Returns wether or not Command cmd may be triggered by the given trigger.
//        /// </summary>
//        public bool MayExecute(CmdTrigger trigger, Command cmd) {

//            return DefaultAccess;
//        }
//    }

//    public class CommandAccessRule {
//        public bool Allow;
//        List<Command> commands;
//        IDictionary<string, CommandAccessor> accessors;

//        public CommandAccessRule(bool Allow)
//            : this(allow, new List<Command>(),
//                                                        new Dictionary<string, CommandAccessor>()) {
//        }

//        public CommandAccessRule(bool allow, List<Command> commands,
//                                    IDictionary<string, CommandAccessor> accessors) {
//            this.Allow = allow;
//            this.commands = commands;
//            this.accessors = accessors;
//        }

//        public List<Command> Commands {
//            get {
//                return commands;
//            }
//        }

//        public IDictionary<string, CommandAccessor> Accessors {
//            get {
//                return accessors;
//            }
//        }

//        public Set<Privilege> GetPrivs(string id) {
//            CommandAccessor accessor;
//            if (accessors.TryGetValue(id, out accessor)) {
//                return accessor.Privs;
//            }
//            return null;
//        }

//        public CommandAccessor GetAccessor(string id) {
//            CommandAccessor accessor;
//            accessors.TryGetValue(id, out accessor);
//            return accessor;
//        }
//    }

//    // TODO: Change
//    public class CommandAccessor {
//        const Set<Privilege> Voiced = new Set<Privilege>() | Privilege.Voice | Privilege.HalfOp |
//                                        Privilege.Op | Privilege.Admin | Privilege.Owner;

//        const Set<Privilege> Opped = new Set<Privilege>() | Privilege.HalfOp | Privilege.Op |
//                                        Privilege.Admin | Privilege.Owner;

//        ChatTarget accessor;
//        Set<Privilege> privs;

//        public CommandAccessor(User user) {
//            accessor = user;
//        }

//        public CommandAccessor(Channel channel, Set<Privilege> privs) {
//            accessor = channel;
//            this.privs = privs;
//        }

//        public bool IsChannel() {
//            return accessor is Channel;
//        }

//        public ChatTarget Accessor {
//            get {
//                return accessor;
//            }
//        }

//        public Set<Privilege> Privs {
//            get {
//                return privs;
//            }
//        }
//    }
//}
