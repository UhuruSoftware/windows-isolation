using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Prison.Utilities;

namespace Uhuru.Prison.Allowances
{
    // Add the prison user to the IIS_IUSRS group to allow it to have access to the complication mutex
    // Issues also described here: http://blogs.msdn.com/b/jorman/archive/2006/07/24/system-invalidoperationexception-mutex-could-not-be-created.aspx
    class IISGroup : Rule
    {
        const string IISGroupName = "IIS_IUSRS";
        
        public override void Apply(Prison prison)
        {
            WindowsUsersAndGroups.AddUserToGroup(prison.User.Username, IISGroupName);
        }

        public override void Destroy(Prison prison)
        {
        }

        public override RuleInstanceInfo[] List()
        {
            throw new NotImplementedException();           
        }

        public override void Init()
        {
        }

        public override RuleType GetFlag()
        {
            return RuleType.IISGroup;
        }

        public override void Recover(Prison prison)
        {
        }
    }
}
