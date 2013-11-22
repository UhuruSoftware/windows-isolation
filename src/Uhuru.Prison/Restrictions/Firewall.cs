using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Uhuru.Prison.Utilities;

namespace Uhuru.Prison.Restrictions
{
    class Firewall : Rule
    {
        public override void Apply(Prison prison)
        {
            Firewall.RemovePortAccess(prison.Rules.UrlPortAccess, true);
            Firewall.AddPortAccess(prison.Rules.UrlPortAccess, prison.User.Username);
        }

        public override void Destroy()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Allow access to the URL with the specified port for the specified username.
        /// This will allow IIS HWC and IIS Express to bind and listen to that port.
        /// </summary>
        /// <param name="port">Http port number.</param>
        /// <param name="Username">Windows Local username.</param>
        public static void AddPortAccess(int port, string Username)
        {
            string command = String.Format("netsh http add urlacl url=http://*:{0}/ user={1} listen=yes delegate=no", port.ToString(), Username);

            Logger.Debug("Adding url acl with the following command: {0}", command);

            int ret = Command.ExecuteCommand(command);

            if (ret != 0)
            {
                throw new Exception("netsh http add urlacl command failed.");
            }
        }

        /// <summary>
        /// Remove access for the specified port.
        /// </summary>
        /// <param name="port">Http port number.</param>
        public static void RemovePortAccess(int port, bool ignoreFailure = false)
        {
            string command = String.Format("netsh http delete urlacl url=http://*:{0}/", port.ToString());

            Logger.Debug("Removing url acl with the following command: {0}", command);

            int ret = Command.ExecuteCommand(command);

            if (ret != 0 && !ignoreFailure)
            {
                throw new Exception("netsh http delete urlacl command failed.");
            }
        }

        public static string ListPortAccess()
        {
            Logger.Debug("Listing url acl");

            string output = Command.RunCommandAndGetOutput("netsh", "http show urlacl");

            return output;
        }

        public override RuleInstanceInfo[] List()
        {
            List<RuleInstanceInfo> result = new List<RuleInstanceInfo>();

            string portAccessList = ListPortAccess();

            foreach (Match match in Regex.Matches(portAccessList, "Reserved URL.+?SDDL", RegexOptions.Singleline))
            {
                Match infoMatch = Regex.Match(match.Value, @"Reserved URL.+?:\s+(http://.*)$", RegexOptions.Multiline);
                Match userMatch = Regex.Match(match.Value, @"User:\s+(.+)$", RegexOptions.Multiline);


                string info = infoMatch.Groups.Count > 1 ? infoMatch.Groups[1].Value.Trim() : string.Empty;
                string name = userMatch.Groups.Count > 1 ? userMatch.Groups[1].Value.Trim() : (Regex.IsMatch(match.Value, "Can't lookup sid, Error: 1332") ? "orphaned" : string.Empty);

                if (name.Contains(PrisonUser.GlobalPrefix + PrisonUser.Separator) || name == "orphaned")
                {
                    result.Add(new RuleInstanceInfo()
                    {
                        Name = name,
                        Info = info
                    });
                }
            }

            return result.ToArray();
        }

        public override void Init()
        {
        }

        public override RuleType GetFlag()
        {
            return RuleType.Firewall;
        }

        public override void Recover()
        {
            throw new NotImplementedException();
        }
    }
}
