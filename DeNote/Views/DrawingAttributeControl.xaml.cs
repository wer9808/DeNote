using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DeNote.Models;
using DeNote.ViewModels;

namespace DeNote.Views
{
    /// <summary>
    /// PenConfigControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DrawingAttributeControl : UserControl
    {
        private DrawingViewModel viewModel { get => DataContext as DrawingViewModel; }

        public DrawingAttributeControl()
        {
            InitializeComponent();
            ShowThicknessBtn.Visibility = Visibility.Collapsed;
            ShowColorPickerBtn.Visibility = Visibility.Collapsed;
            ShowShapePickerBtn.Visibility = Visibility.Collapsed;
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is DrawingViewModel newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
            else if (e.OldValue is DrawingViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            InitializeThicknessControl();
            InitializeColorControl();
            InitializeShapeControl();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(viewModel.CurrentDrawingObjectType))
            {
                InitializeThicknessControl();
                InitializeColorControl();
                InitializeShapeControl();
            }
            else if (e.PropertyName == nameof(viewModel.CurrentPenType))
            {
                InitializeThicknessControl();
                InitializeColorControl();
            }
            else if (e.PropertyName == nameof(viewModel.CurrentShapeDrawingType))
            {
                InitializeShapeControl();
            }
        }

        private void InitializeThicknessControl()
        {
            if (viewModel == null)
            {
                ShowThicknessBtn.Visibility = Visibility.Collapsed;
                ThicknessPopup.IsOpen = false;
                return;
            }

            ShowThicknessBtn.Visibility = Visibility.Visible;

            InitializeThicknessPopup();
        }

        private void InitializeThicknessPopup()
        {
            if (viewModel == null)
            {
                ThicknessPopup.IsOpen = false;
                return;
            }

            if (viewModel.CurrentDrawingObjectType == DrawingObjectType.Pen)
            {
                ThicknessSlider.Value = viewModel.CurrentPenType == PenType.Normal ? viewModel.NormalPenAttribute.StrokeThickness : viewModel.HighlighterAttribute.StrokeThickness;
                ThicknessSlider.Minimum = viewModel.CurrentPenType == PenType.Normal ? viewModel.NormalPenAttribute.MinStrokeThickness : viewModel.HighlighterAttribute.MinStrokeThickness;
                ThicknessSlider.Maximum = viewModel.CurrentPenType == PenType.Normal ? viewModel.NormalPenAttribute.MaxStrokeThickness : viewModel.HighlighterAttribute.MaxStrokeThickness;
            }
            else if (viewModel.CurrentDrawingObjectType == DrawingObjectType.Shape)
            {
                ThicknessSlider.Value = viewModel.ShapeDrawingAttribute.StrokeThickness;
                ThicknessSlider.Minimum = viewModel.ShapeDrawingAttribute.MinStrokeThickness;
                ThicknessSlider.Maximum = viewModel.ShapeDrawingAttribute.MaxStrokeThickness;
            }
        }

        private void InitializeColorControl()
        {
            if (viewModel == null)
            {
                ShowColorPickerBtn.Visibility = Visibility.Collapsed;
                ColorPickerPopup.IsOpen = false;
                return;
            }

            ShowColorPickerBtn.Visibility = Visibility.Visible;

            InitializeColorPickerPopup();
        }

        private void InitializeColorPickerPopup()
        {
            if (viewModel == null)
            {
                ColorPickerPopup.IsOpen = false;
                return;
            }

            if (viewModel.CurrentDrawingObjectType == DrawingObjectType.Pen)
            {
                if (viewModel.CurrentPenType == PenType.Normal)
                {
                    currentColorPreview.Fill = new SolidColorBrush(viewModel.NormalPenAttribute.Stroke);
                }
                else
                {
                    currentColorPreview.Fill = new SolidColorBrush(viewModel.HighlighterAttribute.Stroke);
                }
            }
            else if (viewModel.CurrentDrawingObjectType == DrawingObjectType.Shape)
            {
                currentColorPreview.Fill = new SolidColorBrush(viewModel.ShapeDrawingAttribute.Stroke);
            }
        }

        private void InitializeShapeControl()
        {
            if (viewModel == null || viewModel.CurrentDrawingObjectType != DrawingObjectType.Shape)
            {
                ShowShapePickerBtn.Visibility = Visibility.Collapsed;
                ShapePickerPopup.IsOpen = false;
                return;
            }
            ShowShapePickerBtn.Visibility = Visibility.Visible;

            if (viewModel.CurrentShapeDrawingType == ShapeDrawingType.Rectangle)
            {
                RectangleBtn.Background = Brushes.LightSkyBlue;
            }
            else if (viewModel.CurrentShapeDrawingType == ShapeDrawingType.Ellipse)
            {
                EllipseBtn.Background = Brushes.LightSkyBlue;
            }

        }

        private void InitializeShapePickerPopup()
        {
            if (viewModel == null)
            {
                ShapePickerPopup.IsOpen = false;
                return;
            }
            if (viewModel.CurrentShapeDrawingType == ShapeDrawingType.Rectangle)
            {
                RectangleBtn.Background = Brushes.LightSkyBlue;
                EllipseBtn.Background = Brushes.White;
            }
            else if (viewModel.CurrentShapeDrawingType == ShapeDrawingType.Ellipse)
            {
                RectangleBtn.Background = Brushes.White;
                EllipseBtn.Background = Brushes.LightSkyBlue;
            }
        }


        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (viewModel == null)
                return;

            if (viewModel.CurrentDrawingObjectType == DrawingObjectType.Pen)
            {
                if (viewModel.CurrentPenType == PenType.Normal)
                {
                    viewModel.NormalPenAttribute.StrokeThickness = e.NewValue;
                }
                else
                {
                    viewModel.HighlighterAttribute.StrokeThickness = e.NewValue;
                }
            }
            else if (viewModel.CurrentDrawingObjectType == DrawingObjectType.Shape)
            {
                viewModel.ShapeDrawingAttribute.StrokeThickness = e.NewValue;
            }
        }

        private void ColorRect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var rect = sender as Rectangle;
            if (rect != null)
            {
                var brush = rect.Fill as SolidColorBrush;
                var color = brush.Color;
                if (color != null)
                {
                    if (viewModel.CurrentDrawingObjectType == DrawingObjectType.Pen)
                    {
                        if (viewModel.CurrentPenType == PenType.Normal)
                        {
                            viewModel.NormalPenAttribute.Stroke = color;
                        }
                        else
                        {
                            viewModel.HighlighterAttribute.Stroke = color;
                        }
                    }
                    else if (viewModel.CurrentDrawingObjectType == DrawingObjectType.Shape)
                    {
                        viewModel.ShapeDrawingAttribute.Stroke = color;
                    }
                    currentColorPreview.Fill = brush;
                }
            }
        }

        private void ShapeBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                if (button == RectangleBtn)
                {
                    viewModel.CurrentShapeDrawingType = ShapeDrawingType.Rectangle;
                }
                else if (button == EllipseBtn)
                {
                    viewModel.CurrentShapeDrawingType = ShapeDrawingType.Ellipse;
                }
            }
        }

        private void ShowThicknessBtn_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null)
                return;

            ThicknessPopup.IsOpen = !ThicknessPopup.IsOpen;
            ColorPickerPopup.IsOpen = false;
            ShapePickerPopup.IsOpen = false;

            InitializeThicknessPopup();
        }

        private void ShowColorPickerBtn_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null)
                return;

            ColorPickerPopup.IsOpen = !ColorPickerPopup.IsOpen;
            ThicknessPopup.IsOpen = false;
            ShapePickerPopup.IsOpen = false;

            InitializeColorPickerPopup();
        }

        private void ShowShapePickerBtn_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null)
                return;

            ShapePickerPopup.IsOpen = !ShapePickerPopup.IsOpen;
            ThicknessPopup.IsOpen = false;
            ColorPickerPopup.IsOpen = false;

            InitializeShapePickerPopup();
        }
    }
}
