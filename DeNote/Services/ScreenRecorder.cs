using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using ScreenRecorderLib;
using System.Diagnostics;

namespace DeNote.Services
{
    internal class ScreenRecorder : IDisposable
    {

        private Recorder? _recorder;
        private bool _isRecording = false;
        public bool IsRecording => _isRecording;

        public void StartRecording()
        {
            if (_isRecording)
                return; 
            try 
            {
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
                        Quality = 70, // 비디오 품질 (0-100)
                        Bitrate = 5000000, // 비트레이트 (5Mbps)
                        Encoder = new H264VideoEncoder()
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

                string fileDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                    $"DeNote");

                if (!System.IO.Directory.Exists(fileDir))
                {
                    System.IO.Directory.CreateDirectory(fileDir);
                }

                // 저장할 파일 경로 지정
                string filePath = System.IO.Path.Combine(
                    fileDir,
                    $"ScreenRecording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");

                // 녹화 시작
                _recorder.Record(filePath);

                _isRecording = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"녹화 시작 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PauseRecording()
        {
            try
            {
                if (_isRecording)
                {
                    // 녹화 일시 정지
                    _recorder?.Pause();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"녹화 일시 정지 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void StopRecording()
        {
            try
            {
                // 녹화 중지
                _recorder?.Stop();

                // 레코더 객체 해제는 OnRecordingComplete 이벤트에서 처리
                _isRecording = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"녹화 중지 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnRecordingComplete(object sender, RecordingCompleteEventArgs e)
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

        private void OnRecordingFailed(object sender, RecordingFailedEventArgs e)
        {
            try
            {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    MessageBox.Show($"녹화 실패: {e.Error}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                });

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

        private void OnRecordingStatusChanged(object sender, RecordingStatusEventArgs e)
        {
            // 상태 변경 처리 (필요한 경우)
            Debug.WriteLine($"녹화 상태 변경: {e.Status}");
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
                Console.WriteLine($"레코더 정리 중 오류: {ex.Message}");
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
