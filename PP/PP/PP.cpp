// PP.cpp : �������̨Ӧ�ó������ڵ㡣
//

#include "stdafx.h"
#include <windows.h>
int  CreateRun();


int _tmain(int argc, _TCHAR* argv[])
{
	/* ����console  start */
	/*
	HWND hwnd;
	hwnd = FindWindow("ConsoleWindowClass", NULL);	//���������ڵ������ʹ�������ƥ��ָ�����ַ���,�������Ӵ��ڡ�
	if (hwnd)
	{
	ShowWindow(hwnd, SW_HIDE);				//����ָ�����ڵ���ʾ״̬
	}
	//MessageBox(NULL, "Hello", "Notice", MB_OK);
	*/
	
	/* ����console  end */


	// ��Ҫ
	HANDLE event1;
	HANDLE event2;
	HANDLE event3;
	STARTUPINFO si = { 0 };
	PROCESS_INFORMATION pi = { 0 };
	event1 = CreateEvent(NULL, FALSE, TRUE, "Global\\p1");// ��ȫ���ԡ���λ��ʽ����ʼ״̬����������

	while (true)
	{
		if (!(event3 = OpenEvent(EVENT_MODIFY_STATE, FALSE, "Global\\p3")))
		{
			if (!CreateProcess("PP2.exe",
				NULL,
				NULL,
				NULL,
				NULL,
				CREATE_NEW_CONSOLE,
				NULL,
				NULL,
				&si,
				&pi
				))
			{
				printf("���ػ�����ʧ�ܣ�\n");
			}
			else printf("���ػ����̳ɹ���PID:%d \n", pi.dwProcessId);
			WaitForSingleObject(pi.hProcess, INFINITE);
			printf("�ػ����̱��رգ�\n");
			CloseHandle(pi.hProcess);
		}
		CloseHandle(event3);
		Sleep(2000);
		/*
		if (!(event2 = OpenEvent(EVENT_MODIFY_STATE, FALSE, "Global\\p2")))
		{
			if (!CreateProcess("C:\\Users\\tabjin\\Desktop\\WindowsFormsApplication1.exe",
				NULL,
				NULL,
				NULL,
				NULL,
				CREATE_NEW_CONSOLE,
				NULL,
				NULL,
				&si,
				&pi
				))
			{
				printf("���ػ�����ʧ�ܣ�\n");
			}
			else printf("���ػ����̳ɹ�����PID:%d \n", pi.dwProcessId);
			WaitForSingleObject(pi.hProcess, INFINITE);
			printf("�ػ����̱��رգ�\n");
			CloseHandle(pi.hProcess);
		}
		CloseHandle(event2);
		Sleep(1000);
		*/
		
	}
	/*while (true)
	{
		if (!(event2 = OpenEvent(EVENT_MODIFY_STATE, FALSE, "Global\\p2")))
		{
			if (!CreateProcess("C:\\Users\\tabjin\\Desktop\\WindowsFormsApplication1.exe",
				NULL,
				NULL,
				NULL,
				NULL,
				CREATE_NEW_CONSOLE,
				NULL,
				NULL,
				&si,
				&pi
				))
			{
				printf("���ػ�����ʧ�ܣ�\n");
			}
			else printf("���ػ����̳ɹ���PID:%d \n", pi.dwProcessId);
			WaitForSingleObject(pi.hProcess, INFINITE);
			printf("�ػ����̱��رգ�\n");
			CloseHandle(pi.hProcess);
		}
		CloseHandle(event2);
		Sleep(1000);
	}
	*/

	
	//CreateRun();
	return 0;
}

/******************************************************************************************
Function:        autostart
Description:     ���ó��򿪻�������
*******************************************************************************************/
//��������
int  CreateRun()
{
	//������´���
	HKEY   hKey;
	char pFileName[MAX_PATH] = { 0 };
	//�õ����������ȫ·�� 
	DWORD dwRet = GetModuleFileNameW(NULL, (LPWCH)pFileName, MAX_PATH);
	//�ҵ�ϵͳ�������� 
	LPCTSTR lpRun = _T("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
	//��������Key 
	long lRet = RegOpenKeyEx(HKEY_LOCAL_MACHINE, lpRun, 0, KEY_WRITE, &hKey);
	if (lRet == ERROR_SUCCESS)
	{
		//���ע��
		RegSetValueEx(hKey, _T("WindowsFormsApplication1"), 0, REG_SZ, (const BYTE*)(LPCSTR)pFileName, MAX_PATH);
		RegCloseKey(hKey);
	}
	return 0;
}
//ȡ����������
int DeleteRun()
{
	//������´���
	HKEY   hKey;
	char pFileName[MAX_PATH] = { 0 };
	//�õ����������ȫ·�� 
	DWORD dwRet = GetModuleFileNameW(NULL, (LPWCH)pFileName, MAX_PATH);
	//�ҵ�ϵͳ�������� 
	LPCTSTR lpRun = _T("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
	//��������Key 
	long lRet = RegOpenKeyEx(HKEY_LOCAL_MACHINE, lpRun, 0, KEY_WRITE, &hKey);
	if (lRet == ERROR_SUCCESS)
	{
		//ɾ��ע��
		RegDeleteValue(hKey, _T("WindowsFormsApplication1"));
		RegCloseKey(hKey);
	}
	return 0;
}

