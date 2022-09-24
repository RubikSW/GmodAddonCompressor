﻿using GmodAddonCompressor.Properties;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmodAddonCompressor.Systems
{
    internal class FFMpegSystem
    {
        private const string _mainDirectoryFFMpeg = "ffmpeg";
        private readonly string _ffmpegFilePath;
        private readonly ILogger _logger = LogSystem.CreateLogger<FFMpegSystem>();

        public FFMpegSystem()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string appDirectory = Path.Combine(baseDirectory, _mainDirectoryFFMpeg);

            if (!Directory.Exists(appDirectory))
            {
                string zipResourcePath = Path.Combine(baseDirectory, _mainDirectoryFFMpeg + ".zip");

                if (!File.Exists(zipResourcePath))
                    File.WriteAllBytes(zipResourcePath, Resources.ffmpeg);

                ZipFile.ExtractToDirectory(zipResourcePath, appDirectory);
                File.Delete(zipResourcePath);
            }

            _ffmpegFilePath = Path.Combine(appDirectory, "ffmpeg.exe");
        }

        private async Task StartFFMpegProcess(string arguments)
        {
            Process? ffMpegProcess = null;

            try
            {
                ffMpegProcess = new Process();
                ffMpegProcess.StartInfo.FileName = _ffmpegFilePath;
                ffMpegProcess.StartInfo.Arguments = arguments;
                ffMpegProcess.StartInfo.UseShellExecute = false;
                ffMpegProcess.StartInfo.CreateNoWindow = true;
                ffMpegProcess.StartInfo.RedirectStandardOutput = true;
                ffMpegProcess.StartInfo.RedirectStandardError = true;
                ffMpegProcess.OutputDataReceived += (sender, args) => _logger.LogDebug(args.Data);
                ffMpegProcess.ErrorDataReceived += (sender, args) => _logger.LogDebug(args.Data);
                ffMpegProcess.Start();
                ffMpegProcess.BeginOutputReadLine();
                ffMpegProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }

            if (ffMpegProcess != null)
                await Task.WhenAny(ffMpegProcess.WaitForExitAsync(), Task.Delay(TimeSpan.FromMinutes(2)));
        }

        internal async Task<bool> CompressAudioAsync(string filePath, string outputFilePath, int bitrate)
        {
            long oldFileSize = new FileInfo(filePath).Length;

            await StartFFMpegProcess($"-i \"{filePath}\" -ar {bitrate} {outputFilePath}");

            if (File.Exists(outputFilePath))
            {
                long newFileSize = new FileInfo(outputFilePath).Length;
                if (newFileSize < oldFileSize)
                {
                    File.Delete(filePath);
                    File.Copy(outputFilePath, filePath);
                    File.Delete(outputFilePath);

                    return true;
                }
                else
                    File.Delete(outputFilePath);
            }

            return false;
        }
    }
}