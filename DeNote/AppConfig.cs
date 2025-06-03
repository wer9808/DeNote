using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
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
            _data.Clear();

            if (!File.Exists(_iniFilePath))
            {
                Debug.WriteLine("INI 파일이 존재하지 않습니다. 기본 설정으로 생성합니다.");
                SetDefaultSettings();
                Save();
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
                Debug.WriteLine($"INI 파일 로드 중 오류 발생: {ex.Message}");
            }
        }

        // 기본 설정 값을 _data에 채워넣는 메서드
        private static void SetDefaultSettings()
        {
            // General 섹션
            SetString("General", "AppName", "DeNote");
            SetInt("General", "Version", 1);

            // FilePath 섹션
            string videoSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DeNote", "Records");
            string imageSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DeNote", "Screenshots");

            SetString("FilePath", "VideoSavePath", videoSavePath);
            SetString("FilePath", "ImageSavePath", imageSavePath);

            // 필요에 따라 더 많은 기본 설정 추가
        }


        // INI 파일 저장하기

        public static void Save()
        {
            try
            {
                // 저장 전 폴더 존재 확인 및 생성 (선택적이지만 권장)
                string? directory = Path.GetDirectoryName(_iniFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

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
                Debug.WriteLine($"INI 파일 저장 완료: {_iniFilePath}");
            }
            catch (Exception ex)
            {
                // 사용자에게 오류를 알리는 것이 좋습니다.
                Debug.WriteLine($"INI 파일 저장 중 오류 발생: {ex.Message}");
                MessageBox.Show($"설정 파일 저장에 실패했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 값 가져오기 (string)
        public static string? GetString(string section, string key)
        {
            if (_data.ContainsKey(section) && _data[section].ContainsKey(key))
            {
                return _data[section][key];
            }
            // 기본값을 설정했는데, _data에 없는 키라면 SetString을 호출하여 추가하지 않습니다.
            // 이는 GetString 호출 시 파일에 바로 쓰이는 것을 방지합니다.
            return null;
        }

        // 값 설정하기 (string)
        public static void SetString(string section, string key, string value)
        {
            if (!_data.ContainsKey(section))
            {
                _data[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            _data[section][key] = value; // 이미 실행했다고 가정
        }

        // 특정 타입의 값 가져오기 (예: int)
        public static int GetInt(string section, string key, int defaultValue = 0)
        {
            string? value = GetString(section, key); // 기본값을 문자열로 넘겨 TryParse에서 사용
            if (value == null) return defaultValue; // 값이 없으면 기본값 반환
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
            string? value = GetString(section, key); // 기본값을 문자열로 넘겨 TryParse에서 사용
            if (value == null) return defaultValue; // 값이 없으면 기본값 반환
            return value.ToLower() == "true"; // 소문자로 변환하여 비교
        }

        // 값 설정하기 (bool)
        public static void SetBool(string section, string key, bool value)
        {
            SetString(section, key, value.ToString());
        }

        // 키 삭제하기
        public static void RemoveKey(string section, string key)
        {
            if (_data.ContainsKey(section) && _data[section].ContainsKey(key))
            {
                _data[section].Remove(key);
            }
        }

        // 섹션 삭제하기
        public static void RemoveSection(string section)
        {
            if (_data.ContainsKey(section))
            {
                _data.Remove(section);
            }
        }

        // 예시: 특정 설정 프로퍼티 (편의성을 위해 추가)
        public static string? AppName
        {
            get => GetString("General", "AppName");
            set => SetString("General", "AppName", value);
        }

        public static string? VideoSavePath
        {
            get => GetString("FilePath", "VideoSavePath");
            set => SetString("FilePath", "VideoSavePath", value);
        }

        public static string? ImageSavePath
        {
            get => GetString("FilePath", "ImageSavePath");
            set => SetString("FilePath", "ImageSavePath", value);
        }
    }
}
