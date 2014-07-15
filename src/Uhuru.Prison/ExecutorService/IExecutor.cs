using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Uhuru.Prison.ExecutorService
{
    [ServiceContract]
    public interface IExecutor
    {
        [OperationContract]
        int ExecuteProcess(
            Prison prison,
            string filename, 
            string arguments,
            Dictionary<string, string> extraEnvironmentVariables,
            string stdinPipeName, 
            string stdoutPipeName, 
            string stderrPipeName
            );
    }
}
