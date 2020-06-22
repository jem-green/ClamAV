C:
cd \
cd "C:\Source\GIT\cs.net\ClamAV\ClamAVService\bin\Debug"
cd "c:\program files\ClamAV"
Net Stop "ClamAV service""
C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\installutil /u ClamAVservice.exe
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\installutil /u ClamAVservice.exe
pause
