using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DeNote.Models;
using SkiaSharp;
using System.ComponentModel;
using Wpf.Ui.Tray.Controls;
using DeNote.Services;
using Brushes = System.Windows.Media.Brushes;
using DeNote.Models.Drawing;
using DeNote.Models.DeNote.Models;
using Color = System.Windows.Media.Color;

namespace DeNote.Views
{
    /// <summary>
    /// OverlayWindow.xaml에 대한 상호 작용 논리
    /// </summary>

    public partial class OverlayWindow : Window, IDisposable
    {
        private const int HOTKEY_ID = 9000;
        private const int WM_HOTKEY = 0x0312;

        // Win32 API 선언
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_F = 0x46;  // F 키의 가상 키 코드

        private IntPtr _windowHandle;
        private HwndSource _source;

        public OverlayWindow()
        {
            InitializeComponent();

            this.Loaded += OverlayWindow_Loaded;
            this.Closing += OverlayWindow_Closing;
            this.Activated += OverlayWindow_Activated;

            DrawingCanvasControl.CloseRequested += (s, e) => HideToTray();
        }

        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 창 핸들 가져오기
            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            // 글로벌 핫키 등록 (Ctrl+Shift+F)
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_F);
        }

        private void OverlayWindow_Closing(object? sender, CancelEventArgs e)
        {
            // 핫키 등록 해제
            UnregisterHotKey(_windowHandle, HOTKEY_ID);

            // HwndSource 정리
            _source?.RemoveHook(HwndHook);
            _source?.Dispose();
        }

        private void OverlayWindow_Activated(object? sender, EventArgs e)
        {

        }

        private void ToggleWindowVisibility()
        {
            if (Visibility == Visibility.Visible)
            {
                HideToTray();
            }
            else
            {
                ShowFromTray();
            }
        }

        private void HideToTray()
        {
            // 창 숨기기
            Hide();
        }

        private void ShowFromTray()
        {
            // 창 보이기
            Show();
            DrawingCanvasControl.ReloadCanvas();
            WindowState = WindowState.Maximized;
            Activate();
        }

        // 윈도우 위치 설정 (예: 화면 오른쪽 상단)
        public void PositionWindow()
        {
            this.Left = 0;
            this.Top = 0;

            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
        }

        private void NotifyIcon_Show(object sender, RoutedEventArgs e)
        {
            ShowFromTray();
        }

        private void NotifyIcon_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 핫키 메시지 처리
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                ToggleWindowVisibility();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            DrawingCanvasControl?.Dispose();
            // 핫키 등록 해제
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            // HwndSource 정리
            _source?.RemoveHook(HwndHook);
            _source?.Dispose();
        }
    }
}
