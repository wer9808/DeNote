using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DeNote.Views.Controls
{
    /// <summary>
    /// ColorPickerPopup.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class QIColorPicker : UserControl
    {
        // 표시할 색상 목록
        public List<Color> ColorPreset { get; set; }

        public QIColorPicker()
        {
            InitializeComponent();
            InitializeColors();
            // DataContext를 자신으로 설정하여 XAML에서 Colors 속성 바인딩 가능하게 함
            // 하지만 XAML에서 ElementName=Root를 사용했으므로 필수는 아님.
            this.DataContext = this;
        }

        // HSL 값을 RGB로 변환하는 함수
        private static Color HslToRgb(double h, double s, double l)
        {
            h = h % 360; // Hue 값을 0-359 범위로 정규화
            double r, g, b;

            if (s == 0)
            {
                r = g = b = l; // 회색조 (채도가 0)
            }
            else
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;

                Func<double, double> HueToRgbComponent = t =>
                {
                    if (t < 0) t += 1;
                    if (t > 1) t -= 1;
                    if (t < 1.0 / 6) return p + (q - p) * 6 * t;
                    if (t < 1.0 / 2) return q;
                    if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
                    return p;
                };

                double hk = h / 360.0;
                r = HueToRgbComponent(hk + 1.0 / 3);
                g = HueToRgbComponent(hk);
                b = HueToRgbComponent(hk - 1.0 / 3);
            }

            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        // 새로운 색상 팔레트 생성 메소드
        private void InitializeColors()
        {
            ColorPreset = new List<Color>();
            const int rows = 10; // 행 수
            const int hueColumns = 12; // 색조 열 수

            double[] hues = { 0, 30, 60, 90, 120, 150, 180, 210, 240, 270, 300, 330 };

            for (int r = 0; r < rows; r++)
            {
                // 1. 회색조 열 추가 (첫 번째 열)
                double grayLightness = 1.0 - (r / (double)(rows - 1));
                // 맨 위는 완전 흰색, 맨 아래는 완전 검정색이 되도록 보정
                if (r == 0) grayLightness = 1.0;
                if (r == rows - 1) grayLightness = 0.0;
                ColorPreset.Add(HslToRgb(0, 0, grayLightness));

                // 2. 색조 열 추가
                for (int c = 0; c < hueColumns; c++)
                {
                    double hue = hues[c];
                    // 위에서 아래로 갈수록 명도(Lightness)를 낮춤
                    // 맨 위는 약간 밝게(0.9), 맨 아래는 어둡게(0.1)
                    double hueLightness = 0.90 - (r * (0.80 / (rows - 1)));

                    // 최상단은 해당 색조에서 가장 채도가 높은 색(L=0.5)으로 할 수도 있음.
                    // 여기서는 위에서 아래로 자연스럽게 어두워지는 방식을 선택.
                    // 만약 맨 위가 가장 '쨍한' 색이길 원하면 L 값을 다르게 조절.
                    // 예: double hueLightness = 0.5; if (r > 0) hueLightness = ...

                    ColorPreset.Add(HslToRgb(hue, 1.0, hueLightness));
                }
            }
        }

        #region SelectedColor Dependency Property

        // 선택된 색상을 저장하고 외부에 알리기 위한 Dependency Property
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(QIColorPicker),
                                        new PropertyMetadata(Colors.Black, OnSelectedColorPropertyChanged)); // 기본값: Black

        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        #endregion

        #region SelectedColorChanged Routed Event

        // SelectedColor가 변경될 때 발생하는 라우트된 이벤트 정의
        public static readonly RoutedEvent SelectedColorChangedEvent =
            EventManager.RegisterRoutedEvent("SelectedColorChanged", RoutingStrategy.Bubble,
                                              typeof(RoutedPropertyChangedEventHandler<Color>), typeof(QIColorPicker));

        // CLR 이벤트 래퍼
        public event RoutedPropertyChangedEventHandler<Color> SelectedColorChanged
        {
            add { AddHandler(SelectedColorChangedEvent, value); }
            remove { RemoveHandler(SelectedColorChangedEvent, value); }
        }

        // SelectedColorProperty의 변경 콜백에서 이벤트를 발생시키는 메서드
        private static void OnSelectedColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            QIColorPicker instance = (QIColorPicker)d;
            Color oldValue = (Color)e.OldValue;
            Color newValue = (Color)e.NewValue;
            instance.OnSelectedColorChanged(oldValue, newValue);
        }

        // SelectedColorChanged 이벤트를 발생시키는 보호된 가상 메서드
        protected virtual void OnSelectedColorChanged(Color oldValue, Color newValue)
        {
            RoutedPropertyChangedEventArgs<Color> args = new RoutedPropertyChangedEventArgs<Color>(oldValue, newValue, SelectedColorChangedEvent);
            RaiseEvent(args);
        }

        #endregion

        // 색상 버튼 클릭 이벤트 핸들러
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Color color)
            {
                SelectedColor = color; // 선택된 색상 업데이트
            }
        }
    }
}
