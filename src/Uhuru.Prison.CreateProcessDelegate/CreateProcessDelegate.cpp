#include <windows.h>
#include <map>
#include <string>
#include <iostream>
#include <cstdio>

using namespace std;

map<wstring, wstring> * GetEnvs()
{
	map<wstring, wstring> *envsMap = new map<wstring, wstring>;

	wchar_t *envs = GetEnvironmentStringsW();

	wchar_t *curLine = envs;
	while(1)
	{
		if (curLine[0] == 0)
		{
			break;
		}
		wstring envName;
		wchar_t *envValue = 0;
		wchar_t *curChar = curLine;

		while (1)
		{
			if (curChar[0] == 0) break;

			if (curChar[0] == L'=')
			{
				envName.assign(curLine, curChar - curLine);
				envValue = curChar + 1;

				break; 
			}

			curChar++;
		}

		while (1)
		{
			if (curChar[0] == 0) break;
			curChar++;
		}

		if (envValue == 0) exit(1);
		(*envsMap)[envName] = *(new wstring(envValue, curChar - envValue));

		curLine = curChar + 1;
	}


	return envsMap;
}

// Input will be recited with by stdin plus environment variables and output will be provided to stdout.
// If successful the return code is 0. If there was an error the return code is the error message.
int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, PWSTR pCmdLine, int nCmdShow)
{
	wstring inputCommand;

	// This will block the current process and allow the Prison Manager to tag it with the Job Object.
	getline(std::wcin, inputCommand);

	// If the input command is not CreateProcess then return the error message 1.
	if (inputCommand != L"CreateProcess")
	{
		return 1;
	}

	// Parse the environment variables.

	map<wstring, wstring> *envs = GetEnvs();
	wstring method = envs->at(L"Method");

	wstring logonFlagsStr = envs->at(L"LogonFlags");
	int logonFlags = _wtoi(logonFlagsStr.c_str());
	wstring commandLine = envs->at(L"CommandLine");
	wstring creationFlagsStr = envs->at(L"CreationFlags");
	int creationFlags = _wtoi(creationFlagsStr.c_str());

	wstring currentDirectoryStr = envs->at(L"CurrentDirectory");
	const wchar_t * currentDirectory = currentDirectoryStr.c_str();
	if (currentDirectoryStr == L"") currentDirectory = NULL;

	wstring desktopStr = envs->at(L"Desktop");
	const wchar_t * desktop = desktopStr.c_str();
	if (desktopStr == L"") desktop = NULL;


	STARTUPINFOW suInfo;
	ZeroMemory(&suInfo, sizeof(STARTUPINFOW));
	suInfo.cb = sizeof(STARTUPINFOW);
	suInfo.lpDesktop = (wchar_t *) desktop;

	PROCESS_INFORMATION processInfo;
	ZeroMemory(&processInfo, sizeof(PROCESS_INFORMATION));

	if (method == L"CreateProcessWithLogonW")
	{
		wstring username = envs->at(L"pUsername");
		wstring domain = envs->at(L"Domain");
		wstring password = envs->at(L"Password");

		BOOL createProcessSuccess = CreateProcessWithLogonW(
			username.c_str(), domain.c_str(), password.c_str(), logonFlags,
			NULL, (wchar_t  *)commandLine.c_str(), creationFlags, NULL, currentDirectory, &suInfo, &processInfo
			);

		if (!createProcessSuccess)
		{
			int error = GetLastError();
			return error;
		}
	}

	if (method == L"CreateProcessWithTokenW")
	{
		wstring tokenStr = envs->at(L"Token");
		HANDLE token = (HANDLE)_wtoi(tokenStr.c_str());

		BOOL createProcessSuccess = CreateProcessWithTokenW(
			token, logonFlags,
			NULL, (wchar_t  *)commandLine.c_str(), creationFlags, NULL, currentDirectory, &suInfo, &processInfo
			);

		if (!createProcessSuccess)
		{
			int error = GetLastError();
			return error;
		}
	}

	CloseHandle(processInfo.hProcess);
	CloseHandle(processInfo.hThread);

	// Return the worker process PID to stdout
	wstring workerPid = to_wstring(processInfo.dwProcessId);
	wcout << workerPid << endl;

	return 0;
}