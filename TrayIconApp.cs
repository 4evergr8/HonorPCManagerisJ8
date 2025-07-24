using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

static class TrayIconApp
{
    private static NotifyIcon notifyIcon;
    private static ContextMenuStrip trayMenu;
    private static Icon trayIcon;

    public static void Run()
    {
        // 加载嵌入的J8.png图标
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using (Stream stream = asm.GetManifestResourceStream("HonorPCManagerisJ8.J8.png"))
            using (Bitmap bmp = new Bitmap(stream))
            {
                trayIcon = Icon.FromHandle(bmp.GetHicon());
            }
        }
        catch
        {
            MessageBox.Show("无法加载嵌入图标 J8.png", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("退出", null, OnExit);

        notifyIcon = new NotifyIcon
        {
            Icon = trayIcon,
            Text = "J8 托盘程序",
            ContextMenuStrip = trayMenu,
            Visible = true
        };

        // 隐藏窗体防止程序退出
        var invisibleForm = new Form
        {
            ShowInTaskbar = false,
            WindowState = FormWindowState.Minimized,
            Visible = false
        };

        Application.Run(invisibleForm);

        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        trayMenu.Dispose();
        trayIcon.Dispose();
    }

    private static void OnExit(object sender, EventArgs e)
    {
        Application.Exit();
    }
}