C:
cd \
cd "C:\program files\ClamAV"
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\installutil ClamAVservice.exe
C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\installutil ClamAVservice.exe
sc config ClamAV start= delayed-auto
Net Start "ClamAV service"
pause
