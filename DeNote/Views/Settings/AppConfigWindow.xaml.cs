using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using DeNote.ViewModels.Settings;

namespace DeNote.Views.Settings
{
    /// <summary>
    /// AppConfigWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AppConfigWindow : Window
    {
        public AppConfigWindow()
        {
            InitializeComponent();
            AppConfig.Load();
            this.DataContext = new AppConfigWindowViewModel();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 창이 로드될 때 AppSettings에서 최신 설정 값을 불러옵니다.
            // AppSettings의 static 생성자에서 이미 Load()를 호출하므로,
            // 여기서 다시 호출할 필요는 없지만, 혹시 모를 경우를 대비하여 명시적으로 호출할 수도 있습니다.
            // AppSettings.Load(); // 이미 static 생성자에서 로드하므로 일반적으로 필요 없음.
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AppConfigWindowViewModel viewModel)
            {
                try
                {
                    // 설정 저장 로직을 호출합니다.
                    viewModel.SaveSettingsCommand.Execute(null);
                }
                catch (Exception ex)
                {
                    // 예외 발생 시 사용자에게 알립니다.
                    MessageBox.Show($"설정 저장 중 오류가 발생했습니다", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // 오류 발생 시 저장하지 않고 종료
                }
            }

            MessageBox.Show("설정이 저장되었습니다.", "저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close(); // 창 닫기
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // 취소 시에는 저장하지 않고 창을 닫습니다.
            // 만약 취소 시 초기 상태로 되돌리고 싶다면, Load()를 다시 호출하거나
            // DataContext를 초기화하는 로직이 필요할 수 있습니다.
            if (DataContext is AppConfigWindowViewModel viewModel)
            {
                viewModel.CancelSettingsCommand.Execute(null);
            }
            this.Close();
        }
    }
}
