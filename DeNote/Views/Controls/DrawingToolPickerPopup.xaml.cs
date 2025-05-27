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
using DeNote.Models.Drawing;

namespace DeNote.Views.Controls
{
    /// <summary>
    /// DrawingToolPickerPopup.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DrawingToolPickerPopup : UserControl
    {
        public DrawingToolPickerPopup()
        {
            InitializeComponent();
        }


        private void DrawingToolButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tagString)
            {
                if (Enum.TryParse<QIDrawingToolType>(tagString, out var toolType))
                {
                    SelectedTool = toolType;
                }
            }
        }

        public static readonly DependencyProperty SelectedToolProperty =
            DependencyProperty.Register(
                nameof(SelectedTool),
                typeof(QIDrawingToolType),
                typeof(DrawingToolPickerPopup),
                new FrameworkPropertyMetadata(
                    QIDrawingToolType.Pen, // 기본값
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedToolPropertyChanged));

        public QIDrawingToolType SelectedTool
        {
            get { return (QIDrawingToolType)GetValue(SelectedToolProperty); }
            set { SetValue(SelectedToolProperty, value); }
        }

        public static readonly RoutedEvent SelectedToolChangedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(SelectedToolChanged),
                RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<QIDrawingToolType>),
                typeof(DrawingToolPickerPopup));

        public event RoutedPropertyChangedEventHandler<QIDrawingToolType> SelectedToolChanged
        {
            add { AddHandler(SelectedToolChangedEvent, value); }
            remove { RemoveHandler(SelectedToolChangedEvent, value); }
        }

        private static void OnSelectedToolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DrawingToolPickerPopup)d;
            var newToolType = (QIDrawingToolType)e.NewValue;
            var oldToolType = (QIDrawingToolType)e.OldValue;

            // RoutedPropertyChangedEventArgs를 사용하여 SelectedToolChanged 이벤트 발생
            var routedArgs = new RoutedPropertyChangedEventArgs<QIDrawingToolType>(oldToolType, newToolType, SelectedToolChangedEvent);
            control.RaiseEvent(routedArgs);
        }


        #region IsOpen Dependency Property

        // 팝업의 열림/닫힘 상태를 제어하기 위한 Dependency Property
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(DrawingToolPickerPopup),
                                        new PropertyMetadata(false));

        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        #endregion

        #region PlacementTarget Dependency Property (추가된 부분)

        // Popup의 PlacementTarget을 외부에서 설정할 수 있도록 Dependency Property 추가
        public static readonly DependencyProperty PlacementTargetProperty =
            DependencyProperty.Register("PlacementTarget", typeof(UIElement), typeof(DrawingToolPickerPopup),
                                        new PropertyMetadata(null));

        public UIElement PlacementTarget
        {
            get { return (UIElement)GetValue(PlacementTargetProperty); }
            set { SetValue(PlacementTargetProperty, value); }
        }

        #endregion

        #region Placement Dependency Property

        // Popup의 Placement를 외부에서 설정할 수 있도록 Dependency Property 추가
        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.Register("Placement", typeof(PlacementMode), typeof(DrawingToolPickerPopup),
                                        new PropertyMetadata(PlacementMode.Bottom));

        public PlacementMode Placement
        {
            get { return (PlacementMode)GetValue(PlacementProperty); }
            set { SetValue(PlacementProperty, value); }
        }

        #endregion

        #region HorizontalOffset Dependency Property

        // Popup의 수평 오프셋을 제어하기 위한 Dependency Property
        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register("HorizontalOffset", typeof(double), typeof(DrawingToolPickerPopup),
                                        new PropertyMetadata(0.0));

        public double HorizontalOffset
        {
            get { return (double)GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }
        #endregion

        #region VerticalOffset Dependency Property

        // Popup의 수직 오프셋을 제어하기 위한 Dependency Property
        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.Register("VerticalOffset", typeof(double), typeof(DrawingToolPickerPopup),
                                        new PropertyMetadata(0.0));

        public double VerticalOffset
        {
            get { return (double)GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }
        #endregion

    }
}
