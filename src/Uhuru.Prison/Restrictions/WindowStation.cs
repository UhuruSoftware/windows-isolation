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
        IntPtr windowStation = IntPtr.Zero;
        IntPtr desktop = IntPtr.Zero;

        public override void Apply(Prison prison)
        {
            if (windowStation != IntPtr.Zero) return;

            Native.SECURITY_ATTRIBUTES secAttributes = new Native.SECURITY_ATTRIBUTES();
            secAttributes.nLength = Marshal.SizeOf(secAttributes);


            windowStation = NativeOpenWindowStation(prison.User.Username);

            int openWinStaStatus = Marshal.GetLastWin32Error();

            // Error 0x2 is ERROR_FILE_NOT_FOUND
            // http://msdn.microsoft.com/en-us/library/windows/desktop/ms681382%28v=vs.85%29.aspx
            if (windowStation == IntPtr.Zero && openWinStaStatus != 0x2)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (windowStation == IntPtr.Zero &&  openWinStaStatus == 0x2)
            {
                // TODO SECURITY: change security attributes. the default will give everyone access to the object including other prisons
                windowStation = NativeCreateWindowStation(prison.User.Username);

                if (windowStation == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }

            lock (windowStationContextLock)
            {
                IntPtr currentWindowStation = NativeGetProcessWindowStation();

                try
                {
                    bool setOk = NativeSetProcessWindowStation(windowStation);

                    if (!setOk)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    // TODO SECURITY: change security attributes. the default will give everyone access to the object including other prisons
                    desktop = NativeCreateDesktop();

                    if (desktop == IntPtr.Zero)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    prison.desktopName = string.Format(@"{0}\Default", prison.User.Username);
                }
                finally
                {
                    NativeSetProcessWindowStation(currentWindowStation);
                }
            }
        }

        public override void Destroy(Prison prison)
        {
            
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
            NativeEnumWindowsStations(childProc, gcHandle);

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

        public override void Recover(Prison prison)
        {
        }

        private static bool NativeEnumWindowsStations(Native.EnumWindowStationsDelegate childProc, GCHandle gcHandle)
        {
            return Native.EnumWindowStations(childProc, GCHandle.ToIntPtr(gcHandle));
        }

        private static IntPtr NativeOpenWindowStation(string username)
        {
            return Native.OpenWindowStation(username, false, Native.WINDOWS_STATION_ACCESS_MASK.WINSTA_CREATEDESKTOP);
        }

        private static IntPtr NativeCreateWindowStation(string username)
        {
            return Native.CreateWindowStation(username, 0, Native.WINDOWS_STATION_ACCESS_MASK.WINSTA_CREATEDESKTOP, null);
        }

        private static bool NativeSetProcessWindowStation(IntPtr windowStation)
        {
            return Native.SetProcessWindowStation(windowStation);
        }

        private static IntPtr NativeGetProcessWindowStation()
        {
            return Native.GetProcessWindowStation();
        }

        private static IntPtr NativeCreateDesktop()
        {
            return Native.CreateDesktop("Default", null, null, 0, Native.ACCESS_MASK.DESKTOP_CREATEWINDOW, null);
        }

    }
}

