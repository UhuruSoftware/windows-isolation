using System;
using System.Collections.Generic;
using System.IO.Pipes;
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
            string curDir,
            Dictionary<string, string> extraEnvironmentVariables,
            PipeStream stdinPipeName, 
            PipeStream stdoutPipeName, 
            PipeStream stderrPipeName
            );
    }
}
