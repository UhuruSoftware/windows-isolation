# Uhuru Windows Prison #

The goal for the Windows Prison is to be able to support a multi-tenant secure environment on Microsoft Windows Server.

### Supported Versions ###

- Windows Server 2012 
- Windows Server 2012 R2 

There are no plans to support older versions of Windows or Desktop equivalents (Windows 8 and 8.1).
Desktop versions of Windows have different default behavior when running processes and default policies.

### Use Case ###

Standard Windows Server configurations do not have sufficient security capabilities to securely allow multi-tenant use. The Uhuru Windows Prison fills this gap in Windows Server functionality.

The Prison is meant for a clean Windows installation that is managed by automated software (such as a Platform as a Service) because there are system-wide policies and configurations that are assumed to be in place.

Disclaimer: The Prison contains complex mechanisms that are not tested on systems administered by humans in every-day life scenarios.

### Priorities ###

1. The first priority is to achieve privacy - this means that any application cannot access another application's data
2. The second priority is offering a decent SLA (service level agreement) - which means the Prison has to be resistant to DoS attacks
3. The third priority is monitoring - the prison needs to publish sufficient information so system administrators can identify and mitigate threats that are not handled automatically   

## Surface Areas ##

In order to secure these areas the Prison needs a separate Windows identity to run applications. The Prison starts with a non-privileged user whose credentials are generated in a cryptographically secure manner. For some of the areas a Windows Job Object is required next to this identity. 

All application processes are run under the aforementioned user account, and inside the Job Object.   

### CPU ###

**Possible attack scenarios**

- steal CPU cycles from other applications running on the same machine
- perform a DoS attack on the box by using all available CPU cycles
- fork bombs

**Lock-down mechanism**

- the job object containing the app has a limit on the number of active processes
- the job object that contains the app has a priority class limit flag set, and the priority class is 'Low'; this means that all processes started in a prison have a Low priority
- the job object that contains the app is given a CPU rate - this limits the percentage of CPU cycles available for all processes running in the prison

**Risks**

- a Low priority process starting a child process that has a higher priority than 'Low'

**References**

- [Scheduling Priorities](http://msdn.microsoft.com/en-us/library/windows/desktop/ms685100.aspx)
- [Job Objects CPU Rate](http://msdn.microsoft.com/en-us/library/windows/desktop/hh448384.aspx)
- [Job Objects Basic Limits](http://msdn.microsoft.com/en-us/library/windows/desktop/ms684147.aspx)
- [Processor Scheduling Quanta](http://msdn.microsoft.com/en-us/library/windows/desktop/hh448384.aspx)

----------

### Memory ###

**Possible attack scenarios**

- use more memory than allowed 
- buffer overflow attacks
- targeting specific memory locations
- creating large memory mapped files that are not accounted for

**Lock-down mechanism**

- all processes started in a prison (including all child processes) are assigned to a Windows Job Object that limits the amount of virtual  memory that can be committed
- an active process (called a Prison Guard) checks (via polling) for total memory usage and immediately kills the Job Object and thus all containing processes if a limit is breached; the guard will constantly re-assign the virtual memory commit limit based on nonenforceable resources like shared memory
- In order to decrease the chances of an app crashing the system between polling calls, the containerized processes will will depend on the Guard process to run and thus all processes will get killed by the OS of the Guard process terminates. This is achieved with an additional Job Object only referenced by the Guard and configured to kill the Job Object when the reference count reaches 0.
- The PagedPoolQuota and NonPagedPoolQuota setting can be configured per user in the Registry to enforce those limits from the Windows Kernel.

**Risks**

- an application may want to dynamically verify how much memory it can use by allocating more and more memory in a loop, and trapping the bad allocation error when it happens; in this case, the guard would kill the application
- a long running multi-tenant environment gives a malicious user a test-bed for creating exploits that are based on buffer overflow attacks and that target specific memory locations
- there may be undiscovered methods that can be used to allocate memory that is not properly tracked and accounted for by Windows  
- allocating shared memory to leak memory
- abusing the memory manager to create fragmentation
- the Windows kernel creates the 'conhost' process for console applications outside of job objects

**References**

- [Job Objects Basic Limits](http://msdn.microsoft.com/en-us/library/windows/desktop/ms684147.aspx)
- [Job Objects Extended Limits](http://msdn.microsoft.com/en-us/library/windows/desktop/ms684156.aspx)
- [C++ bad_alloc exception](http://www.cplusplus.com/reference/new/bad_alloc/)
- [Exploit Mitigations in Windows](http://www.microsoft.com/security/sir/strategy/default.aspx#!section_3_3)
- [Setting Process Mitigation Policies](http://msdn.microsoft.com/en-us/library/windows/desktop/hh769088.aspx)
- [Mysteries of Memory Management, Part 1](http://channel9.msdn.com/Events/TechEd/NorthAmerica/2011/WCL405)
- [Mysteries of Memory Management, Part 2](http://channel9.msdn.com/Events/TechEd/NorthAmerica/2011/WCL406)
- [Memory Priority Hint](http://msdn.microsoft.com/en-us/library/windows/desktop/hh448387.aspx)
- [Memory Resource Notification](http://msdn.microsoft.com/en-us/library/aa366541.aspx)
- [PagedPoolQuota and NonPagedPoolQuota](http://books.google.ro/books?id=_Y2WiG6WfGkC&lpg=PA246&dq=windows%20internals%20PagedPoolQuota&hl=ro&pg=PA246#v=onepage&q=windows%20internals%20PagedPoolQuota&f=false)

----------

### Windows Random Number Generator ###

**Possible attack scenarios**

- The Windows CryptoAPI provides a high quality RNG interface for processes. Malicious processes running in a container can abuse the API and possibely lower the ouput entropy for other processes.

**Lock-down mechanism**

- We do not yet have a way to prevent users from running RNG attacks from a prison.

**Risks**

- Inter-prison access to encrypted data may be possible.

**References**

- [Cryptanalysis of the Random Number Generator of Windows](http://eprint.iacr.org/2007/419.pdf)
- [RNG Attacks](http://en.wikipedia.org/wiki/Random_number_generator_attack)

----------

### Mandatory Access Controls ###

**Possible attack scenarios**

- without Mandatory Access Controls the system is vulnerable due to complex black listing mechanisms for securable objects such as File System objects 

**Lock-down mechanism**

- Mandatory Access Control is achieved using Windows Integrity Levels
- All files in a user's sandbox have an "Untrusted" Integrity Level
- Disabling the Bypass Traverse Checking privilege

**Risks**

- compatibility problems may arise, because most applications do not assume they are running in an environment that is configured in this manner

**References**

- [Windows Integrity Mechanism](http://msdn.microsoft.com/en-us/library/bb625957.aspx)
- [Windows Integrity Mechanism Resources](http://msdn.microsoft.com/en-us/library/bb625959.aspx)
- [The Bypass Traverse Checking Privilege](http://blogs.technet.com/b/markrussinovich/archive/2005/10/19/the-bypass-traverse-checking-or-is-it-the-change-notify-privilege.aspx)

----------

### HTTP Server API ###

**Possible attack scenarios**

- using ports or hosts that have not been assigned to the user via the HTTP Server API

**Lock-down mechanism**

- by default, users do not have access to listen on any URLs using HTTP Server API (.Net HttpListener, WCF, IIS hostable web core, etc.)
- the prison has a list of white-listed URLs it can listen on

**Risks**

- users can still open sockets on ports that are not being used

**References**

- [Configuring HTTP and HTTPS](http://msdn.microsoft.com/en-us/library/ms733768.aspx)
- [HTTP Server API](http://msdn.microsoft.com/en-us/library/windows/desktop/aa364510%28v=vs.85%29.aspx)

----------

### Disk ###

Disk quota can be enforced in more than one way on Windows:

- NTFS Disk Quotas
- File System Resource Management
- Virtual Hard Disks (VHDs)

The Uhuru Windows prison does not use VHDs for sandboxing because of the following:

- By creating and dedicating a VHD for a tenant, the tenant could not write on other file systems
- Scalability and uncollected empty space from VHDs could be limiting factors
 - Empty space from VHDs could be collected with the Optimize-VHD PS command or CompactVirtualDisk function call from the VirtDisk.dll API. Further investigation is required on the performance and live compacting of VHDs
- Mounted VHDs will consume Kernel memory / Paged Pool, thus limiting the total number of mounted disks

**Possible attack scenarios**

- Attack host machine by filling the entire disk
- Use more disk space than allowed

**Lock-down mechanism**

- NTFS Disk Quotas - enforces quota for a specific user 
- FSRM (File System Resource Management) service - enforces quota on a specific path 

**Risks**

- there may be unidentified ways the user can write to disk and perform a DoS by filling in the entire hard drive; such ways would involve Windows APIs whose side effects include writing data to disk (logging functions, registry functions, functions that write to the security logs, etc.)
- a shortcoming for the first two security methods is that they are limited to enforcing only disk space, without any other policy type like the number of files; this could allow a tenant to abuse the file system

**References**

- [Managing Disk NTFS Quotas](http://msdn.microsoft.com/en-us/library/windows/desktop/aa365228.aspx)
- [File System Resource Management](http://msdn.microsoft.com/en-us/library/bb972746.aspx)
- [About VHDs](http://msdn.microsoft.com/en-us/library/windows/desktop/dd323654.aspx)

----------

### Filesystem ###

In Windows the file system isolation is enforced with file system ACLs. Unlike Linux there is no concept of 'chroot', 'namespaces', or 'cgroups'. Windows has limited support for MAC (Mandatory Access Control) for files (e.g. Linux's SELinux and AppArmor). This could allow a tenant to maliciously or accidentally set the ACLs for its own files to be accessible to everyone on the system.

**Possible attack scenarios**

- Reading data from other users
- Writing to restricted locations
- Taking ownership of restricted files/folders
- 

**Lock-down mechanism**

- by default, users are locked down by the Mandatory Access Control mechanism described earlier in the document
- users have a list of white-listed ACLs; these include readable files and folders, executable files and a home directory, which the prison user owns

**Risks**

**References**

- [Access Control Lists](http://msdn.microsoft.com/en-us/library/windows/desktop/aa374872.aspx)

----------

### Network ###

**Possible attack scenarios**

-	Prison applications have unrestricted access to the network resources
-	Prison applications can create a denial of service on network bandwidth
-	Prison applications can exhaust networking resources like TCP connections and listening ports

**Lock-down mechanism**

-	apply network upload limit policy for the Prison user to limit the upload bandwidth limit
-	Windows Filtering Platform could enforce quota on TCP connections and listening ports

**Risks**

- unrestricted download rate may pose some risks; this issue can be mitigated with WFP and/or dummynet

**References**

- [Policy-based Quality of Service](http://technet.microsoft.com/en-us/library/jj159288.aspx)
- [dummynet](http://info.iet.unipi.it/~luigi/dummynet/)
- [Windows Filtering Platform](http://msdn.microsoft.com/en-us/library/windows/desktop/aa366510)

----------

### Window Stations ###

**Possible attack scenarios**

- prison applications can create GDI objects that have limits in the OS; reaching these limits in one prison would prevent other prisons from creating objects
- if all prisons would share the same Window Station they would be able to access objects created by another prison

**Lock-down mechanism**

- a new window station and default desktop is created for each prison; this makes sure that:
 - prisons are not sharing the same GDI object pool
 - inter-prison access to GDI objects is prevented

**Risks**

- There are no risks currently identified

**References**

- [GDI Objects](http://msdn.microsoft.com/en-us/library/windows/desktop/ms724291(v=vs.85).aspx)

----------

### Firewall ###

**Possible attack scenarios**

- Prison applications have access to the network interfaces from the host Windows OS and possibly connect to internal management services.

**Lock-down mechanism**

-	Windows firewall rules are applied to specific outbound IP addresses to prevent access to the internal network
-	only authorized incoming ports are opened for the prison to listen to.
-	firewall rules are applied on a per Windows user basis.


**Risks**

-	firewall rules cannot block Prison processes to open network sockets on specific ports; it can just block the incoming connection to specific ports; a possible solution is to use Windows Filtering Platform

**References**

- [Windows Firewall API](http://msdn.microsoft.com/en-us/library/windows/desktop/ff956124)
- [Windows Filtering Platform](http://msdn.microsoft.com/en-us/library/windows/desktop/aa366510)

----------

## Testing ##

Every surface area described has Unit Tests that verify the locking mechanism.

Other testing methods will include using [OWASP](https://www.owasp.org/index.php/OWASP_.NET_Active_Projects) tools  (such as [SAM'SEH](https://www.owasp.org/index.php/SAM%27SHE)) to audit Prison security.

## Monitoring ##

Monitoring is just as important as locking down a sandbox. Vulnerabilities are always present in software, and having data available about how Prisons are running can allow system administrators to detect and manually mitigate attacks. 

The prison library exposes methods that allow reading metrics. The prison library does not keep historical data.
The following is a list of all metrics exposed by the prison library:

- CPU (Accumulated time)
- Process Memory (Bytes used)
- Disk Usage (Bytes used)
- IO (Accumulated bytes)
- IO (Operations)

## Components ##

### Passive ###

The prison libraries and a local data store. The data store is a set of XML files that contain information about all prisons on the system (e.g. windows user name, cpu and memory quota, home path). The library can be invoked by scripts, services or other tools.

----------

### Active ###

There is a Prison Guard process running per container. It actively monitors a prison to make sure it's not overstepping the quotas and checks for the overall system health. All processes in the container will be killed if the Guard will terminate.

----------
