using System;
using System.Collections.Generic;
using System.Text;

namespace ClamAVLibrary
{
    public interface INotify
    {
   
        int Priority
        {
            set;
        }

        int Notify(string applicationName, string eventName, string description, int priority);
        int Notify(string applicationName, string eventName, string description);
        int Verify();
        string ErrorDescription(int errorCode);

    }
}
