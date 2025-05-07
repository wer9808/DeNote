using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using DeNote.ViewModels;

namespace DeNote.Views
{
    /// <summary>
    /// ToolbarWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ToolbarWindow : Window
    {

        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_F = 0x46; // F 키의 가상 키 코드

        private IntPtr _windowHandle;
        private HwndSource _source;

        private OverlayWindow _overlayWindow;
        private DrawingViewModel _viewModel { get => DataContext as DrawingViewModel; }

        public ToolbarWindow(DrawingViewModel drawingViewModel)
        {
            InitializeComponent();

            DataContext = drawingViewModel;

            _overlayWindow = new OverlayWindow(drawingViewModel);

            this.Loaded += ToolbarWindow_Loaded;
            this.Closed += ToolbarWindow_Closed;
        }

        private void ToolbarWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            // 글로벌 핫키 등록
            RegisterHotKey();

            // OverlayWindow 위치 설정 및 표시
            _overlayWindow.PositionWindow();
            _overlayWindow.Show();

            // MainWindow가 활성화되면 OverlayWindow도 활성화
            this.Activated += (s, args) =>
            {
                if (!_overlayWindow.IsVisible)
                {
                    _overlayWindow.Show();
                }
            };
        }


        private void ToolbarWindow_Closed(object? sender, EventArgs e)
        {
            // 오버레이 윈도우 종료
            _overlayWindow?.Close();

            // 핫키 해제
            UnregisterHotKey();

            if (_source != null)
            {
                _source.RemoveHook(HwndHook);
                _source = null;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // 기본 닫기 동작 취소하고 트레이로 최소화
            e.Cancel = true;
            MinimizeToTray();
        }

        private void MinimizeToTray()
        {
            // 메인 윈도우 숨기기
            this.Hide();

            // 오버레이 윈도우도 함께 숨기기
            if (_overlayWindow != null && _overlayWindow.IsVisible)
            {
                _overlayWindow.Hide();
            }
        }

        private void ShowWindow()
        {
            // 메인 윈도우 표시
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();

            // 오버레이 윈도우도 함께 표시
            if (_overlayWindow != null && !_overlayWindow.IsVisible)
            {
                _overlayWindow.Show();
                _overlayWindow.Activate();
            }
        }


        #region 글로벌 핫키 관련 코드

        // Win32 API 가져오기
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;

        private void RegisterHotKey()
        {
            // Ctrl+Shift+F 조합 등록
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_F);
        }

        private void UnregisterHotKey()
        {
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                // 핫키가 눌렸을 때
                ShowWindow();
                handled = true;
            }
            return IntPtr.Zero;
        }

        #endregion

        private void NotifyIcon_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
