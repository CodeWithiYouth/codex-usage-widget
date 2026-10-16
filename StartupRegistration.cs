using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace WindowsUsageWidget
{
    public class StartupRegistration
    {
        const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
        const string ValueName="CodexUsageWidget";
        public virtual bool Enabled
        {
            get
            {
                using(var key=Registry.CurrentUser.OpenSubKey(RunKey))
                    return key!=null&&!string.IsNullOrWhiteSpace(key.GetValue(ValueName) as string);
            }
        }
        public static string CommandFor(string executablePath)
        {
            if(string.IsNullOrWhiteSpace(executablePath)||executablePath.IndexOf('"')>=0)throw new ArgumentException("无效的程序路径。","executablePath");
            return "\""+System.IO.Path.GetFullPath(executablePath)+"\"";
        }
        public virtual void SetEnabled(bool enabled)
        {
            if(enabled)
            {
                using(var process=Process.GetCurrentProcess())
                using(var key=Registry.CurrentUser.CreateSubKey(RunKey))
                    key.SetValue(ValueName,CommandFor(process.MainModule.FileName),RegistryValueKind.String);
            }
            else
            {
                using(var key=Registry.CurrentUser.OpenSubKey(RunKey,true))
                    if(key!=null)key.DeleteValue(ValueName,false);
            }
        }
    }
}
