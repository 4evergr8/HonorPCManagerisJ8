using Microsoft.Win32;
using System;

public static class AutoStartHelper
{
    /// <summary>
    /// 设置当前程序开机自启
    /// </summary>
    /// <param name="enable">是否启用</param>
    public static void SetAutoStart(bool enable)
    {
        string appName = AppDomain.CurrentDomain.FriendlyName; // 可执行文件名
        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

        if (enable)
        {
            key.SetValue(appName, $"\"{exePath}\"");
        }
        else
        {
            if (key.GetValue(appName) != null)
            {
                key.DeleteValue(appName);
            }
        }
    }
}