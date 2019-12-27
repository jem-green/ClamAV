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

        int Notify(string applicationName, string eventName, string description);
        int Notify(string applicationName, string eventName, string description, Notify.PriorityOrder priority);
        int Verify();
        int Register(string applicationName, string eventName);
        int Subscribe();
        string ErrorDescription(int errorCode);

    }
}
