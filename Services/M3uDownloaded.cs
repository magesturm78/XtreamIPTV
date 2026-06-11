using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using XtreamIPTV.Models;
using XtreamIPTV.ViewModels;

namespace XtreamIPTV.Services
{
    class M3U8Downloader
    {
        private const string basePath = "E:\\movies";
        public static async Task DownloadAndCombineAsync(
            string m3u8Url,
            string outputFile,
            Dictionary<string, string> headers)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var http = new HttpClient(handler);

            // Apply headers
            if (headers != null)
            {
                foreach (var kv in headers)
                    http.DefaultRequestHeaders.TryAddWithoutValidation(kv.Key, kv.Value);
            }

            // 1. Download playlist
            string playlistContent = await http.GetStringAsync(m3u8Url);

            // 2. Base URL for resolving relative paths
            var baseUri = new Uri(m3u8Url);

            // 3. Extract segment URLs
            var segments = playlistContent
                .Split('\n')
                .Where(line => !line.StartsWith("#") && line.Trim().Length > 0)
                .Select(line => new Uri(baseUri, line.Trim()).ToString())
                .ToList();

            if (segments.Count == 0)
            {
                Debug.Print("No segments found.");
                return;
            }

            Debug.Print($"Found {segments.Count} segments.");

            // 4. Download segments
            var tempFiles = new List<string>();

            for (int i = 0; i < segments.Count; i++)
            {
                string segmentUrl = segments[i];
                string tempFile = Path.GetTempFileName();

                Debug.Print($"Downloading segment {i + 1}/{segments.Count}");

                byte[] data = await http.GetByteArrayAsync(segmentUrl);
                await File.WriteAllBytesAsync(tempFile, data);

                tempFiles.Add(tempFile);
            }

            // 5. Combine segments
            Debug.Print("Combining segments...");

            using (var output = File.Create(outputFile))
            {
                foreach (var temp in tempFiles)
                {
                    byte[] bytes = await File.ReadAllBytesAsync(temp);
                    await output.WriteAsync(bytes);
                }
                output.Flush();
            }
            // 6. Cleanup
            foreach (var temp in tempFiles)
                File.Delete(temp);

            Debug.Print($"Done. Output saved to: {outputFile}");
        }

        public static async Task Download(
            string m3u8Url,
            string location,
            Dictionary<string, string> headers)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var http = new HttpClient(handler);

            // Apply headers
            if (headers != null)
            {
                foreach (var kv in headers)
                    http.DefaultRequestHeaders.TryAddWithoutValidation(kv.Key, kv.Value);
            }

            // 1. Download playlist
            string playlistContent = await http.GetStringAsync(m3u8Url);

            string movieDir = Path.Combine(basePath, location);
            if (!Directory.Exists(movieDir))
                Directory.CreateDirectory(movieDir);

            File.WriteAllBytes(Path.Combine(movieDir, "orig.m3u8"), Encoding.UTF8.GetBytes(playlistContent));
            // 2. Base URL for resolving relative paths
            var baseUri = new Uri(m3u8Url);

            // 3. change names of segments to index0.ts, index1.ts, etc. and save them in the same directory as the playlist
            int index = 0;
            string playlistPath = Path.Combine(movieDir, "index.m3u8");
            StringBuilder playlist = new();
            foreach (var line in playlistContent.Split('\n'))
            {
                if (!line.StartsWith("#") && line.Trim().Length > 0)
                {
                    var segmentUrl = new Uri(baseUri, line.Trim()).ToString();
                    string tempFile = $"index{index}.ts";
                    playlist.AppendLine(tempFile);
                    index++;
                }
                else
                {
                    playlist.AppendLine(line);
                }
            }
            int totalSegments = index;
            if (totalSegments < 20)
            {
                Debug.Print($"Only {totalSegments} segments found. Aborting download.");
                return;
            }
            File.WriteAllBytes(playlistPath, Encoding.UTF8.GetBytes(playlist.ToString()));
            index = 0;
            var movie = MainViewModel.Instance.MoviesVM.SelectedMovie;
            foreach (var line in playlistContent.Split('\n'))
            {
                if (!line.StartsWith("#") && line.Trim().Length > 0)
                {
                    var segmentUrl = new Uri(baseUri, line.Trim()).ToString();
                    string newFile = Path.Combine(movieDir, $"index{index}.ts");
                    index++;
                    Debug.Print($"Downloading {index}/{totalSegments} {((double)index / (double)totalSegments):P2}");
                    MainViewModel.Instance.Title = $"XtreamIPTV Playing {movie?.Title}  --  Downloading {index}/{totalSegments} {((double)index / (double)totalSegments):P2}";
                    bool downloaded = false;
                    int tries = 0;
                    if (File.Exists(newFile))
                    {
                        Debug.Print($"Segment {index} already exists, skipping download.");
                        downloaded = true;
                    };
                    while (!downloaded) 
                        try
                        {
                            byte[] data = await http.GetByteArrayAsync(segmentUrl);
                            await File.WriteAllBytesAsync(newFile, data);
                            downloaded = true;
                        }
                        catch (Exception ex)
                        {
                            Debug.Print($"Error downloading segment {index}: {ex.Message}");
                            Task.Delay(1000).Wait();
                            tries++;
                            if (tries == 5) {
                                Debug.Print($"Failed to download segment {index}");
                                break;
                            }
                        }
                    if (!downloaded)
                        break;
                }
            }
            if (index < totalSegments) {
                Debug.Print($"Failed to download all segments. Only {index}/{totalSegments} were downloaded.");
                MessageBox.Show($"Failed to download all segments. Only {index}/{totalSegments} were downloaded.");
            } 
            else
            {
                MainViewModel.Instance.Title = $"XtreamIPTV Playing {movie?.Title}  --  Downloaded";
                //MessageBox.Show($"{location} downloaded successfully.");
            }
        }

        public static bool ValidM3U8(string movieDir)
        {
            string playlistPath = Path.Combine(movieDir, "index.m3u8");
            string playlistContent = File.ReadAllText(playlistPath);
            StringBuilder playlist = new();
            int index = 0;
            bool containsSegments = false;
            foreach (var line in playlistContent.Split('\n'))
            {
                if (!line.StartsWith("#") && line.Trim().Length > 0)
                {
                    containsSegments = true;
                    string newFile = Path.Combine(movieDir, $"index{index}.ts");
                    index++;

                    if (!File.Exists(newFile))
                    {
                        return false;
                    }
                }
            }
            return containsSegments;
        }
    }
}