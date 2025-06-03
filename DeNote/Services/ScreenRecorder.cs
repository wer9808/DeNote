using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ScreenRecorderLib;

namespace DeNote.Services
{
    internal class ScreenRecorder : IDisposable
    {

        private Recorder? _recorder;
        private bool _isRecording = false;
        public bool IsRecording => _isRecording;

        public event EventHandler<RecordingFailedEventArgs>? RecordingFailed;

        // Helper method to check write permissions
        private bool HasWritePermissionToDirectory(string directoryPath)
        {
            try
            {
                // 1. Attempt to create the directory if it doesn't exist.
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath); // This will create all directories in the path.
                }

                // 2. Attempt to create and delete a temporary file in the directory.
                // Using Guid to ensure a unique temporary file name.
                // FileOptions.DeleteOnClose ensures the file is cleaned up automatically.
                string tempFilePath = Path.Combine(directoryPath, Guid.NewGuid().ToString() + ".tmp");
                using (FileStream fs = File.Create(tempFilePath, 1, FileOptions.DeleteOnClose))
                {
                    // If we reach here, we were able to create the file.
                    // The file will be deleted when fs is disposed (at the end of the using block).
                }
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // Specific catch for permission issues.
                Debug.WriteLine($"Permission denied for directory: {directoryPath}");
                return false;
            }
            catch (Exception ex)
            {
                // Other exceptions (e.g., path too long, invalid characters, disk full, etc.)
                // For simplicity in this check, also treat these as "cannot write here".
                Debug.WriteLine($"Error checking write permission for '{directoryPath}': {ex.Message}");
                return false;
            }
        }

        public void StartRecording()
        {
            if (_isRecording)
                return;

            string? saveDir = AppConfig.VideoSavePath; // Make sure AppConfig.VideoSavePath provides a valid path string
            if (saveDir == null)
            {
                _isRecording = false; // Ensure state is correct
                throw new Exception("녹화 저장 경로가 설정되지 않았습니다. 저장 경로를 확인해주세요.");
            }

                // --- Permission Check Start ---
            if (!HasWritePermissionToDirectory(saveDir))
            {
                Debug.WriteLine($"녹화 권한 오류: '{saveDir}' 경로에 쓸 수 없습니다.");
                _isRecording = false; // Ensure state is correct
                throw new UnauthorizedAccessException($"녹화 권한 오류: '{saveDir}' 경로에 쓸 수 없습니다.");
            }

            try 
            {

                // 저장할 파일 경로 지정
                string savePath = System.IO.Path.Combine(
                    saveDir,
                    $"ScreenRecording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");

                // Primary Display만 녹화하는 설정
                var options = new RecorderOptions
                {
                    SourceOptions = SourceOptions.MainMonitor,

                    // 음성 설정
                    AudioOptions = new AudioOptions
                    {
                        IsAudioEnabled = true,
                        IsOutputDeviceEnabled = true,  // 시스템 소리 녹음
                        IsInputDeviceEnabled = true    // 마이크 녹음 (필요 없으면 false로 설정)
                    },

                    // 비디오 설정
                    VideoEncoderOptions = new VideoEncoderOptions
                    {
                        Framerate = 30, // 초당 프레임 수
                        Quality = 100, // 비디오 품질 (0-100)
                        Encoder = new H264VideoEncoder() // H.264 인코더 사용
                    },

                    // 출력 설정
                    OutputOptions = new OutputOptions
                    {
                        RecorderMode = RecorderMode.Video,
                        
                        // null로 설정하면 원본 해상도 유지
                        OutputFrameSize = null
                    }

                    
                };

                // 레코더 생성
                _recorder = Recorder.CreateRecorder(options);

                // 이벤트 핸들러 연결
                _recorder.OnRecordingFailed += OnRecordingFailed;
                _recorder.OnStatusChanged += OnRecordingStatusChanged;

                // 녹화 시작
                _recorder.Record(savePath);

                _isRecording = true;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"녹화 권한 오류: {ex.Message}");
                _isRecording = false;
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"녹화 시작 중 오류: {ex.Message}");
                _isRecording = false;
                throw;
            }
        }

        public void StopRecording()
        {
            // 녹화 중지
            _recorder?.Stop();
            _isRecording = false;
        }

        private void OnRecordingComplete(object? sender, RecordingCompleteEventArgs e)
        {
            try
            {
                // 레코더 객체 정리
                DisposeRecorder();
            }
            catch (Exception ex)
            {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    MessageBox.Show($"녹화 완료 후 처리 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void OnRecordingFailed(object? sender, RecordingFailedEventArgs e)
        {
            try
            {
                // 녹화 실패 이벤트 발생 시 사용자에게 알림
                this.RecordingFailed?.Invoke(this, e);

                // 레코더 객체 정리
                DisposeRecorder();

                _isRecording = false;
            }
            catch (Exception ex)
            {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    MessageBox.Show($"오류 처리 중 추가 예외 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void OnRecordingStatusChanged(object? sender, RecordingStatusEventArgs e)
        {
            // 상태 변경 처리 (필요한 경우)

        }

        private void DisposeRecorder()
        {
            try
            {
                // 이벤트 핸들러 제거
                if (_recorder != null)
                {
                    _recorder.OnRecordingComplete -= OnRecordingComplete;
                    _recorder.OnRecordingFailed -= OnRecordingFailed;
                    _recorder.OnStatusChanged -= OnRecordingStatusChanged;

                    // 레코더 객체 해제
                    _recorder.Dispose();
                    _recorder = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"레코더 정리 중 오류: {ex.Message}");
            }
        }

        public void Dispose()
        {
            // Dispose 메서드에서 레코더 객체 정리
            if (_isRecording)
            {
                StopRecording();
            }

            DisposeRecorder();
        }
    }
}
