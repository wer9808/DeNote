using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using System.Windows.Interop;
using System.Windows;
using System.IO;

namespace DeNote.Services
{
    public class ScreenCaptureService
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // ShowWindow 명령 상수
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        public async Task<SKBitmap?> CaptureScreenWithoutWindowAsync(IntPtr windowHandle)
        {
            return await Task.Run(() =>
            {
                SKBitmap result = null;

                try
                {
                    // 1. 윈도우 위치 기록
                    GetWindowRect(windowHandle, out RECT windowRect);

                    // 2. 윈도우를 일시적으로 숨기기
                    ShowWindow(windowHandle, SW_HIDE);

                    // 3. 화면 갱신을 위한 짧은 대기
                    System.Threading.Thread.Sleep(100);

                    // 4. 전체 화면 캡처
                    int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
                    int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

                    using (Bitmap screenBitmap = new Bitmap(screenWidth, screenHeight))
                    {
                        using (Graphics g = Graphics.FromImage(screenBitmap))
                        {
                            g.CopyFromScreen(0, 0, 0, 0, screenBitmap.Size);
                        }

                        // 5. Bitmap을 SKBitmap으로 변환
                        using (MemoryStream ms = new MemoryStream())
                        {
                            screenBitmap.Save(ms, ImageFormat.Png);
                            ms.Position = 0;
                            result = SKBitmap.Decode(ms);
                        }
                    }

                    // 6. 윈도우 복원
                    ShowWindow(windowHandle, SW_SHOW);
                }
                catch (Exception ex)
                {
                    // 오류 발생 시 윈도우 복원 시도
                    ShowWindow(windowHandle, SW_SHOW);
                    Console.WriteLine($"Screen capture error: {ex.Message}");
                }

                return result;
            });
        }
    }
}
