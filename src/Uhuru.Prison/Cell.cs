using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison
{
    public abstract class Rule
    {
        public abstract void Apply(Prison prison);

        public abstract void Destroy();

        public abstract CellInstanceInfo[] List();

        public abstract void Init();

        public abstract CellType GetFlag();
    }

    public class CellInstanceInfo
    {
        public string Name
        {
            get;
            set;
        }

        public string Info
        {
            get;
            set;
        }

        public CellInstanceInfo()
        {
            this.Name = string.Empty;
            this.Info = string.Empty;
        }
    }

    public enum CellType
    {
        None = 0x0,
        CPU = 0x1,
        Disk = 0x2,
        Filesystem = 0x4,
        Firewall = 0x8,
        Network = 0x10,
        WindowStation = 0x20,
        Memory = 0x40,
        All = 0x80
    }
}
