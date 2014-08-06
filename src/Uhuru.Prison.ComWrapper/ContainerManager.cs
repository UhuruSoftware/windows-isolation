using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.ComWrapper
{
    [ComVisible(true)]
    //[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IContainerManager
    {
        [ComVisible(true)]
        // IContainer[] ListContainers(); // will not work with go-ole
        string[] ListContainerIds();

        [ComVisible(true)]
        IContainer GetContainerById(string Id);
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    //[ProgId("Uhuru.ContainerManager")]
    public class ContainerManager : IContainerManager
    {
        public string[] ListContainerIds()
        {
            var res = new List<string>();
            var all = Prison.Load();
            foreach (var p in all)
            {
                res.Add(p.ID.ToString());
            }

            return res.ToArray();
        }

        public IContainer GetContainerById(string Id)
        {
            var p = Prison.LoadPrisonAndAttach(new Guid(Id));
            if (p == null)
            {
                return null;
            }

            var c = new Container(p);
            return c;
        }

        public void DestoryContainer(string Id)
        {
            var p = Prison.LoadPrisonAndAttach(new Guid(Id));
            if (p == null)
            {
                throw new ArgumentException("Container ID not found");
            }

            p.Destroy();
        }
    }
}
