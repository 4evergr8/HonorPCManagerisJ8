using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main()
    {
        // 判断管理员权限
        if (!IsAdministrator())
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Assembly.GetExecutingAssembly().Location,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(startInfo);
            }
            catch
            {
                MessageBox.Show("需要管理员权限才能运行此程序。", "权限不足", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // 调用Ring0Init，确保驱动加载
        

        TrayIconApp.Run(); // 运行托盘程序
    }

    static bool IsAdministrator()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}