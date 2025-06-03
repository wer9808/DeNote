using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeNote.ViewModels.Settings
{
    public partial class FilePathSettingsPageViewModel : ObservableObject
    {

        
        private string _videoSavePath = AppConfig.VideoSavePath;
        public string VideoSavePath
        {
            get => _videoSavePath;
            set
            {
                _videoSavePath = value;
                OnPropertyChanged(nameof(VideoSavePath));
            }
        }

        private string _imageSavePath = AppConfig.ImageSavePath;
        public string ImageSavePath
        {
            get => _imageSavePath;
            set
            {
                _imageSavePath = value;
                OnPropertyChanged(nameof(ImageSavePath));
            }
        }

        public FilePathSettingsPageViewModel() {
            Debug.WriteLine("FilePathSettingsPageViewModel 생성됨");
        }


        [RelayCommand]
        private void ChangeVideoSavePath()
        {
            var videoPathDialog = new OpenFolderDialog
            {
                Title = "비디오 저장 경로를 선택하세요.",
                ValidateNames = true,
            };

            if (videoPathDialog.ShowDialog() != true)
                return;

            string newPath = videoPathDialog.FolderName;

            if (!string.IsNullOrWhiteSpace(newPath))
            {
                VideoSavePath = newPath;
            }
            Debug.WriteLine($"비디오 경로 설정 완료 : {VideoSavePath}");
        }

        [RelayCommand]
        private void ChangeImageSavePath()
        {
            var imagePathDialog = new OpenFolderDialog
            {
                Title = "이미지 저장 경로를 선택하세요.",
                ValidateNames = true
            };
            if (imagePathDialog.ShowDialog() != true)
                return;

            string? newPath = imagePathDialog.FolderName;
            if (!string.IsNullOrWhiteSpace(newPath))
            {
                ImageSavePath = newPath;
            }
        }

        internal void SaveSettings()
        {
            // 설정 저장 로직
            Debug.WriteLine($"파일 경로 설정 : 비디오 {VideoSavePath}, 이미지 {ImageSavePath}");

            AppConfig.VideoSavePath = VideoSavePath;
            AppConfig.ImageSavePath = ImageSavePath;
            Debug.WriteLine("파일 경로 설정이 저장되었습니다.");
            Debug.WriteLine($"저장된 경로 : 비디오 {AppConfig.VideoSavePath}, 이미지 {AppConfig.ImageSavePath}");
        }
    }
}
