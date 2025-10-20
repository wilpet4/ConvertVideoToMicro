using System.Globalization;
using System.Text.Json;
using FFmpegArgs.Executes;
using FFMpegCore;

namespace ConvertVideoToMicro
{
    internal class Program
    {
        public static string ffmpegPath = "C:\\Users\\William\\AppData\\Local\\Playnite\\ffmpeg-7.1.1-essentials_build\\ffmpeg-7.1.1-essentials_build\\bin\\ffmpeg.exe";
        public static string ffmpegBinPath = "C:\\Users\\William\\AppData\\Local\\Playnite\\ffmpeg-7.1.1-essentials_build\\ffmpeg-7.1.1-essentials_build\\bin";

        public static JsonDocument configuration = JsonDocument.Parse(File.ReadAllText("configuration.json"));

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    var videoPath = args[i];
                    var file = new FileInfo(videoPath);
                    var microName = file.Name.Insert(0, "micro.");
                    var microPath = Path.Combine("C:\\Users\\William\\Documents\\ConvertVideoToMicro\\output", microName);

                    Console.WriteLine($"Converting Video: {file.Name}");
                    var result = ConvertVideoToMicro(videoPath, microPath, true);

                    if (result == true)
                    {
                        Console.WriteLine($"Conversion Finished: {file.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"Conversion Failed: {file.Name}");
                    }
                }

                    Console.ReadLine();
            }
            else
            {
                Console.WriteLine("Need input file!");
                Console.ReadLine();
            }
        }

        public static bool ConvertVideoToMicro(string videoPath, string microPath, bool overwrite)
        {
            var noSound = true;
            if (!File.Exists(videoPath) || (File.Exists(microPath) && !overwrite))
            {
                return false;
            }

            GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegBinPath);

            var probe = FFProbe.Analyse(videoPath);
            var videoDuration = probe.Duration.TotalSeconds;

            var rangeStringList = new List<string>();
            var clipDuration = configuration.RootElement.GetProperty("ClipDuration").GetInt32();

            int[] startPercentageVideo = JsonSerializer.Deserialize<int[]>(configuration.RootElement.GetProperty("ClipTimings"));

            if (startPercentageVideo == null || clipDuration == null)
            {
                return false;   
            }

            foreach (var percentage in startPercentageVideo)
            {
                double clipStart = (percentage * videoDuration) / 100;
                double clipEnd = clipStart + clipDuration;
                rangeStringList.Add(string.Format("between(t,{0:N2},{1:N2})", clipStart.ToString(CultureInfo.InvariantCulture), clipEnd.ToString(CultureInfo.InvariantCulture)));
            }
            var selectString = $"\"select = '{string.Join("+", rangeStringList)}', setpts = N / FRAME_RATE / TB, scale = trunc(iw / 2) * 2:trunc(ih / 2) * 2\"";
            string args = string.Empty;

            var audio = configuration.RootElement.GetProperty("Audio").GetBoolean();

            if (audio == false)
            {
                args = $"-y -i \"{videoPath}\" -map 0:0 -vf {selectString} -c:v libx264 \"{microPath}\"";
            }
            else
            {
                var audioSelectString = $"\"aselect = '{string.Join("+", rangeStringList)}', asetpts = N / FRAME_RATE / TB\"";
                args = $"-y -i \"{videoPath}\" -vf {selectString} -af {audioSelectString}  -c:v libx264 -c:a aac \"{microPath}\"";
            }
            var render = FFmpegRender.FromArguments(args, x =>
            {
                x.FFmpegBinaryPath = ffmpegPath;
                x.WorkingDirectory = ffmpegBinPath;
            });
            render.Execute();

            return true;
        }
    }
}
