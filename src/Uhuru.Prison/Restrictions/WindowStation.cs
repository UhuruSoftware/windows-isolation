using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Restrictions
{
    class WindowStation : Rule
    {
        private static readonly object windowStationContextLock = new object();

        public override void Apply(Prison prison)
        {
            Native.SECURITY_ATTRIBUTES secAttributes = new Native.SECURITY_ATTRIBUTES();
            secAttributes.nLength = Marshal.SizeOf(secAttributes);

            // TODO SECURITY: change security attributes. the default will give everyone access to the object including other prisons
            IntPtr windowStation = Native.CreateWindowStation(prison.User.Username, 0, Native.WINDOWS_STATION_ACCESS_MASK.WINSTA_NONE, null);

            lock (windowStationContextLock)
            {
                IntPtr currentWindowStation = Native.GetProcessWindowStation();

                try
                {
                    bool setOk = Native.SetProcessWindowStation(windowStation);

                    if (!setOk)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    // TODO SECURITY: change security attributes. the default will give everyone access to the object including other prisons
                    var desktop = Native.CreateDesktop("Default", null, null, 0, Native.ACCESS_MASK.DESKTOP_CREATEWINDOW, null);

                    if (desktop == IntPtr.Zero)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    prison.ProcessStartupInfo.lpDesktop = string.Format(@"{0}\Default", prison.User.Username);
                }
                finally
                {
                    Native.SetProcessWindowStation(currentWindowStation);
                }
            }
        }

        public override void Destroy()
        {
            throw new NotImplementedException();
        }

        public override RuleInstanceInfo[] List()
        {
            List<RuleInstanceInfo> result = new List<RuleInstanceInfo>();

            Native.EnumWindowStationsDelegate childProc = new Native.EnumWindowStationsDelegate((windowStation, lParam) =>
            {
                GCHandle gch = GCHandle.FromIntPtr(lParam);
                IList<string> list = gch.Target as List<string>;

                if (null == list)
                {
                    return (false);
                }

                list.Add(windowStation);

                return (true);
            });

            IList<string> workstationList = new List<string>();

            GCHandle gcHandle = GCHandle.Alloc(workstationList);
            Native.EnumWindowStations(childProc, GCHandle.ToIntPtr(gcHandle));

            foreach (string workstation in workstationList)
            {
                result.Add(new RuleInstanceInfo() { Name = workstation });
            }

            return result.ToArray();
        }

        public override void Init()
        {
        }

        public override RuleType GetFlag()
        {
            return RuleType.WindowStation;
        }

        public override void Recover()
        {
            throw new NotImplementedException();
        }
    }
}
