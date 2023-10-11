using FFmpeg.AutoGen;
using FFMpegCore;
using System.Diagnostics;

namespace ProcesarVideosSeeltApi.Modelos
{
    public class AudioMD
    {
        //public static void CompressAudio(string inputFile, string outputFile, int targetFileSizeInBytes)
        //{
        //    bool compressionSuccessful = false;

        //    while (!compressionSuccessful)
        //    {
        //        // Ejecutar FFmpeg para comprimir el archivo de audio
        //        string ffmpegCommand = $"-i {inputFile} -sn -vn -ar 22050 {outputFile}";
        //        Process ffmpegProcess = new Process();
        //        ffmpegProcess.StartInfo.FileName = "ffmpeg"; // Debe estar en el PATH del sistema o especifica la ruta completa
        //        ffmpegProcess.StartInfo.Arguments = ffmpegCommand;
        //        ffmpegProcess.StartInfo.UseShellExecute = false;
        //        ffmpegProcess.StartInfo.RedirectStandardError = true;
        //        ffmpegProcess.StartInfo.CreateNoWindow = true;
        //        ffmpegProcess.Start();
        //        string output = ffmpegProcess.StandardError.ReadToEnd();
        //        ffmpegProcess.WaitForExit();


        //        // Verificar el tamaño del archivo resultante
        //        FileInfo outputFileInfo = new FileInfo(outputFile);
        //        long fileSizeInBytes = outputFileInfo.Length;

        //        if (fileSizeInBytes <= targetFileSizeInBytes)
        //        {
        //            compressionSuccessful = true;
        //        }
        //        else
        //        {
        //            File.Delete(outputFile);
        //            // Si el archivo es demasiado grande, resta 10 segundos y vuelve a intentar
        //            int secondsToRemove = 60;
        //            string newInputFile = "input_audio_temp.mp3";
        //            TrimAudio(inputFile, newInputFile, secondsToRemove);
        //            inputFile = newInputFile;
        //        }
        //    }
        //}

        public static void TrimAudio(string inputFile, string outputFile, int startTimeInSeconds, int durationInSeconds)
        {
            string ffmpegCommand = $"-i \"{inputFile}\" -ss {startTimeInSeconds} -t {durationInSeconds} \"{outputFile}\"";
            Process ffmpegProcess = new Process();
            ffmpegProcess.StartInfo.FileName = "ffmpeg"; // Debe estar en el PATH del sistema o especifica la ruta completa
            ffmpegProcess.StartInfo.Arguments = ffmpegCommand;
            ffmpegProcess.StartInfo.UseShellExecute = false;
            ffmpegProcess.StartInfo.RedirectStandardError = true;
            ffmpegProcess.StartInfo.CreateNoWindow = true;
            ffmpegProcess.Start();
            string output = ffmpegProcess.StandardError.ReadToEnd();
            ffmpegProcess.WaitForExit();
        }

    }
}
