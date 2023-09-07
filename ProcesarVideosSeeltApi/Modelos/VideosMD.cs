
using FFMpegCore.Enums;
using FFMpegCore;
using Google.Cloud.Storage.V1;
using System;
using System.IO;
using Google.Cloud.Speech.V1;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Text;
using System.IO.Pipes;
using System.Linq;
using Google.Api.Gax.ResourceNames;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProcesarVideosSeeltApi.Modelos
{
    public class VideosMD
    {
        //Ubicacion de la credenciales de google cloud
        public static string CloudCredenciales = @".\seelt-987cd-7dbe2c8a55dc.json";
        //Nombre del bucket de google cloud
        public static string NombreDelBucket = @"seelt-987cd.appspot.com";

        //ID unico del usuario
        public static string UID { get; set; }

        //Nombre unico del archivo
        public static string NombreUnico { get; set; }

        public class DataDelVideo
        {
            public string RutaDelVideoEnELDirectorio { get; set; }
            public string Nombre { get; set; }
            public string Formato { get; set; }
            public long Tamano { get; set; }
        }

        public class Video 
        {
            public string Resolucion { get; set; }
            public string Ubicacion { get; set;}
        }

        public class M3U8_DeVideos
        {
            public string URL { get; set; }
            public string Resolucion { get; set; }
        }

        /*Aqui se procesan los videos de manera automatica,ejemplo: llega el video y
        directamente se separa el audio y se determina el idioma, de esa manera se genera 
        la pista en ese idioma si necesida de ser especificado por el usuario, 
        igualmente mediante IA se determina la categoria del video*/

        public static async void ProcesarVideoGeneral(string UbicacionVideoEnElBucket, string uID)
        {
            try
            {
                List<M3U8_DeVideos> m3U8_DeVideos = new List<M3U8_DeVideos> ();

                //Lista de tareas para manejar varios segmentos de codigo de manera asincronica
                var tasks = new List<Task>();

                //Es la lista de los diferente videos en las diferentes resoluciones
                List<Video> ListaDeVideos = new List<Video>();

                //Se asigna la id unica de usuario
                UID = uID;

                //Nombre unico para los archivos
                NombreUnico = Guid.NewGuid().ToString().Substring(0, 5);

                //Se crea un directorio temporal donde se guardara algunos de los archivos realizados hasta que el metodo termine
                string DirectorioTemporal = GeneralesMD.CrearDirectorioTemporal().path;

                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", CloudCredenciales);

                //Descarga el video enviado por usuario para trabajar con el, luego sera eliminado
                DataDelVideo dataDelVideo = DescargarVideo(UbicacionVideoEnElBucket, DirectorioTemporal, NombreUnico);

                //Obtienes solamente el audio del video
                string UbicacionDelAuido = ObtenerAudio(dataDelVideo.RutaDelVideoEnELDirectorio, Path.Combine(DirectorioTemporal, $"{NombreUnico}.mp3")) == true ? Path.Combine(DirectorioTemporal, $"{NombreUnico}.mp3") : "";

                //Elimina el Audio del video original
                //Crea el nuevo video sin audio y remplaza el aterior
                string VideoSinAudio = Path.Combine(DirectorioTemporal, "AudioOFF_" + NombreUnico + "." + dataDelVideo.Formato);
                Mutear(dataDelVideo.RutaDelVideoEnELDirectorio, VideoSinAudio);
                RenombrarArchivo(VideoSinAudio, dataDelVideo.RutaDelVideoEnELDirectorio);

                Dictionary<string, string> resolucionesYouTube = new Dictionary<string, string>
                {
                    {"2160p", "3840x2160"},
                    {"1440p", "2560x1440"},
                    {"1080p", "1920x1080"},
                    {"720p", "1280x720"},
                    {"480p", "854x480"},
                    {"360p", "640x360"},
                    {"240p", "426x240"},
                    {"144p", "256x144"},
                };

                string Resolucion = ObtenerResolucionVideo(dataDelVideo.RutaDelVideoEnELDirectorio);
                if (resolucionesYouTube.ContainsKey(Resolucion))
                {
                    bool encontrada = false;
                    foreach (var item in resolucionesYouTube)
                    {
                        if (item.Key == Resolucion)
                        {
                            encontrada = true;
                            RenombrarArchivo(dataDelVideo.RutaDelVideoEnELDirectorio, Path.Combine(DirectorioTemporal, $"{item.Value}_{NombreUnico}.{dataDelVideo.Formato}"));
                            dataDelVideo.RutaDelVideoEnELDirectorio = Path.Combine(DirectorioTemporal, $"{item.Value}_{NombreUnico}.{dataDelVideo.Formato}");
                            Video _video = new Video()
                            {
                                Ubicacion = Path.Combine(DirectorioTemporal, $"{item.Value}_{NombreUnico}.{dataDelVideo.Formato}"),
                                Resolucion = item.Key,
                            };
                            ListaDeVideos.Add(_video);
                            continue;
                        }
                        if (encontrada)
                        {
                            var _task = Task.Run(() =>
                            {
                                Video _video = new Video()
                                {
                                    Ubicacion = ConvertirVideo(dataDelVideo.RutaDelVideoEnELDirectorio, Path.Combine(DirectorioTemporal, $"{item.Value}_{NombreUnico}.{dataDelVideo.Formato}"), item.Value, dataDelVideo.Formato),
                                    Resolucion = item.Key,
                                };
                                ListaDeVideos.Add(_video);
                            });
                            tasks.Add(_task);
                        }
                    }
                }
                await Task.WhenAll(tasks);
                tasks.Clear();

                foreach (var item in ListaDeVideos)
                {
                    var _task = Task.Run(() =>
                    {

                        M3U8_DeVideos m3U8_DeVideos1 = new M3U8_DeVideos()
                        {
                            URL = SegmentVideo(item.Ubicacion, DirectorioTemporal, NombreUnico, item.Resolucion),
                            Resolucion = item.Resolucion
                        };
                        m3U8_DeVideos.Add(m3U8_DeVideos1);
                    });
                    tasks.Add(_task);
                }
                await Task.WhenAll(tasks);


            }
            catch (Exception)
            {

                throw;
            }
        }

        public static void ProcesarVideoProfesional()
        {

        }

        /// <summary>
        /// Descarga un video desde Google Cloud Storage y guarda información sobre el video.
        /// </summary>
        /// <param name="UbicacionVideoEnElBucket">La ubicación del video en el bucket de Google Cloud Storage.</param>
        /// <param name="DirectorioTemporal">El directorio temporal donde se guardará el video.</param>
        /// <param name="NombreUnico">El Nombre unico con el que se guardará el video.</param>
        /// <returns>Un objeto DataDelVideo que contiene información sobre el video descargado.</returns>
        public static DataDelVideo DescargarVideo(string UbicacionVideoEnElBucket, string DirectorioTemporal, string NombreUnico)
        {
            try
            {

                // Crea un cliente para interactuar con Google Cloud Storage.
                var client = StorageClient.Create();

                // Crea una memoria intermedia para almacenar el contenido descargado.
                var stream = new MemoryStream();

                // Descarga el objeto desde el bucket de Google Cloud Storage y lo almacena en la memoria intermedia.
                var obj = client.DownloadObject(NombreDelBucket, UbicacionVideoEnElBucket, stream);
                stream.Position = 0;

                // Combina la ubicación del directorio temporal con el NombreUnico para obtener la ruta completa.
                string rutaCompleta = Path.Combine(DirectorioTemporal, $"{NombreUnico}{Path.GetExtension(obj.Name)}");

                // Crea el archivo en el directorio temporal y copia el contenido descargado en él.
                using (var fileStream = File.Create(rutaCompleta))
                {
                    stream.CopyTo(fileStream);
                }

                // Crea un objeto DataDelVideo para almacenar información sobre el video descargado.
                DataDelVideo dataDelVideo = new DataDelVideo()
                {
                    RutaDelVideoEnELDirectorio = rutaCompleta,
                    Nombre = Path.GetFileNameWithoutExtension(obj.Name),
                    Formato = Path.GetExtension(obj.Name).TrimStart('.'),
                    Tamano = stream.Length,
                };

                // Devuelve el objeto DataDelVideo que contiene la información del video.
                return dataDelVideo;
            }
            catch (Exception ex)
            {
                // Maneja cualquier excepción que ocurra durante la descarga y lanzala para que sea manejada en el nivel superior.
                throw;
            }
        }

        public static bool ObtenerAudio(string UbicacionDelVideoEnELDirectorio, string UbicacionDelAudio)
        {
            try
            {
                FFMpeg.ExtractAudio(UbicacionDelVideoEnELDirectorio, UbicacionDelAudio);
                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static string SegmentVideo(string UbicacionDeArchivo, string DirectorioTemporal, string NombreUnico, string resolucion, bool audio = false)
        {


            string outputM3U8Path = Path.Combine(DirectorioTemporal,  audio == true ? "Audio_" : "Video_" + resolucion +"_" + NombreUnico + ".m3u8");
            string ffmpegCmd;
            if (audio)
            {
                ffmpegCmd = $"-i \"{UbicacionDeArchivo}\" -hls_time 2 -hls_list_size 0 -hls_segment_filename \"{DirectorioTemporal}/{NombreUnico}_AudioP_segment_%03d.mp3\" \"Audio_{outputM3U8Path}\"";
            }
            else
            {
                ffmpegCmd = $"-i \"{UbicacionDeArchivo}\" -hls_time 2 -hls_list_size 0 -hls_segment_filename \"{DirectorioTemporal}/{NombreUnico}_{resolucion}P_segment_%03d.ts\" \"{outputM3U8Path}\"";
            }

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegCmd,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            process.WaitForExit();

            if (File.Exists(outputM3U8Path))
            {
                return ModifySegments(outputM3U8Path, DirectorioTemporal, audio == true ? "Or_Audio_" : "Or_Video_" + resolucion +"_"+ NombreUnico + ".m3u8");
            }
            else
            {
                return "";
            }
        }

        public static string ModifySegments(string m3u8FilePath, string DirectorioTemporal, string NombreUnico_)
        {
            try
            {
                using (FileStream fileStream = File.OpenRead(m3u8FilePath))
                {
                    string tempFilePath = Path.Combine(DirectorioTemporal, NombreUnico_);
                    using (FileStream fileStreamwriter = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        using (StreamReader reader = new StreamReader(fileStream))
                        {
                            using (StreamWriter writer = new StreamWriter(fileStreamwriter))
                            {
                                string line;
                                while ((line = reader.ReadLine()) != null)
                                {
                                    if (line.StartsWith("#EXTINF:"))
                                    {
                                        writer.WriteLine(line);
                                    }
                                    else if (line.EndsWith(".ts") || line.EndsWith(".mp3"))
                                    {
                                        string Ubicacion = Path.Combine(DirectorioTemporal, line);
                                        UploadFileToGCS(Ubicacion, line, UID, NombreUnico);
                                        writer.WriteLine($"https://storage.googleapis.com/{NombreDelBucket}/{UID}/{NombreUnico}/{line}");

                                    }
                                    else
                                    {
                                        writer.WriteLine(line);
                                    }
                                }
                            }
                        }

                    }
                    UploadFileToGCS(tempFilePath, NombreUnico_, UID, NombreUnico);
                    return $"https://storage.googleapis.com/{NombreDelBucket}/{UID}/{NombreUnico}/{NombreUnico_}";

                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static async void UploadFileToGCS(string filePath, string objectName, string UID, string folderName = "")
        {

            var storageClient = StorageClient.Create();

            try
            {
                FileStream fileStream = File.OpenRead(filePath);
                var objectPredefinedAcl = PredefinedObjectAcl.PublicRead; // Hace el objeto público

                var Configuracion = new UploadObjectOptions
                {
                    PredefinedAcl = objectPredefinedAcl,
                };
                objectName = $"{UID}/{folderName}/{objectName}";
                var obj = storageClient.UploadObject(NombreDelBucket, objectName, null, fileStream, Configuracion);


                fileStream?.Close();
            }
            catch (Exception)
            {
                throw;
            }

        }

        public static bool RenombrarArchivo(string rutaActual, string nuevoNombre)
        {
            try
            {
                // Verifica si el archivo actual existe
                if (File.Exists(rutaActual))
                {
                    if (File.Exists(nuevoNombre))
                    {
                        // Elimina el archivo
                        File.Delete(nuevoNombre);
                    }
                    // Obtiene la ruta del directorio del archivo actual
                    string directorio = Path.GetDirectoryName(rutaActual);

                    // Combina el directorio con el nuevo nombre de archivo
                    string nuevaRuta = Path.Combine(directorio, nuevoNombre);

                    // Intenta renombrar el archivo
                    File.Move(rutaActual, nuevaRuta);

                    // La operación de renombrado fue exitosa
                    return true;
                }
                else
                {
                    // El archivo actual no existe
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static bool Mutear(string input, string output)
        {
            try
            {
                FFMpeg.Mute(input, output);
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }

        public static string ConvertirVideo(string rutaEntrada, string rutaSalida, string resolucion, string formato)
        {
            try
            {
                // Construye el comando FFmpeg para realizar la conversión
                string comandoFFmpeg = $"-i \"{rutaEntrada}\" -vf scale={resolucion} -c:v libx264 -preset fast -crf 18 -c:a aac -strict experimental -b:a 192K \"{rutaSalida}\"";

                // Crea un proceso para ejecutar FFmpeg
                Process ffmpegProcess = new Process();
                ffmpegProcess.StartInfo.FileName = "ffmpeg"; // Debe estar en el PATH del sistema o especifica la ruta completa
                ffmpegProcess.StartInfo.Arguments = comandoFFmpeg;
                ffmpegProcess.StartInfo.UseShellExecute = false;
                ffmpegProcess.StartInfo.RedirectStandardError = true;
                ffmpegProcess.StartInfo.CreateNoWindow = true;

                // Inicia el proceso FFmpeg
                ffmpegProcess.Start();
                string output = ffmpegProcess.StandardError.ReadToEnd();
                ffmpegProcess.WaitForExit();

                if (ffmpegProcess.ExitCode == 0)
                {

                    return rutaSalida;
                }
                else
                {

                    return "";
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static string ObtenerResolucionVideo(string rutaVideo)
        {
            try
            {
                // Construye el comando FFmpeg para obtener la información del video
                string comandoFFmpeg = $"-i \"{rutaVideo}\"";

                // Crea un proceso para ejecutar FFmpeg
                Process ffmpegProcess = new Process();
                ffmpegProcess.StartInfo.FileName = "ffmpeg"; // Debe estar en el PATH del sistema o especifica la ruta completa
                ffmpegProcess.StartInfo.Arguments = comandoFFmpeg;
                ffmpegProcess.StartInfo.UseShellExecute = false;
                ffmpegProcess.StartInfo.RedirectStandardError = true;
                ffmpegProcess.StartInfo.CreateNoWindow = true;

                // Inicia el proceso FFmpeg
                ffmpegProcess.Start();
                string output = ffmpegProcess.StandardError.ReadToEnd();
                ffmpegProcess.WaitForExit();

                // Busca la línea que contiene la información de la resolución
                string patron = @"(\d{2,4}x\d{2,4})";
                Match match = Regex.Match(output, patron);

                if (match.Success)
                {
                    // Obtiene la resolución encontrada
                    string resolucion = match.Value;

                    // Convierte la resolución a un formato legible
                    switch (resolucion)
                    {
                        case "3840x2160":
                            return "2160p";
                        case "2560x1440":
                            return "1440p";
                        case "1920x1080":
                            return "1080p";
                        case "1280x720":
                            return "720p";
                        case "854x480":
                            return "480p";
                        case "640x360":
                            return "360p";
                        case "426x240":
                            return "240p";
                        case "256x144":
                            return "144p";
                        default:
                            return resolucion;
                    }
                }
                else
                {
                    return "Desconocida";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener la resolución del video: {ex.Message}");
                return "Desconocida";
            }
        }
    }
    //public async static void ProcesarVideoGeneral(string RutaDelVideo, string UID)
    //{
    //    try
    //    {
    //        var DirectorioTemporal = GeneralesMD.CrearDirectorioTemporal();
    //        if (DirectorioTemporal.success)
    //        {
    //            var VDescargarVideo = DescargarVideo(RutaDelVideo, DirectorioTemporal.path);
    //            if (VDescargarVideo.success)
    //            {
    //                string VideoNombre = VDescargarVideo.name.Split(".")[0];
    //                string AudioPath = Path.Combine(DirectorioTemporal.path, VideoNombre + ".mp3");
    //                if (ObtenerAudio(VDescargarVideo.path, AudioPath))
    //                {
    //                    var VDectectarIdioma = DectectarIdioma(AudioPath);
    //                    string VideoSinAudio = Path.Combine(DirectorioTemporal.path, "AudioOFF_" + VDescargarVideo.name);

    //                    if (Mutear(VDescargarVideo.path, VideoSinAudio))
    //                    {
    //                        var ListaDeResoluciones = new List<VideoSize>()
    //                    {
    //                        VideoSize.Ld, VideoSize.Ed, VideoSize.Hd
    //                    };

    //                        var ListaDeResolucionesVideos = new List<(string, double)>();
    //                        var ListaDevideosM3U8 = new List<(string, double)>();
    //                        List<(string,string)> AudioM3U8 = new List<(string, string)>();

    //                        var tasks = new List<Task>();
    //                        foreach (var sizeV in ListaDeResoluciones)
    //                        {
    //                            var task = Task.Run(() =>
    //                            {
    //                                var VCambiarResolucion = CambiarResolucion(VideoSinAudio, sizeV, DirectorioTemporal.path, VideoNombre + ".ts");
    //                                if (VCambiarResolucion.success)
    //                                {
    //                                    ListaDeResolucionesVideos.Add((VCambiarResolucion.path, (double)sizeV));
    //                                }
    //                            });

    //                            tasks.Add(task);
    //                        }
    //                        await Task.WhenAll(tasks);
    //                        tasks.Clear();
    //                        foreach (var item in ListaDeResolucionesVideos)
    //                        {
    //                            var task = Task.Run(() =>
    //                            {
    //                                string M3U8Nombre = $"{item.Item2}_M3U8_{VideoNombre}.m3u8";
    //                                ListaDevideosM3U8.Add((SegmentVideo(item.Item1, DirectorioTemporal.path, M3U8Nombre, UID, VideoNombre, item.Item2.ToString()), item.Item2));
    //                            });
    //                            tasks.Add(task);
    //                        }
    //                        string AudioM3U8Name = $"Audio_M3U8_{VideoNombre}.m3u8";
    //                        AudioM3U8.Add((SegmentVideo(AudioPath, DirectorioTemporal.path, AudioM3U8Name, UID, VideoNombre, "Audio", true), VDectectarIdioma.idiomaCode));
    //                        await Task.WhenAll(tasks);
    //                        (string, string) master = CreateMasterM3U(AudioM3U8, ListaDevideosM3U8, DirectorioTemporal.path, VideoNombre);
    //                        UploadFileToGCS(master.Item1, master.Item2, UID, VideoNombre);

    //                    }
    //                }

    //            }

    //        }
    //    }
    //    catch (Exception)
    //    {

    //        throw;
    //    }
    //}

    //public static string SegmentVideo(string inputVideoPath, string outputFolder, string outputM3U8FileName, string UID, string NombreVideo, string resolucion, bool audio = false)
    //{
    //    // Verifica si la carpeta de salida existe, y si no, créala.
    //    if (!Directory.Exists(outputFolder))
    //    {
    //        Directory.CreateDirectory(outputFolder);
    //    }

    //    // Configura el comando FFmpeg para segmentar el video en partes y generar el archivo M3U8.
    //    string outputM3U8Path = Path.Combine(outputFolder, outputM3U8FileName);
    //    string ffmpegCmd;
    //    if (audio)
    //    {
    //        ffmpegCmd = $"-i \"{inputVideoPath}\" -hls_time 2 -hls_list_size 0 -hls_segment_filename \"{outputFolder}/{NombreVideo}_AudioP_segment_%03d.mp3\" \"{outputM3U8Path}\"";
    //    }
    //    else
    //    {
    //        ffmpegCmd = $"-i \"{inputVideoPath}\" -hls_time 2 -hls_list_size 0 -hls_segment_filename \"{outputFolder}/{NombreVideo}_{resolucion}P_segment_%03d.ts\" \"{outputM3U8Path}\"";
    //    }

    //    Process process = new Process
    //    {
    //        StartInfo = new ProcessStartInfo
    //        {
    //            FileName = "ffmpeg",
    //            Arguments = ffmpegCmd,
    //            RedirectStandardOutput = true,
    //            UseShellExecute = false,
    //            CreateNoWindow = true,
    //        }
    //    };

    //    process.Start();
    //    process.WaitForExit();

    //    if (File.Exists(outputM3U8Path))
    //    {
    //        return ModifySegments(outputM3U8Path, outputFolder, UID, NombreVideo, outputM3U8FileName);
    //    }
    //    else
    //    {
    //        return "";
    //    }
    //}

    //public static string ModifySegments(string m3u8FilePath, string PathFolder, string UID, string VideoNombre, string m3u8Nombre)
    //{
    //    try
    //    {
    //        using (FileStream fileStream = File.OpenRead(m3u8FilePath))
    //        {
    //            string tempFilePath = Path.Combine(PathFolder, "File_" + m3u8Nombre);
    //            using (FileStream fileStreamwriter = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
    //            {
    //                using (StreamReader reader = new StreamReader(fileStream))
    //                {
    //                    using (StreamWriter writer = new StreamWriter(fileStreamwriter))
    //                    {
    //                        string line;
    //                        while ((line = reader.ReadLine()) != null)
    //                        {
    //                            if (line.StartsWith("#EXTINF:"))
    //                            {
    //                                // Puedes cambiar la duración del segmento aquí si es necesario.
    //                                writer.WriteLine(line);
    //                            }
    //                            else if (line.EndsWith(".ts") || line.EndsWith(".mp3"))
    //                            {
    //                                string Ubicacion = Path.Combine(PathFolder, line);
    //                                var data = UploadFileToGCS(Ubicacion, line, UID, VideoNombre);
    //                                if (data.success)
    //                                {
    //                                    // Reemplaza la URL del segmento con la nueva URL de GCS.
    //                                    writer.WriteLine(data.path);
    //                                }
    //                            }
    //                            else
    //                            {
    //                                writer.WriteLine(line);
    //                            }
    //                        }
    //                    }
    //                }

    //            }
    //            return UploadFileToGCS(tempFilePath, m3u8Nombre, UID, VideoNombre).path;

    //        }
    //    }
    //    catch (Exception)
    //    {
    //        return "";
    //    }
    //}

    //public static (bool success, string path, string name, string formato) DescargarVideo(string ruta, string directorio)
    //{
    //    try
    //    {
    //        string credentialsPath = @".\seelt-987cd-7dbe2c8a55dc.json"; // Ruta completa del archivo JSON de credenciales
    //        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
    //        var client = StorageClient.Create();
    //        var stream = new MemoryStream();
    //        var obj = client.DownloadObject("seelt-987cd.appspot.com", ruta, stream);
    //        stream.Position = 0;

    //        string rutaCompleta = Path.Combine(directorio, obj.Name);

    //        using (var fileStream = File.Create(rutaCompleta))
    //        {
    //            stream.CopyTo(fileStream);
    //        }
    //        return (true, rutaCompleta, obj.Name, ruta.Split('.')[1]);
    //    }
    //    catch (Exception)
    //    {
    //        return (false, "", "", "");
    //    }
    //}

    //public static (bool success, string path) ObtenerMiniatura(string input, string output, int time)
    //{
    //    try
    //    {
    //        FFMpeg.Snapshot(input, output, null, TimeSpan.FromSeconds(time));
    //        return (true, output);
    //    }
    //    catch (Exception)
    //    {

    //        return (false, output);
    //    }
    //}

    //public static (bool success, string path) CambiarResolucion(string input, VideoSize videoSize, string Directorio, string NombreVideo)
    //{
    //    string output = "";
    //    try
    //    {

    //        switch (videoSize)
    //        {
    //            case VideoSize.FullHd:
    //                break;
    //            case VideoSize.Hd:
    //                output = Path.Combine(Directorio, "720p_" + NombreVideo);
    //                break;
    //            case VideoSize.Ed:
    //                output = Path.Combine(Directorio, "480p_" + NombreVideo);
    //                break;
    //            case VideoSize.Ld:
    //                output = Path.Combine(Directorio, "360p_" + NombreVideo);
    //                break;
    //            case VideoSize.Original:
    //                break;
    //            default:
    //                break;
    //        }
    //        FFMpeg.Convert(input, output, VideoType.Ts, Speed.SuperFast, videoSize, AudioQuality.Normal, true);
    //        return (true, output);
    //    }
    //    catch (Exception)
    //    {

    //        return (false, output);
    //    }
    //}

    //public static bool ObtenerAudio(string input, string output)
    //{
    //    try
    //    {
    //        FFMpeg.ExtractAudio(input, output);
    //        return true;
    //    }
    //    catch (Exception)
    //    {

    //        return false;
    //    }
    //}

    //public static bool Mutear(string input, string output)
    //{
    //    try
    //    {
    //        FFMpeg.Mute(input, output);
    //        return true;
    //    }
    //    catch (Exception)
    //    {

    //        return false;
    //    }
    //}

    //public static (bool success, string idiomaCode) DectectarIdioma(string pathToAudioFile)
    //{
    //    try
    //    {
    //        var speech = SpeechClient.Create();

    //        var response_en = speech.Recognize(new RecognitionConfig
    //        {
    //            Encoding = RecognitionConfig.Types.AudioEncoding.EncodingUnspecified,
    //            SampleRateHertz = 44100,
    //            LanguageCode = "en-US",
    //        }, RecognitionAudio.FromFile(pathToAudioFile));

    //        var response_es = speech.Recognize(new RecognitionConfig
    //        {
    //            Encoding = RecognitionConfig.Types.AudioEncoding.EncodingUnspecified,
    //            SampleRateHertz = 44100,
    //            LanguageCode = "es-ES",
    //        }, RecognitionAudio.FromFile(pathToAudioFile));

    //        // Verificar si hay resultados en inglés y español
    //        if (response_en.Results.Count > 0 && response_es.Results.Count > 0)
    //        {
    //            float confidence_en = response_en.Results[0].Alternatives[0].Confidence;
    //            float confidence_es = response_es.Results[0].Alternatives[0].Confidence;

    //            // Determinar el idioma con mayor confianza
    //            if (confidence_en > confidence_es)
    //            {
    //                return (true, "Ingles");
    //            }
    //            else
    //            {
    //                return (true, "Español");
    //            }
    //        }
    //        else if (response_en.Results.Count > 0)
    //        {
    //            return (true, "Ingles");
    //        }
    //        else if (response_es.Results.Count > 0)
    //        {
    //            return (true, "Español");
    //        }
    //        else
    //        {
    //            return (false, "");
    //        }
    //    }
    //    catch (Exception)
    //    {
    //        return (false, "");
    //    }
    //}

    //public static (bool success, string path) UploadFileToGCS(string filePath, string objectName, string UID, string folderName = "")
    //{
    //    string credentialsPath = @".\seelt-987cd-7dbe2c8a55dc.json"; // Ruta completa del archivo JSON de credenciales
    //    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
    //    var storageClient = StorageClient.Create();
    //    string bucketName = "seelt-987cd.appspot.com";

    //    try
    //    {
    //        FileStream fileStream = File.OpenRead(filePath);
    //        var objectPredefinedAcl = PredefinedObjectAcl.PublicRead; // Hace el objeto público

    //        if (!string.IsNullOrWhiteSpace(folderName))
    //        {
    //            objectName = $"{UID}/{folderName}/{objectName}";
    //        }

    //        // Sube el archivo al bucket con configuración para hacerlo público

    //        var Configuracion = new UploadObjectOptions
    //        {
    //            PredefinedAcl = objectPredefinedAcl,
    //        };

    //        var obj = storageClient.UploadObject(bucketName, objectName, null, fileStream, Configuracion);

    //        // Obtiene la URL pública del archivo
    //        var publicUrl = $"https://storage.googleapis.com/{bucketName}/{objectName}";
    //        fileStream?.Close();
    //        return (true, publicUrl);
    //    }
    //    catch (Exception)
    //    {
    //        return (false, "");
    //    }

    //}

    //public static (string, string) CreateMasterM3U(List<(string, string)> AudioM3U8List, List<(string, double)> videoM3U8List, string path, string VideoName, List<(string, string)> SubM3U8list = null)
    //{
    //    try
    //    {
    //        SubM3U8list = new List<(string, string)>();
    //        ListasDeM3U8 listasDeM3U8 = new ListasDeM3U8()
    //        {
    //            audio = AudioM3U8List,
    //            video = videoM3U8List,
    //            sub = SubM3U8list
    //        };

    //        string masterM3UPath = Path.Combine(path, VideoName + "_master.m3u8"); // Nombre del archivo M3U maestro

    //        using (StreamWriter writer = new StreamWriter(masterM3UPath))
    //        {
    //            int x = 0;
    //            writer.WriteLine("#EXTM3U");
    //            writer.WriteLine("#EXT-X-VERSION:6");
    //            writer.WriteLine("#EXT-X-INDEPENDENT-SEGMENTS");

    //            //VIDEO
    //            foreach (var videoM3U8 in videoM3U8List)
    //            {
    //                writer.WriteLine($"#EXT-X-STREAM-INF:AVERAGE-BANDWIDTH={CalculateBandwidth(videoM3U8.Item2)},BANDWIDTH={CalculateBandwidth(videoM3U8.Item2)},CODECS=\"avc1.640020,MP3 \",RESOLUTION={ObtenerAncho(videoM3U8.Item2.ToString())}x{videoM3U8.Item2},FRAME-RATE=60.000{AgegarSubAudio(listasDeM3U8)}");
    //                writer.WriteLine(videoM3U8.Item1);
    //            }

    //            //Audio
    //            foreach (var AudioM3U8 in AudioM3U8List)
    //            {
    //                writer.WriteLine($"#EXT-X-MEDIA:TYPE=AUDIO,GROUP-ID=\"aud{x}\",LANGUAGE=\"{ObtenerCodigoDeIdioma(AudioM3U8.Item2)}\",NAME=\"{AudioM3U8.Item2}\",AUTOSELECT=YES,DEFAULT=YES,CHANNELS=\"2\",URI=\"{AudioM3U8.Item1}\"");
    //                x++;
    //            }
    //            writer.WriteLine($"#EXT-X-MEDIA:TYPE=CLOSED-CAPTIONS,GROUP-ID=\"cc0\",LANGUAGE=\"{ObtenerCodigoDeIdioma(AudioM3U8List[0].Item2)}\",NAME=\"{AudioM3U8List[0].Item2}\",AUTOSELECT=YES,DEFAULT=YES,INSTREAM-ID=\"cc0\"");
    //            x = 0;
    //            foreach (var SuboM3U8 in SubM3U8list)
    //            {
    //                writer.WriteLine($"#EXT-X-MEDIA:TYPE=SUBTITLES,GROUP-ID=\"sub{x}\",LANGUAGE=\"{ObtenerCodigoDeIdioma(SuboM3U8.Item2)}\",NAME=\"{SuboM3U8.Item2}\",AUTOSELECT=YES,DEFAULT=YES,FORCED=NO,URI=\"{SuboM3U8.Item1}\"");
    //            }
    //        }

    //        return (masterM3UPath, VideoName + "_master.m3u8");
    //    }
    //    catch (Exception)
    //    {
    //        return ("", "");
    //    }
    //}

    //public static string ObtenerAncho(string H)
    //{
    //    switch (H)
    //    {
    //        case "720":
    //            return "1280";
    //        case "480":
    //            return "854";
    //        case "360":
    //            return "640";
    //    }
    //    return "";
    //}

    //public static string ObtenerCodigoDeIdioma(string H)
    //{
    //    switch (H)
    //    {
    //        case "Ingles":
    //            return "en";
    //        case "Español":
    //            return "es";
    //    }
    //    return "";
    //}

    //public static int CalculateBandwidth(double resolution)
    //{
    //    // Calcular el ancho de banda basado en la resolución (ajusta el valor según sea necesario)
    //    int bandwidth = (int)(resolution * 1000000 / 2); // Ajusta este cálculo según tus necesidades.
    //    return bandwidth;
    //}

    //public class ListasDeM3U8
    //{
    //    public List<(string, string)> audio { get; set;}
    //    public List<(string, double)> video { get; set;}
    //    public List<(string, string)> sub { get; set;}
    //}

    //public static string AgegarSubAudio(ListasDeM3U8 listasDeM3U8) 
    //{
    //    string txt = "";
    //    StringBuilder builder = new StringBuilder(txt);
    //    if (listasDeM3U8.video.Count > 0 || listasDeM3U8.video == null)
    //    {
    //        builder.Append(",CLOSED-CAPTIONS=\"cc0\"");
    //    }
    //    if (listasDeM3U8.audio.Count > 0 || listasDeM3U8.audio == null)
    //    {
    //        builder.Append(",AUDIO=\"aud0\"");
    //    }
    //    if (listasDeM3U8.sub.Count > 0 || listasDeM3U8.sub == null)
    //    {
    //        builder.Append(",SUBTITLES=\"sub0\"");
    //    }

    //    return builder.ToString();
    //}
}
