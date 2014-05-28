using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.ServiceModel;
using System.Text;
using Uhuru.Prison.ExecutorService;

namespace Uhuru.Prison.ChangeSession
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "ExecuteProcess" in both code and config file together.
    public class Executor : IExecutor
    {
        [PrincipalPermission(SecurityAction.Demand, Role = "BUILTIN\\Administrators")]
        public int ExecuteProcess(Prison prison, string filename, string arguments, Dictionary<string, string> extraEnvironmentVariables)
        {
            prison.Reattach();
            var p = prison.InitializeProcess(filename, arguments, false, extraEnvironmentVariables);

            return p.Id;
        }
    }
}
