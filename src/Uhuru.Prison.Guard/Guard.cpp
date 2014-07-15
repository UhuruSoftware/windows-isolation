#include <windows.h>
#include <Psapi.h>
#include <Aclapi.h>

#include <iostream>
#include <string>
#include <vector>

using namespace std;

HANDLE hPrisonJob;
HANDLE hGuardJob;
HANDLE hDischargeEvent;

size_t processesIdListLength = 16384;
JOBOBJECT_BASIC_PROCESS_ID_LIST* processesInJobBuffer;

// The rate to check for memory
long checkRateMs = 100;

HANDLE GetPrisonJobObjectHandle(wstring name)
{
	HANDLE hJob = CreateJobObject(NULL, (wstring(L"Global\\") + name).c_str());

	if (hJob == NULL)
	{
		wclog << L"Could not open Job Object: " << name << " . Terminating guard." << endl;

		exit(GetLastError());
	}

	if (GetLastError() == ERROR_ALREADY_EXISTS)
	{
		// Job already existed

		wclog << L"Opened existing Job Object: " << name << endl;
	}
	else
	{
		// Job was created
		wclog << L"Created new Job Object: " << name << endl;
	}

	return hJob;
}

HANDLE CreateGuardJobObject(wstring name)
{
	HANDLE hJob = CreateJobObject(NULL, (wstring(L"Global\\") + name).c_str());

	if (hJob == NULL)
	{
		wclog << L"Could not open Safety Job Object: " << name << " . Terminating guard." << endl;

		exit(GetLastError());
	}

	if (GetLastError() == ERROR_ALREADY_EXISTS)
	{
		// Job already existed
		wclog << L"Unexpected state. Guard Job Object already exists: " << name << " . Terminating guard.";

		exit(-2);
	}
	else
	{
		// Job was created
		wclog << L"Created new Job Object: " << name << endl;

		// Activate JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE for Guard Job Object

		JOBOBJECT_EXTENDED_LIMIT_INFORMATION  jobOptions;
		ZeroMemory(&jobOptions, sizeof(jobOptions));
		jobOptions.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
		BOOL res = SetInformationJobObject(hJob, JobObjectExtendedLimitInformation, &jobOptions, sizeof(jobOptions));

		if (res == false)
		{
			wclog << L"Could not set JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE for job: " << name << " . Terminating guard." << endl;
			exit(GetLastError());
		}
	}

	return hJob;
}

void GetProcessIds(HANDLE hJob, vector<unsigned long> &processList)
{
	//Get a list of all the processes in this job.
	size_t listSize = sizeof(JOBOBJECT_BASIC_PROCESS_ID_LIST)+sizeof(ULONG)* processesIdListLength;

	if (processesInJobBuffer == NULL)
	{
		processesInJobBuffer = (JOBOBJECT_BASIC_PROCESS_ID_LIST*)LocalAlloc(
			LPTR,
			listSize);
	}

	BOOL ret = QueryInformationJobObject(
		hJob,
		JobObjectBasicProcessIdList,
		processesInJobBuffer,
		(DWORD)listSize,
		NULL);

	if (!ret)
	{
		wclog << L"Error queering for JobObjectBasicProcessIdList. Terminating guard.";
		exit(GetLastError());
	}

	if (processesInJobBuffer->NumberOfAssignedProcesses != processesInJobBuffer->NumberOfProcessIdsInList)
	{
		wclog << L"Processes id list is to small. Doubling the list size.";

		processesIdListLength *= 2;
		LocalFree(processesInJobBuffer);
		processesInJobBuffer = NULL;

		return GetProcessIds(hJob, processList);
	}

	processList.resize(processesInJobBuffer->NumberOfProcessIdsInList);

	for (size_t i = 0; i < processesInJobBuffer->NumberOfProcessIdsInList; i++)
	{
		processList[i] = (unsigned long)processesInJobBuffer->ProcessIdList[i];
	}


}


PROCESS_MEMORY_COUNTERS GetProcessIdMemoryInfo(unsigned long processId)
{
	HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);

	if (hProcess == NULL)
	{
		wclog << L"Could not open Process Id: " << processId << L" . Terminating guard. \n";

		exit(GetLastError());
	}

	PROCESS_MEMORY_COUNTERS counters;
	ZeroMemory(&counters, sizeof(counters));

	GetProcessMemoryInfo(hProcess, &counters, sizeof(counters));

	CloseHandle(hProcess);

	return counters;
}

void GetProcessesMemoryInfo(vector<unsigned long> &processList, vector<PROCESS_MEMORY_COUNTERS> &counters)
{

	counters.resize(processList.size());
	for (size_t i = 0; i < processList.size(); i++)
	{
		PROCESS_MEMORY_COUNTERS coutner = GetProcessIdMemoryInfo(processList[i]);
		counters[i] = coutner;
	}
}

long long GetTotalKernelMemory(vector<PROCESS_MEMORY_COUNTERS> &counters)
{
	long long sum = 0;

	for (size_t i = 0; i < counters.size(); i++)
	{
		sum += counters[i].QuotaPagedPoolUsage + counters[i].QuotaNonPagedPoolUsage;
	}
	return sum;
}

long long GetTotalWorkingSet(vector<PROCESS_MEMORY_COUNTERS> &counters)
{
	long long  sum = 0;

	for (size_t i = 0; i < counters.size(); i++)
	{
		sum += counters[i].WorkingSetSize;
	}
	return sum;
}

//LPWSTR *argv;
//int argc;
//
//void GetCommandLineArgs()
//{
//	argv = CommandLineToArgvW(GetCommandLineW(), &argc);
//	if (NULL == argv)
//	{
//		wclog << L"CommandLineToArgvW failed\n";
//		exit(1);
//	}
//
//}
//
//int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, PWSTR pCmdLine, int nCmdShow)
//{
//	GetCommandLineArgs();
//

bool Discharged()
{
	DWORD res = WaitForSingleObject(hDischargeEvent, 0);
	if (res == WAIT_OBJECT_0)
	{
		return true;
	}
	else if (res == WAIT_TIMEOUT)
	{
		return false;
	}
	else
	{
		return true;
	}
	
}

// Similar code here: https://chromium.googlesource.com/chromium/chromium/+/master/sandbox/src/acl.cc
void SetDefaultDacl()
{
	HANDLE curProc = GetCurrentProcess();
	HANDLE curToken;
	BOOL res = OpenProcessToken(curProc, TOKEN_ADJUST_DEFAULT | TOKEN_READ, &curToken);

	if (res == false)
	{
		wclog << L"Error on OpenProcessToken." << endl;
		exit(GetLastError());
	}

	DWORD defDaclLen;
	GetTokenInformation(curToken, TokenDefaultDacl, NULL, 0, &defDaclLen);


	TOKEN_DEFAULT_DACL *defaultDacl = (TOKEN_DEFAULT_DACL *)malloc(defDaclLen);
	
	res = GetTokenInformation(curToken, TokenDefaultDacl, defaultDacl, defDaclLen, &defDaclLen);
	if (res == false)
	{
		wclog << L"Error on GetTokenInformation." << endl;
		exit(GetLastError());
	}
	

	EXPLICIT_ACCESS eaccess;
	BuildExplicitAccessWithName(&eaccess, L"BUILTIN\\Administrators", GENERIC_ALL, GRANT_ACCESS, 0);
	
	PACL newDacl = NULL;
	DWORD dres = SetEntriesInAcl(1, &eaccess, defaultDacl->DefaultDacl, &newDacl);
	if (dres != ERROR_SUCCESS)
	{
		wclog << L"Error on SetEntriesInAcl." << endl;
		exit(dres);
	}

	TOKEN_DEFAULT_DACL newDefaultDacl = { 0 };
	newDefaultDacl.DefaultDacl = newDacl;
	res = SetTokenInformation(curToken, TokenDefaultDacl, &newDefaultDacl, sizeof(newDefaultDacl));
	if (res == false)
	{
		wclog << L"Error on SetTokenInformation." << endl;
		exit(GetLastError());
	}

	LocalFree(newDacl);
	free(defaultDacl);
}

int wmain(int argc, wchar_t **argv)
{
	if (argc != 3)
	{
		wcerr << L"Usage: Uhuru.Prison.Guard.exe <job_object_name> <memory_bytes_quota>\n";
		exit(-1);
	}

	// Get arguments
	wstring jobName(argv[1]);
	wstring memoryQuotaString(argv[2]);
	long memoryQuota = _wtol(memoryQuotaString.c_str());

	SetDefaultDacl();

	wstring dischargeEventName = wstring(L"Global\\discharge-") + jobName;

	hPrisonJob = GetPrisonJobObjectHandle(jobName);
	hGuardJob = CreateGuardJobObject(jobName + L"-guard");

	hDischargeEvent = CreateEvent(NULL, true, false, dischargeEventName.c_str());

	vector<PROCESS_MEMORY_COUNTERS> counters;
	vector<unsigned long> processList;

	for (;;)
	{
		if (Discharged())
		{
			wclog << L"Guard is discharged. Shutting down." << endl;

			break;
		}

		if (memoryQuota > 0)
		{

			processList.clear();
			counters.clear();

			GetProcessIds(hPrisonJob, processList);

			// TODO: fail if the hPrisonJob list is not in with hGuardJob +/- 1 processes

			GetProcessesMemoryInfo(processList, counters);

			long long kernelMemoryUsage = GetTotalKernelMemory(counters);
			long long workingSetUsage = GetTotalWorkingSet(counters);
			long long totalMemUsage = kernelMemoryUsage + workingSetUsage;

			if (totalMemUsage >= memoryQuota)
			{
				// all hell breaks loose
				// TODO: consider killing the prison by setting the job memory limit to 0
				TerminateJobObject(hPrisonJob, -1);

				wclog << L"Quota exceeded. Terminated Job: " << jobName << " at " << totalMemUsage << " bytes." << endl;
			}

		}

		// If waiting for multiple events is required use:
		// http://msdn.microsoft.com/en-us/library/windows/desktop/ms687003 or
		// http://msdn.microsoft.com/en-us/library/windows/desktop/ms687008
		Sleep(checkRateMs);
	}

	CloseHandle(hGuardJob);
	CloseHandle(hDischargeEvent);

	return 0;
}
