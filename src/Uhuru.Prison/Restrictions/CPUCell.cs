using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Cells
{
    class CPUCell : Rule
    {
        public override void Apply(Prison prison)
        {
            throw new NotImplementedException();
        }

        public override void Destroy()
        {
            throw new NotImplementedException();
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
            return CellType.CPU;
        }
    }
}
