C:
cd \
cd C:\Users\jeremy.SOLAR\Dropbox\Source\Projects\ClamAV\ClamAVService\bin\Debug
cd "C:\program files\ClamAV"
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\installutil ClamAVservice.exe
sc config ClamAV start= delayed-auto
Net Start "ClamAV service"
pause
