using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeNote.ViewModels.Settings
{
    public class FilePathSettingsPageViewModel : ObservableObject
    {

        public string VideoSavePath { get => AppConfig.VideoSavePath; }
        public string ImageSavePath { get => AppConfig.ImageSavePath; }

    }
}
