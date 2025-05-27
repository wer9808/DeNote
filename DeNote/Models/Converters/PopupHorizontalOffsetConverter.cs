using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DeNote.Models.Converters
{
    public class PopupHorizontalOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double buttonWidth)
            {
                // 팝업의 ActualWidth는 팝업이 열린 후에만 얻을 수 있으므로,
                // 이 컨버터에서는 Button의 너비를 기준으로 계산합니다.
                // 팝업의 내용(Border, StackPanel 등)의 너비를 팝업이 열리기 전에
                // 정확히 알 수 있다면 그 값을 사용하거나,
                // 팝업이 열린 후 MyPopup.ActualWidth를 직접 사용해야 합니다.
                // 여기서는 팝업의 너비가 200이라고 가정하겠습니다.
                // 팝업의 가로 중앙을 버튼의 가로 중앙에 맞추려면,
                // (버튼 너비 / 2) - (팝업 너비 / 2) 만큼 이동해야 합니다.
                double popupWidth = 200; // 팝업 내용의 고정된 너비라고 가정합니다.
                                         // 팝업 내용의 실제 너비를 동적으로 얻을 수 있다면 더 좋습니다.

                return (buttonWidth / 2) - (popupWidth / 2);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PopupHorizontalOffsetMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values 배열은 MultiBinding에 바인딩된 순서대로 값을 포함합니다.
            // 첫 번째 값은 PlacementTarget (버튼)의 ActualWidth
            // 두 번째 값은 팝업 콘텐츠의 ActualWidth

            if (values.Length < 2 || !(values[0] is double) || !(values[1] is double))
            {
                return 0.0; // 유효하지 않은 입력 시 기본값 반환
            }

            double targetWidth = (double)values[0];
            double popupWidth = (double)values[1];

            // 팝업을 버튼 중앙에 맞추기 위한 Offset 계산
            return (targetWidth - popupWidth) / 2;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
