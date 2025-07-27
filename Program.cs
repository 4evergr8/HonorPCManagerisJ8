using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
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
        
        TrayIconApp.Run(); // 运行托盘程序
        
        
        
        var config = YamlConfigLoader.LoadConfig();
        var timeout= config.timeout;
        var startup = config.startup;
        var ec = config.ec;
        var data = config.data;
        var settings = config.settings;
        
        
        AutoStartHelper.SetAutoStart(startup);

        while (true)
        {
            
                

            // 调用Ring0Init，确保驱动加载
            try
            {
                EcAccess.Init();                      // 初始化驱动
                EcAccess.WriteEC(0x93, 99);         // 向 EC 寄存器 0x93 写入 0x55
            }
            catch (Exception ex)
            {
                MessageBox.Show("EC 写入失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            // === 你要执行的逻辑 ===
            Console.WriteLine("跑一次：" + DateTime.Now);
            

            Thread.Sleep(timeout);
        }
        
        
        
        
        
        
    



        

        



        
        
        
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