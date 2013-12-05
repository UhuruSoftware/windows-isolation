#include <windows.h>
#include <Psapi.h>

#include <iostream>
#include <string>
#include <vector>

using namespace std;

HANDLE hGuardedJob;
HANDLE hDischargeEvent;

size_t processesIdListLength = 16384;
JOBOBJECT_BASIC_PROCESS_ID_LIST* processesInJobBuffer;

// The rate to check for memory
long checkRateMs = 100;

HANDLE GetJobObjectHandle(wstring name)
{
	hGuardedJob = CreateJobObject(NULL, name.c_str());

	if (hGuardedJob == NULL)
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

	return hGuardedJob;
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

	wstring dischargeEventName = wstring(L"discharge-") + jobName;

	hGuardedJob = GetJobObjectHandle(jobName);
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

		processList.clear();
		counters.clear();

		GetProcessIds(hGuardedJob, processList);
		GetProcessesMemoryInfo(processList, counters);

		long long kernelMemoryUsage = GetTotalKernelMemory(counters);
		long long workingSetUsage = GetTotalWorkingSet(counters);
		long long totalMemUsage = kernelMemoryUsage + workingSetUsage;

		if (totalMemUsage >= memoryQuota)
		{
			// all hell breaks loose
			TerminateJobObject(hGuardedJob, -1);

			wclog << L"Quota exceeded. Terminated Job: " << jobName << " at " << totalMemUsage << " bytes." << endl;
		}

		// If waiting for multiple events is required use:
		// http://msdn.microsoft.com/en-us/library/windows/desktop/ms687003 or
		// http://msdn.microsoft.com/en-us/library/windows/desktop/ms687008
		Sleep(checkRateMs);
	}

	CloseHandle(hGuardedJob);
	CloseHandle(hDischargeEvent);

	return 0;
}
