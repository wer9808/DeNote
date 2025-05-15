using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace DeNote.Utils
{
    public static class UIElementExtensions
    {
        /// <summary>
        /// 주어진 UIElement의 부모 Window를 찾아 반환합니다.
        /// </summary>
        /// <param name="element">찾을 대상 UIElement</param>
        /// <returns>부모 Window 또는 찾지 못한 경우 null</returns>
        public static Window? GetParentWindow(this UIElement element)
        {
            if (element == null)
                return null;

            // 현재 요소의 부모 찾기
            DependencyObject parent = VisualTreeHelper.GetParent(element);

            // 부모가 없으면 종료
            if (parent == null)
                return null;

            // 부모가 Window라면 반환
            if (parent is Window window)
                return window;

            // 부모가 UIElement라면 재귀적으로 호출
            if (parent is UIElement parentElement)
                return GetParentWindow(parentElement);

            // 찾지 못한 경우
            return null;
        }

        /// <summary>
        /// 주어진 UIElement의 부모 Window를 찾아 반환합니다(Application.Current.MainWindow를 사용한 대체 방법).
        /// </summary>
        /// <param name="element">찾을 대상 UIElement</param>
        /// <returns>부모 Window 또는 찾지 못한 경우 MainWindow</returns>
        public static Window GetParentWindowOrMainWindow(this UIElement element)
        {
            Window window = GetParentWindow(element);

            // 부모 Window를 찾지 못했으면 MainWindow 반환
            return window ?? Application.Current.MainWindow;
        }
    }
}
