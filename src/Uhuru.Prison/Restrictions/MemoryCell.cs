using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Cells
{
    class MemoryCell : Rule
    {
        public override void Apply(Prison prison)
        {
            prison.JobObject.JobMemoryLimitBytes = prison.Rules.TotalPrivateMemoryLimitBytes;
            prison.JobObject.ActiveProcessesLimit = prison.Rules.RunningProcessesLimit;
        }

        public override void Destroy()
        {
        }

        public override CellInstanceInfo[] List()
        {
            return new CellInstanceInfo[0];
        }

        public override void Init()
        {
        }

        public override CellType GetFlag()
        {
            return CellType.Memory;
        }
    }
}
