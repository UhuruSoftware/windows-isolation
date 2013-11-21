using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Cells
{
    class WindowStationCell : Rule
    {
        private static readonly object windowStationLock = new object();

        public override void Apply(Prison prison)
        {
            Native.SECURITY_ATTRIBUTES secAttributes = new Native.SECURITY_ATTRIBUTES();
            secAttributes.nLength = Marshal.SizeOf(secAttributes);

            IntPtr windowStation = Native.CreateWindowStation(prison.User.Username, 0, Native.WINDOWS_STATION_ACCESS_MASK.WINSTA_NONE, null);

            IntPtr desktop = IntPtr.Zero;
            

            lock (windowStationLock)
            {
                IntPtr currentWindowStation = Native.GetProcessWindowStation();
                bool setOk = Native.SetProcessWindowStation(windowStation);

                if (!setOk)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                Native.CreateDesktop(prison.User.Username, null, null, 0, Native.ACCESS_MASK.DESKTOP_CREATEWINDOW, null);
                
                prison.ProcessStartupInfo.lpDesktop = string.Format(@"{0}\{0}", prison.User.Username);

                Native.SetProcessWindowStation(currentWindowStation);
            }
        }

        public override void Destroy()
        {
            throw new NotImplementedException();
        }

        public override CellInstanceInfo[] List()
        {
            List<CellInstanceInfo> result = new List<CellInstanceInfo>();

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
                result.Add(new CellInstanceInfo() { Name = workstation });
            }

            return result.ToArray();
        }

        public override void Init()
        {
        }

        public override CellType GetFlag()
        {
            return CellType.WindowStation;
        }
    }
}
