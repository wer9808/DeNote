using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace DeNote
{
    // 앱 설정 관리 Static 클래스
    public static class AppConfig
    {
        private const string _iniFileName = "DeNote.ini";
        private static readonly string _iniFilePath;
        private static Dictionary<string, Dictionary<string, string>> _data;
        private static bool _isLoaded = false; // 설정이 로드되었는지 확인하는 플래그

        static AppConfig()
        {
            // 정적 생성자에서 INI 파일 경로 설정
            _iniFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _iniFileName);
            _data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Load(); // 앱 시작 시 자동으로 설정 로드
        }

        // INI 파일 불러오기 및 없을 경우 기본값 설정
        public static void Load()
        {
            _data.Clear(); // 기존 데이터 초기화

            if (!File.Exists(_iniFilePath))
            {
                Console.WriteLine("INI 파일이 존재하지 않습니다. 기본 설정으로 생성합니다.");
                // 파일이 없으므로, 기본 설정을 _data에 채웁니다.
                SetDefaultSettings();
                Save(); // 기본 설정으로 파일을 생성하고 저장합니다.
                _isLoaded = true;
                return;
            }

            string currentSection = string.Empty;
            try
            {
                foreach (string line in File.ReadAllLines(_iniFilePath))
                {
                    string trimmedLine = line.Trim();

                    if (trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#") || string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        continue;
                    }

                    Match sectionMatch = Regex.Match(trimmedLine, @"^\[(.*?)\]$");
                    if (sectionMatch.Success)
                    {
                        currentSection = sectionMatch.Groups[1].Value;
                        if (!_data.ContainsKey(currentSection))
                        {
                            _data.Add(currentSection, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                        }
                        continue;
                    }

                    Match keyValueMatch = Regex.Match(trimmedLine, @"^([^=]+?)\s*=\s*(.*)$");
                    if (keyValueMatch.Success)
                    {
                        if (!string.IsNullOrEmpty(currentSection))
                        {
                            string key = keyValueMatch.Groups[1].Value.Trim();
                            string value = keyValueMatch.Groups[2].Value.Trim();
                            _data[currentSection][key] = value;
                        }
                    }
                }
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"INI 파일 로드 중 오류 발생: {ex.Message}");
                // 로드 실패 시, 기본 설정을 다시 채우고 저장하는 것을 고려할 수 있습니다.
                // SetDefaultSettings();
                // Save();
            }
        }

        // 기본 설정 값을 _data에 채워넣는 메서드
        private static void SetDefaultSettings()
        {
            // General 섹션
            SetString("General", "AppName", "DeNote");
            SetInt("General", "Version", 1);

            // Network 섹션
            SetString("Network", "ServerIP", "127.0.0.1");
            SetInt("Network", "Port", 8080);
            SetInt("Network", "TimeoutMs", 5000);

            // FilePath 섹션
            string videoSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeNote", "Records");
            string imageSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeNote", "Screenshots");
            SetString("FilePath", "VideoSavePath", videoSavePath);
            SetString("FilePath", "ImageSavePath", imageSavePath);

            // 필요에 따라 더 많은 기본 설정 추가
        }


        // INI 파일 저장하기
        public static void Save()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(_iniFilePath))
                {
                    foreach (var sectionEntry in _data)
                    {
                        sw.WriteLine($"[{sectionEntry.Key}]");
                        foreach (var keyValuePair in sectionEntry.Value)
                        {
                            sw.WriteLine($"{keyValuePair.Key}={keyValuePair.Value}");
                        }
                        sw.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"INI 파일 저장 중 오류 발생: {ex.Message}");
            }
        }

        // 값 가져오기 (string)
        public static string GetString(string section, string key, string defaultValue = "")
        {
            if (!_isLoaded) Load();
            if (_data.ContainsKey(section) && _data[section].ContainsKey(key))
            {
                return _data[section][key];
            }
            // 기본값을 설정했는데, _data에 없는 키라면 SetString을 호출하여 추가하지 않습니다.
            // 이는 GetString 호출 시 파일에 바로 쓰이는 것을 방지합니다.
            return defaultValue;
        }

        // 값 설정하기 (string)
        public static void SetString(string section, string key, string value)
        {
            if (!_data.ContainsKey(section))
            {
                _data[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            _data[section][key] = value;
        }

        // 특정 타입의 값 가져오기 (예: int)
        public static int GetInt(string section, string key, int defaultValue = 0)
        {
            string value = GetString(section, key, defaultValue.ToString()); // 기본값을 문자열로 넘겨 TryParse에서 사용
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        // 특정 타입의 값 설정하기 (예: int)
        public static void SetInt(string section, string key, int value)
        {
            SetString(section, key, value.ToString());
        }

        // 값 가져오기 (bool)
        public static bool GetBool(string section, string key, bool defaultValue = false)
        {
            string value = GetString(section, key, defaultValue.ToString()).ToLower(); // 소문자로 변환하여 비교
            return value == "true";
        }

        // 값 설정하기 (bool)
        public static void SetBool(string section, string key, bool value)
        {
            SetString(section, key, value.ToString());
        }

        // 키 삭제하기
        public static void RemoveKey(string section, string key)
        {
            if (!_isLoaded) Load();
            if (_data.ContainsKey(section) && _data[section].ContainsKey(key))
            {
                _data[section].Remove(key);
            }
        }

        // 섹션 삭제하기
        public static void RemoveSection(string section)
        {
            if (!_isLoaded) Load();
            if (_data.ContainsKey(section))
            {
                _data.Remove(section);
            }
        }

        // 예시: 특정 설정 프로퍼티 (편의성을 위해 추가)
        public static string AppName
        {
            get => GetString("General", "AppName", "DeNote");
            set => SetString("General", "AppName", value);
        }

        public static string VideoSavePath
        {
            get => GetString("FilePath", "VideoSavePath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeNote", "Records"));
            set => SetString("FilePath", "VideoSavePath", value);
        }

        public static string ImageSavePath
        {
            get => GetString("FilePath", "ImageSavePath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeNote", "Screenshots"));
            set => SetString("FilePath", "ImageSavePath", value);
        }
    }


}
