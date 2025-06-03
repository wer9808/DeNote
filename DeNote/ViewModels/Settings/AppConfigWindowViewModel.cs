using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DeNote.ViewModels.Settings
{
    public partial class AppConfigWindowViewModel : ObservableObject, IDisposable
    {
        public class AppConfigTabItem
        {
            public string Header { get; set; }
            public object Content { get; set; }
            public AppConfigTabItem(string header, object content)
            {
                Header = header;
                Content = content;
            }
        }

        private List<AppConfigTabItem> _tabItems = new List<AppConfigTabItem>();
        public List<AppConfigTabItem> TabItems
        {
            get => _tabItems;
            set => SetProperty(ref _tabItems, value);
        }

        private AppConfigTabItem? _selectedTabItem;
        public AppConfigTabItem? SelectedTabItem
        {
            get => _selectedTabItem;
            set => SetProperty(ref _selectedTabItem, value);
        }

        private GeneralSettingsPageViewModel _generalSettings;
        private FilePathSettingsPageViewModel _filePathSettings;
        private AppUsageHelpViewModel _appUsageHelp;


        public AppConfigWindowViewModel()
        {
            _generalSettings = new GeneralSettingsPageViewModel();
            _filePathSettings = new FilePathSettingsPageViewModel();
            _appUsageHelp = new AppUsageHelpViewModel();

            var tabItems = new List<AppConfigTabItem>
            {
                new AppConfigTabItem("일반", _generalSettings),
                new AppConfigTabItem("파일 경로", _filePathSettings),
                new AppConfigTabItem("도움말", _appUsageHelp)
            };

            TabItems = tabItems;
            SelectedTabItem = TabItems.FirstOrDefault(); // 기본 선택 탭 설정
        }

        [RelayCommand]
        private void SaveSettings()
        {
            try
            {
                // 각 탭의 ViewModel에서 설정을 저장합니다.
                _generalSettings.SaveSettings();
                _filePathSettings.SaveSettings();
                AppConfig.Save(); // AppConfig의 모든 변경 사항을 INI 파일에 저장
            }
            catch (Exception ex)
            {
                // 예외 처리 로직을 여기에 추가
                Debug.WriteLine($"설정 저장 중 오류: {ex.Message}");
                throw;
            }
        }

        [RelayCommand]
        private void CancelSettings()
        {
            // 설정 취소 로직이 필요하다면 여기에 추가
            AppConfig.Load(); // AppConfig에서 설정을 다시 불러옵니다.
        }

        public void Dispose()
        {
            
        }
    }
}
