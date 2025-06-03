using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using DeNote.Models.Drawing;

namespace DeNote.Views.Controls
{
    /// <summary>
    /// QIThicknessPicker.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 

    public class ThicknessPresetItem
    {
        public double Thickness { get; set; }
    }

    public partial class QIThicknessPicker : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 두께 선택을 위한 프리셋 값들

        // QIDrawingToolType에 따라 두께 프리셋을 정의합니다.
        private List<double> PenThicknessPresets = new List<double> { 2, 3, 5, 8, 10, 15 };
        private List<double> HighlighterThicknessPresets = new List<double> { 5, 10, 15 };
        private List<double> EraserThicknessPresets = new List<double> { 5, 10, 20, 30 };
        private List<double> ShapeThicknessPresets = new List<double> { 1, 2, 3, 5 };

        // QIDrawingToolType에 따라 두께 프리셋을 반환하는 메소드가 필요합니다.

        private Dictionary<QIDrawingToolType, double> _toolThickness = new Dictionary<QIDrawingToolType, double>();

        public QIThicknessPicker()
        {
            InitializeComponent();
            // DataContext를 자신으로 설정하여 XAML에서 ThicknessPresets 속성 바인딩 가능하게 함
            this.DataContext = this;
            // SelectedTool이 변경되면 두께 프리셋을 업데이트하도록 초기화 시점에도 호출
            UpdateThicknessPresets();
        }

        private bool _isToolInitialized = false;

        private QIDrawingToolType _selectedTool = QIDrawingToolType.Pen;
        // 선택된 도구를 나타내는 속성
        public QIDrawingToolType SelectedTool
        {
            get { return _selectedTool; }
            set
            {
                if (!_isToolInitialized)
                {
                    _selectedTool = value;
                    OnPropertyChanged(nameof(SelectedTool));
                    UpdateThicknessPresets();
                    if (ThicknessPreset.Count > 0)
                    {
                        // 프리셋이 비어있지 않다면 첫 번째 아이템을 선택
                        SelectedItem = ThicknessPreset.First();
                    }
                    else
                    {
                        SelectedItem = null; // 프리셋이 없으면 선택 해제
                    }
                    _isToolInitialized = true;
                }
                else if (_selectedTool != value)
                {
                    if (_selectedItem != null) _toolThickness[_selectedTool] = _selectedItem.Thickness; // 선택된 도구에 대한 프리셋 저장
                    _selectedTool = value;
                    OnPropertyChanged(nameof(SelectedTool));
                    // 선택된 도구에 따라 두께 프리셋을 업데이트
                    UpdateThicknessPresets();
                    // 도구가 변경되면 기본 두께 또는 이전 두께와 가장 유사한 두께를 선택할 수 있도록 처리
                    // 예를 들어, 첫 번째 프리셋을 기본값으로 선택하거나, 현재 SelectedThickness와 가장 가까운 값을 선택

                    if (_toolThickness.TryGetValue(_selectedTool, out var thickness))
                    {
                        // 선택된 도구에 대한 프리셋이 있다면 해당 프리셋을 선택
                        SelectedItem = ThicknessPreset.FirstOrDefault(x => x.Thickness == thickness);
                    }
                    else if (ThicknessPreset.Count > 0)
                    {
                        // 프리셋이 비어있지 않다면 첫 번째 아이템을 선택
                        SelectedItem = ThicknessPreset.First();
                    }
                    else
                    {
                        SelectedItem = null; // 프리셋이 없으면 선택 해제
                    }
                }
            }
        }

        public double SelectedThickness
        {
            get => SelectedItem?.Thickness ?? (ThicknessPreset.FirstOrDefault()?.Thickness ?? 1); 
        }

        private ThicknessPresetItem? _selectedItem = null;
        public ThicknessPresetItem? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged(nameof(SelectedItem));
                    SelectedItemChanged?.Invoke(this, new RoutedEventArgs()); // SelectedItem 변경 이벤트 발생
                    OnPropertyChanged(nameof(SelectedThickness)); // 두께가 변경되면 알림
                    ThicknessChanged?.Invoke(this, new RoutedEventArgs()); // 두께 변경 이벤트 발생
                }
            }
        }

        public event RoutedEventHandler? ThicknessChanged;
        public event RoutedEventHandler? SelectedItemChanged;


        public ObservableCollection<ThicknessPresetItem> ThicknessPreset { get; set; } = new ObservableCollection<ThicknessPresetItem>();

        // 선택된 도구에 따라 두께 프리셋을 반환합니다.
        private List<ThicknessPresetItem> GetThicknessPresets(QIDrawingToolType toolType)
        {
            return toolType switch
            {
                QIDrawingToolType.Pen => PenThicknessPresets.Select(thickness => new ThicknessPresetItem { Thickness = thickness }).ToList(),
                QIDrawingToolType.Highlighter => HighlighterThicknessPresets.Select(thickness => new ThicknessPresetItem { Thickness = thickness }).ToList(),
                QIDrawingToolType.Eraser => EraserThicknessPresets.Select(thickness => new ThicknessPresetItem { Thickness = thickness }).ToList(),
                QIDrawingToolType.Shape => ShapeThicknessPresets.Select(thickness => new ThicknessPresetItem { Thickness = thickness }).ToList(),
                _ => new List<ThicknessPresetItem>() // 기본적으로 빈 리스트 반환
            };
        }

        private void UpdateThicknessPresets()
        {
            var currentSelectedThickness = SelectedThickness; // 현재 두께 저장
            ThicknessPreset.Clear();
            var presets = GetThicknessPresets(SelectedTool);
            foreach (var preset in presets)
            {
                ThicknessPreset.Add(preset);
            }
            OnPropertyChanged(nameof(ThicknessPreset)); // 컬렉션 자체가 변경되었음을 알림 (Clear 후 Add 했으므로)
        }


        private void ThicknessButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.DataContext is ThicknessPresetItem item)
                {
                    SelectedItem = item; // 선택된 아이템을 설정
                }
            }
        }
    }
}
