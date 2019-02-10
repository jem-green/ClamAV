C:
cd \
cd C:\Users\jeremy.SOLAR\Dropbox\Source\Projects\ClamAV\ClamAVService\bin\Debug
cd "C:\program files\ClamAV"
Net Stop "ClamAV service""
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\installutil /u ClamAVservice.exe
C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\installutil /u ClamAVservice.exe
pause
