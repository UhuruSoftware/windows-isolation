using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Restrictions
{
    class Memory : Rule
    {
        public override void Apply(Prison prison)
        {
            prison.JobObject.JobMemoryLimitBytes = prison.Rules.TotalPrivateMemoryLimitBytes;
            prison.JobObject.ActiveProcessesLimit = prison.Rules.ActiveProcessesLimit;
        }

        public override void Destroy()
        {
        }

        public override RuleInstanceInfo[] List()
        {
            return new RuleInstanceInfo[0];
        }

        public override void Init()
        {
        }

        public override RuleType GetFlag()
        {
            return RuleType.Memory;
        }

        public override void Recover()
        {
            throw new NotImplementedException();
        }
    }
}
