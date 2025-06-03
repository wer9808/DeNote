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
    public partial class QIDrawingToolPicker : UserControl
    {
        public QIDrawingToolPicker()
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
                typeof(QIDrawingToolPicker),
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
                typeof(QIDrawingToolPicker));

        public event RoutedPropertyChangedEventHandler<QIDrawingToolType> SelectedToolChanged
        {
            add { AddHandler(SelectedToolChangedEvent, value); }
            remove { RemoveHandler(SelectedToolChangedEvent, value); }
        }

        private static void OnSelectedToolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (QIDrawingToolPicker)d;
            var newToolType = (QIDrawingToolType)e.NewValue;
            var oldToolType = (QIDrawingToolType)e.OldValue;

            // RoutedPropertyChangedEventArgs를 사용하여 SelectedToolChanged 이벤트 발생
            var routedArgs = new RoutedPropertyChangedEventArgs<QIDrawingToolType>(oldToolType, newToolType, SelectedToolChangedEvent);
            control.RaiseEvent(routedArgs);
        }

    }
}
