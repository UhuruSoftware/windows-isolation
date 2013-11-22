using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Restrictions
{
    class CPU : Rule
    {
        public override void Apply(Prison prison)
        {
            throw new NotImplementedException();
        }

        public override void Destroy()
        {
            throw new NotImplementedException();
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
            return RuleType.CPU;
        }

        public override void Recover()
        {
            throw new NotImplementedException();
        }
    }
}
