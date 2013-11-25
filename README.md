# Uhuru Windows Prison #


The Windows Prison is a system used to sandbox processes in a secure fashion on Windows Server 2012. The ultimate goal is to be able to support a multi-tenant Windows environment that can host any application that does not require administrator privileges.


## File system isolation and quota
In Windows the file system isolation is enforced with file system ACLs. Unlike Linux there is no concept of 'chroot', 'namespaces', for 'cgroups'. The only method to isolate file system access is with ACLs.
Windows has limited support for MAC (Mandoatory Access Control) for files (e.g. Linux's SELinux and AppArmor). This could allow a tenant to set the ACLs for its own files to be accessable (be it accidently or not) to everyone on the system.


Disk quota can be enforced in the following ways:
  - NTFS Disk Quota | enforces quota for a specific user | 
       http://msdn.microsoft.com/en-us/library/windows/desktop/aa365228(v=vs.85).aspx
  - FSRM (File System Resource Management) service | enforces quota on a specific path
       http://msdn.microsoft.com/en-us/library/bb972746(v=vs.85).aspx
  - Using VHDs | enfores quots on a specific path 
       http://msdn.microsoft.com/en-us/library/windows/desktop/dd323654(v=vs.85).aspx

A shorcomming for the first two methods is that they are limted to enforcing only disk space, without any other policy type like the number of files. This could allow a tenant to abuse the file system. 

By creating and dedicating a VHD for a tenant, the tenant could not wirte on other file systems. Scalability and uncollected empty space from VHD could be limiting factors. Mounted VHDs will consume Kernel memory / Paged Pool, thus limiting the total number of mounted disks. 
Empty space from VHDs could be collected with the Optimize-VHD PS command or CompactVirtualDisk function call from the VirtDisk.dll API. Further investigation is required on the performance and live compating of VHDs.