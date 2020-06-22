C:
cd \
cd "C:\Source\GIT\cs.net\ClamAV\ClamAVService\bin\Debug"
cd "c:\program files\ClamAV"
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\installutil ClamAVservice.exe
C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\installutil ClamAVservice.exe
sc config "ClamAV" start= delayed-auto
sc config "ClamAV" obj= SOLAR\administrator password= C0mplex2B0ld
ntrights +r SeServiceLogonRight -u SOLAR\administrator -m \\%COMPUTERNAME%
Net Start "ClamAV service"
pause
