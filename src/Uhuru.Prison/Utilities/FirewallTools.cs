// -----------------------------------------------------------------------
// <copyright file="FirewallTools.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.Prison
{
    using System;
    using System.Globalization;
    using System.Security.Principal;
    using NetFwTypeLib;

    /// <summary>
    /// A set of Windows Firewall tool. 
    /// 
    /// NB: BLOCK rules have percedence/priority over ALLOW rules.
    /// 
    /// Note from http://technet.microsoft.com/en-us/library/cc755191(v=ws.10).aspx :
    /// This type of rule explicitly blocks a particular type of incoming or outgoing traffic. Because 
    /// these rules are evaluated before allow rules, they take precedence. Network traffic that matches 
    /// both an active block and an active allow rule is blocked.
    /// </summary>
    public static class FirewallTools
    {
        /// <summary>
        /// Opens a firewall port.
        /// </summary>
        /// <param name="port">the port to open</param>
        /// <param name="rulName">the application to open the port to</param>
        public static void OpenPort(int port, string rulName)
        {
            Type netFwOpenPortType = Type.GetTypeFromProgID("HNetCfg.FWOpenPort");
            INetFwOpenPort openPort = (INetFwOpenPort)Activator.CreateInstance(netFwOpenPortType);
            openPort.Port = port;
            openPort.Name = rulName;
            openPort.Enabled = true;
            openPort.Protocol = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;

            Type netFwMgrType = Type.GetTypeFromProgID("HNetCfg.FwMgr", false);
            INetFwMgr mgr = (INetFwMgr)Activator.CreateInstance(netFwMgrType);
            INetFwOpenPorts openPorts = (INetFwOpenPorts)mgr.LocalPolicy.CurrentProfile.GloballyOpenPorts;

            openPorts.Add(openPort);
        }

        /// <summary>
        /// Closes a port
        /// </summary>
        /// <param name="port">the port to be closed</param>
        public static void ClosePort(int port)
        {
            Type netFwMgrType = Type.GetTypeFromProgID("HNetCfg.FwMgr", false);
            INetFwMgr mgr = (INetFwMgr)Activator.CreateInstance(netFwMgrType);
            INetFwOpenPorts openPorts = (INetFwOpenPorts)mgr.LocalPolicy.CurrentProfile.GloballyOpenPorts;
            openPorts.Remove(port, NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);
        }

        /// <summary>
        /// Opens a range of firewall ports
        /// </summary>
        /// <param name="lowPort">the start port to open</param>
        /// <param name="highPort">the end port to open</param>
        /// <param name="ruleName">Firewall rule name</param>
        public static void OpenPortRange(int lowPort, int highPort, string ruleName)
        {
            Type netFwOpenPortType = Type.GetTypeFromProgID("HNetCfg.FwRule");
            INetFwRule rule = (INetFwRule)Activator.CreateInstance(netFwOpenPortType);
            rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            rule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
            rule.Profiles = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL;
            rule.LocalPorts = lowPort.ToString(CultureInfo.InvariantCulture) + "-" + highPort.ToString(CultureInfo.InvariantCulture);
            rule.Name = ruleName;
            rule.Enabled = true;

            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            firewallPolicy.Rules.Add(rule);
        }


        public static void BlockAllOutbound(string ruleName, string windowsUsername)
        {

            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            // This type is only avaible in Windows Server 2012
            INetFwRule3 rule = ((INetFwRule3)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwRule")));

            rule.Name = ruleName;
            rule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
            rule.Enabled = true;

            string userSid = GetLocalUserSid(windowsUsername);
            rule.LocalUserAuthorizedList = String.Format("D:(A;;CC;;;{0})", userSid);

            firewallPolicy.Rules.Add(rule);
        }

        public static void BlockAllInbound(string ruleName, string windowsUsername)
        {

            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            // This type is only avaible in Windows Server 2012
            INetFwRule3 rule = ((INetFwRule3)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwRule")));

            rule.Name = ruleName;
            rule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
            rule.Enabled = true;

            string userSid = GetLocalUserSid(windowsUsername);
            rule.LocalUserAuthorizedList = String.Format("D:(A;;CC;;;{0})", userSid);

            firewallPolicy.Rules.Add(rule);
        }

        public static void DeleteRule(string ruleName)
        {
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            firewallPolicy.Rules.Remove(ruleName);
        }

        private static string GetLocalUserSid(string windowsUsername)
        {
            NTAccount ntaccount = new NTAccount("", windowsUsername);
            return ntaccount.Translate(typeof(SecurityIdentifier)).Value;
        }
    }
}
