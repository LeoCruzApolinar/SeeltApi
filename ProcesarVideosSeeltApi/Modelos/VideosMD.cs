using FFMpegCore;
using Google.Cloud.Storage.V1;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static ProcesarVideosSeeltApi.Modelos.IAMD;
using static ProcesarVideosSeeltApi.Modelos.VideosMD;


namespace ProcesarVideosSeeltApi.Modelos
{
    public class VideosMD
    {
        public static string CloudCredenciales = @".\seelt-987cd-7dbe2c8a55dc.json";

        private static string NombreDelBucket = @"seelt-987cd.appspot.com";

        public class VideosCS
        {
            public string Ubicacion { get; set; }
            public string Resolucion { get; set; }
        }

        public class VideoPeticion
        {
            public string UbicacionVideoOriginal { get; set; }
            public string NombreUnico { get; set; }
            public string Directorio { get; set; }
            public string Formato { get; set; }
            public string UID { get; set; }
            public string TituloVideo { get; set; }
            public string? Descripcion { get; set; }
            public string? URL_Miniatura { get; set; }
            public List<Subtitulo>? ListaSubtitulos = new List<Subtitulo>();
            public List<Audio>? ListaAudios = new List<Audio>();
        }

        public class AudioFile
        {
            public FormFile Archivo { get; set; }
            public string Idioma { get; set; }
        }

        public class SubtituloFile
        {
            public FormFile Archivo { get; set; }
            public string Idioma { get; set; }
        }

        public class Subtitulo
        {
            public string Ubicacion { get; set;}
            public string Idioma { get; set;}
        }

        public class Audio
        {
            public string Ubicacion { get; set; }
            public string Idioma { get; set; }
        }

        public class VideoInterval
        {
            public TimeSpan Inicio { get; set; }
            public TimeSpan Duracion { get; set; }
        }

        public class VideoSegmentos
        {
            public string Resolucion { get; set; }
            public List<string> Ubicaciones = new List<string>();
        }

        public async void ProcesarVideoGeneral(VideoPeticion videoPeticion)
        {
            try
            {
                string UbicacionAudio = Path.Combine(videoPeticion.Directorio, $"Audio.mp3");
                string Audio = ObtenerAudio(videoPeticion.UbicacionVideoOriginal, UbicacionAudio);

                var _taskB = Task.Run(async () =>
                {
                    int TiempoDeTranscripcion = 240;
                    int Tiempo = 0;
                    var seg = ObtenerDuracion(UbicacionAudio).TotalSeconds;
                    List<string> AudiosUbicacion = new List<string>();
                    int a = 0;
                    while ((seg - Tiempo) > 60)
                    {
                        string outputFile = Path.Combine(Path.GetDirectoryName(Audio), "Audio"+a+"_.mp3");
                        AudioMD.TrimAudio(Audio, outputFile, Tiempo, 59);
                        Tiempo = Tiempo + 60;
                        a ++;
                        AudiosUbicacion.Add(outputFile);
                        if (Tiempo == 240 || (seg - Tiempo) < 60)
                        {
                            break;
                        }
                    }
                    if (a == 0 && seg < 60)
                    {
                        string outputFile = Path.Combine(Path.GetDirectoryName(Audio), "Audio" + a + "_.mp3");
                        AudioMD.TrimAudio(Audio, outputFile, 0, (int)seg);
                        AudiosUbicacion.Add(outputFile);
                    }
                    string txt = "";
                    string idioma = "";
                    foreach (var item in AudiosUbicacion)
                    {
                        var result = await IAMD.Transcriber.DetectarIdiomaYTranscribir(item);
                        txt = txt + " " + result.Item2;
                        idioma = result.Item1;
                    }
                    return (idioma, txt);
                });

                string VideoSinAudio = Path.Combine(videoPeticion.Directorio, "videoSinAudio." + videoPeticion.Formato);
                Mutear(videoPeticion.UbicacionVideoOriginal, VideoSinAudio);
                RenombrarArchivo(VideoSinAudio, videoPeticion.UbicacionVideoOriginal);
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", CloudCredenciales);

                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();

                var _task = Task.Run(async () =>
                {
                    List<VideosCS> ListaVideosCS = ObtenerVideoEnDiferenteResoluciones(videoPeticion);
                    return ListaVideosCS;
                });

                await _task;

                var ListaVideosCS = _task.Result;

                List<VideosCS> Carpetas = new List<VideosCS>();

                List<VideosCS> ListaDeM3U8 = new List<VideosCS>();

                foreach (var item in ListaVideosCS)
                {
                    VideosCS videosCS = new VideosCS();
                    VideosCS M3U8 = new VideosCS();
                    string carpetaNueva = $"{Path.Combine(videoPeticion.Directorio, item.Resolucion)}";
                    if (!Directory.Exists(carpetaNueva))
                    {
                        // Crear la carpeta
                        Directory.CreateDirectory(carpetaNueva);
                        Console.WriteLine("Carpeta creada con éxito.");
                    }
                    else
                    {
                        Console.WriteLine("La carpeta ya existe.");
                    }

                    ConvertVideoToHLS(item.Ubicacion, Path.Combine(carpetaNueva, $"FIle_{item.Resolucion}_index.m3u8"));
                    videosCS.Ubicacion = carpetaNueva;
                    videosCS.Resolucion = item.Resolucion;
                    Carpetas.Add(videosCS);
                    M3U8.Resolucion = item.Resolucion;
                    M3U8.Ubicacion = Path.Combine(carpetaNueva, $"FIle_{item.Resolucion}_index.m3u8");
                    ListaDeM3U8.Add(M3U8);
                }

                List<VideoSegmentos> ListVideoSegmentos = new List<VideoSegmentos>();

                foreach (var carpeta in Carpetas)
                {
                    VideoSegmentos videoSegmentos = new VideoSegmentos();
                    List<string> archivosTS = new List<string>();
                    foreach (var item in Directory.GetFiles(carpeta.Ubicacion, "*.ts").ToList())
                    {
                        archivosTS.Add(SubirArchivoGCS(item, videoPeticion.UID, $"{videoPeticion.NombreUnico}/{carpeta.Resolucion}"));
                    }
                    videoSegmentos.Ubicaciones = archivosTS;
                    videoSegmentos.Resolucion = carpeta.Resolucion;
                    ListVideoSegmentos.Add(videoSegmentos);
                }

                int a = 0;

                List<VideosCS> ListaDeM3U8Web = new List<VideosCS>();
                foreach (var item in ListaDeM3U8)
                {
                    VideosCS videosCS = new VideosCS();
                    string NuevoM3U8 = Path.Combine(Path.GetDirectoryName(item.Ubicacion), $"{item.Resolucion}_index.m3u8");
                    ModificarM3U8(item.Ubicacion, NuevoM3U8, Directory.GetFiles(Path.GetDirectoryName(item.Ubicacion), "*.ts").ToList(), ListVideoSegmentos[a].Ubicaciones);
                    videosCS.Ubicacion = SubirArchivoGCS(NuevoM3U8, videoPeticion.UID, $"{videoPeticion.NombreUnico}/{item.Resolucion}");
                    videosCS.Resolucion = item.Resolucion;
                    ListaDeM3U8Web.Add(videosCS);
                    a++;
                }

                string carpetaNuevaAudio = $"{Path.Combine(videoPeticion.Directorio, "Audio")}";
                if (!Directory.Exists(carpetaNuevaAudio))
                {
                    // Crear la carpeta
                    Directory.CreateDirectory(carpetaNuevaAudio);
                    Console.WriteLine("Carpeta creada con éxito.");
                }
                else
                {
                    Console.WriteLine("La carpeta ya existe.");
                }

                string M3U8_Audio = Path.Combine(carpetaNuevaAudio, $"FIle_Audio_index.m3u8");
                string M3U8_AudioNew = Path.Combine(carpetaNuevaAudio, $"Audio_index.m3u8");

                ConvertVideoToHLS(UbicacionAudio, M3U8_Audio);

                if (Directory.Exists(carpetaNuevaAudio))
                {
                    List<string> archivosTSAudio = new List<string>();

                    foreach (var item in Directory.GetFiles(carpetaNuevaAudio, "*.ts").ToList())
                    {
                        archivosTSAudio.Add(SubirArchivoGCS(item, videoPeticion.UID, $"{videoPeticion.NombreUnico}/Audio"));
                    }
                    ModificarM3U8(M3U8_Audio, M3U8_AudioNew, Directory.GetFiles(carpetaNuevaAudio, "*.ts").ToList(), archivosTSAudio);
                    M3U8_AudioNew = SubirArchivoGCS(M3U8_AudioNew, videoPeticion.UID, $"{videoPeticion.NombreUnico}/Audio");
                }
                else
                {
                    Console.WriteLine($"La carpeta {M3U8_Audio} no existe.");
                }

                await _taskB;
                var resultado = _taskB.Result;

                string master = CrearMasterM3U8_Simple(videoPeticion, ListaDeM3U8Web, M3U8_AudioNew, resultado.idioma);
                SubirArchivoGCS(master, videoPeticion.UID, $"{videoPeticion.NombreUnico}");

                var chatGPT = new ChatGPT();

                var responseT = await chatGPT.GetVideoCategories(resultado.txt);
                var ListaDeCategoriasXTranscripcion = chatGPT.ExtraerCategorias(responseT, GeneralesMD.ObtenerLasCategorias());

                var responseTI = await chatGPT.GetVideoCategories(videoPeticion.TituloVideo);
                var ListaDeCategoriasXTitulo = chatGPT.ExtraerCategorias(responseTI, GeneralesMD.ObtenerLasCategorias());

                var responseD = await chatGPT.GetVideoCategories(videoPeticion.Descripcion);
                var ListaDeCategoriasXDescripcion = chatGPT.ExtraerCategorias(responseD, GeneralesMD.ObtenerLasCategorias());

                var SimilarT = chatGPT.EncontrarPalabrasSimilares(resultado.txt, GeneralesMD.ObtenerLasCategorias());
                var SimilarTi = chatGPT.EncontrarPalabrasSimilares(videoPeticion.TituloVideo, GeneralesMD.ObtenerLasCategorias());
                var SimilarD = chatGPT.EncontrarPalabrasSimilares(videoPeticion.Descripcion, GeneralesMD.ObtenerLasCategorias());

                var lc = chatGPT.EliminarDuplicados(chatGPT.EncontrarPalabrasComunes(ListaDeCategoriasXTranscripcion, ListaDeCategoriasXTitulo, ListaDeCategoriasXDescripcion, SimilarT, SimilarTi, SimilarD));

                stopwatch.Stop();
                Console.WriteLine("Tiempo transcurrido: " + stopwatch.Elapsed);
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public async void ProcesarVideoAvanzado(VideoPeticion videoPeticion)
        {
            try
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", CloudCredenciales);

                var _task = Task.Run(async () =>
                {
                    List<VideosCS> ListaVideosCS = ObtenerVideoEnDiferenteResoluciones(videoPeticion);
                    return ListaVideosCS;
                });

                await _task;

                var ListaVideosCS = _task.Result;

                List<VideosCS> Carpetas = new List<VideosCS>();

                List<VideosCS> ListaDeM3U8 = new List<VideosCS>();

                foreach (var item in ListaVideosCS)
                {
                    VideosCS videosCS = new VideosCS();
                    VideosCS M3U8 = new VideosCS();
                    string carpetaNueva = $"{Path.Combine(videoPeticion.Directorio, item.Resolucion)}";
                    if (!Directory.Exists(carpetaNueva))
                    {
                        // Crear la carpeta
                        Directory.CreateDirectory(carpetaNueva);
                        Console.WriteLine("Carpeta creada con éxito.");
                    }
                    else
                    {
                        Console.WriteLine("La carpeta ya existe.");
                    }

                    ConvertVideoToHLS(item.Ubicacion, Path.Combine(carpetaNueva, $"FIle_{item.Resolucion}_index.m3u8"));
                    videosCS.Ubicacion = carpetaNueva;
                    videosCS.Resolucion = item.Resolucion;
                    Carpetas.Add(videosCS);
                    M3U8.Resolucion = item.Resolucion;
                    M3U8.Ubicacion = Path.Combine(carpetaNueva, $"FIle_{item.Resolucion}_index.m3u8");
                    ListaDeM3U8.Add(M3U8);
                }

                List<VideoSegmentos> ListVideoSegmentos = new List<VideoSegmentos>();

                foreach (var carpeta in Carpetas)
                {
                    VideoSegmentos videoSegmentos = new VideoSegmentos();
                    List<string> archivosTS = new List<string>();
                    foreach (var item in Directory.GetFiles(carpeta.Ubicacion, "*.ts").ToList())
                    {
                        archivosTS.Add(SubirArchivoGCS(item, videoPeticion.UID, $"{videoPeticion.NombreUnico}/{carpeta.Resolucion}"));
                    }
                    videoSegmentos.Ubicaciones = archivosTS;
                    videoSegmentos.Resolucion = carpeta.Resolucion;
                    ListVideoSegmentos.Add(videoSegmentos);
                }

                List<Audio> ListaM3u8Audio = new List<Audio>();
                foreach (var item in videoPeticion.ListaAudios)
                {
                    string carpetaNueva = $"{Path.Combine(videoPeticion.Directorio, item.Idioma)}";
                    Audio Audio_M3U8 = new Audio();
                    if (!Directory.Exists(carpetaNueva))
                    {
                        // Crear la carpeta
                        Directory.CreateDirectory(carpetaNueva);
                        Console.WriteLine("Carpeta creada con éxito.");
                    }
                    else
                    {
                        Console.WriteLine("La carpeta ya existe.");
                    }
                    ConvertVideoToHLS(item.Ubicacion, Path.Combine(carpetaNueva, $"FIle_{item.Idioma}_index.m3u8"));
                    Audio_M3U8.Idioma = item.Idioma;
                    Audio_M3U8.Ubicacion = Path.Combine(carpetaNueva, $"FIle_{item.Idioma}_index.m3u8");
                    ListaM3u8Audio.Add(Audio_M3U8);
                }


                Dictionary<string, List<string>> ListAudioSegmentos = new Dictionary<string, List<string>>();
                foreach (var item in ListaM3u8Audio)
                {
                    List<string> archivosTS = new List<string>();
                    string Carpeta = Path.GetDirectoryName(item.Ubicacion);
                    foreach (var itemA in Directory.GetFiles(Carpeta, "*.ts").ToList())
                    {
                        archivosTS.Add(SubirArchivoGCS(itemA, videoPeticion.UID, $"{videoPeticion.NombreUnico}/{Carpeta}"));
                    }
                    ListAudioSegmentos.Add(item.Idioma, archivosTS);
                }
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public string ObtenerAudio(string UbicacionDelVideoEnELDirectorio, string UbicacionDelAudio)
        {
            try
            {
                FFMpeg.ExtractAudio(UbicacionDelVideoEnELDirectorio, UbicacionDelAudio);
                return UbicacionDelAudio;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void ConvertVideoToHLS(string UbicacionVideo, string UbicacionM3U8)
        {
            try
            {
                string ffmpegCommand = $"-i {UbicacionVideo} -hls_time 2 -hls_list_size 0 {UbicacionM3U8}";
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
            catch (Exception)
            {

                throw;
            }
        }

        public TimeSpan ObtenerDuracion(string inputFilePath)
        {
            try
            {
                if (!File.Exists(inputFilePath))
                {
                    throw new FileNotFoundException("El archivo de video no existe.", inputFilePath);
                }

                string ffmpegPath = "./ffmpeg.exe"; // Reemplaza con la ubicación de tu ejecutable FFmpeg

                // Ejecutar FFmpeg para obtener información del video
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = $"-i \"{inputFilePath}\"",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // Buscar la duración en la salida de FFmpeg usando una expresión regular
                Regex regex = new Regex(@"Duration: (\d+:\d+:\d+\.\d+)");
                Match match = regex.Match(output);

                if (match.Success)
                {
                    string durationString = match.Groups[1].Value;
                    TimeSpan duration = TimeSpan.Parse(durationString);
                    return duration;
                }
                else
                {
                    throw new InvalidOperationException("No se pudo obtener la duración del video.");
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public  List<VideosCS> ObtenerVideoEnDiferenteResoluciones(VideoPeticion videoPeticion)
        {
            try
            {
                List<VideosCS> ListavideosCs = new List<VideosCS>();
                var Resoluciones = ObtenerResolucionVideo(videoPeticion.UbicacionVideoOriginal);

                Parallel.ForEach(Resoluciones, resolucion =>
                {
                    string Nombre = $"{resolucion}_{videoPeticion.NombreUnico}.{videoPeticion.Formato}";
                    string _Ubicacion = CambiarResolucion(videoPeticion.UbicacionVideoOriginal, Path.Combine(videoPeticion.Directorio, Nombre), resolucion, videoPeticion.Formato);
                    //string URL = SubirArchivoGCS(_Ubicacion, videoPeticion.UID, videoPeticion.NombreUnico);
                    VideosCS videosCS = new VideosCS()
                    {
                        Ubicacion = _Ubicacion,
                        Resolucion = resolucion,
                    };

                    lock (ListavideosCs) // Asegúrate de que la lista sea segura para subprocesos
                    {
                        ListavideosCs.Add(videosCS);
                    }
                });

                return ListavideosCs;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public  List<string> ObtenerResolucionVideo(string rutaVideo)
        {
            try
            {
                List<string> Resoluciones = new List<string>();
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
                            Resoluciones.Add("3840x2160");
                            goto case "2560x1440";
                        case "2560x1440":
                            Resoluciones.Add("2560x1440");
                            goto case "1920x1080";
                        case "1920x1080":
                            Resoluciones.Add("1920x1080");
                            goto case "1280x720";
                        case "1280x720":
                            Resoluciones.Add("1280x720");
                            goto case "854x480";
                        case "854x480":
                            Resoluciones.Add("854x480");
                            goto case "640x360";
                        case "640x360":
                            Resoluciones.Add("640x360");
                            goto case "426x240";
                        case "426x240":
                            Resoluciones.Add("426x240");
                            goto case "256x144";
                        case "256x144":
                            Resoluciones.Add("256x144");
                            break;
                        default:
                            Resoluciones.Add("144p");
                            break;
                    }
                    return Resoluciones;
                }
                else
                {
                    return Resoluciones;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener la resolución del video: {ex.Message}");
                return null;
            }
        }

        public string CambiarResolucion(string rutaEntrada, string rutaSalida, string resolucion, string formato)
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
                
                Console.WriteLine(ex.Message);
                return "";
            }
        }

        public string SubirArchivoGCS(string rutaArchivo, string UID, string CarpetaDeAlojamiento)
        {
            var storageClient = StorageClient.Create();
            string NombreDelObjeto = Path.GetFileName(rutaArchivo);
            try
            {
                using (FileStream fileStream = File.OpenRead(rutaArchivo))
                {
                    var objectPredefinedAcl = PredefinedObjectAcl.PublicRead;
                    var Configuracion = new UploadObjectOptions
                    {
                        PredefinedAcl = objectPredefinedAcl,
                    };
                    NombreDelObjeto = $"{UID}/{CarpetaDeAlojamiento}/{NombreDelObjeto}";

                    // Subir el objeto a GCS
                    storageClient.UploadObject(NombreDelBucket, NombreDelObjeto, null, fileStream, Configuracion);
                }
                // Construir y retornar la URL pública del objeto
                string urlPublica = $"https://storage.googleapis.com/{NombreDelBucket}/{NombreDelObjeto}";
                return urlPublica;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }

        public void ModificarM3U8(string rutaM3U8Original, string rutaM3U8Modificado, List<string> originales, List<string> nuevos)
        {
            try
            {

                List<string> lineasOriginales = new List<string>(File.ReadAllLines(rutaM3U8Original));


                Dictionary<string, string> archivoMapeo = new Dictionary<string, string>();
                int x = 0;
                foreach (var item in originales)
                {
                    archivoMapeo.Add(item, nuevos[x]);
                    x++;
                }


                for (int i = 0; i < lineasOriginales.Count; i++)
                {
                    string linea = lineasOriginales[i];
                    if (!linea.StartsWith("#") && archivoMapeo.ContainsKey(Path.Combine(Path.GetDirectoryName(rutaM3U8Original), linea)))
                    {

                        lineasOriginales[i] = archivoMapeo[Path.Combine(Path.GetDirectoryName(rutaM3U8Original), linea)];
                    }
                }


                File.WriteAllLines(rutaM3U8Modificado, lineasOriginales);

                Console.WriteLine("Archivo M3U8 modificado con éxito.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al modificar el archivo M3U8: {ex.Message}");
            }
        }

        public string CrearMasterM3U8_Simple(VideoPeticion videoPeticion, List<VideosCS> ListaDeM3U8Web, string UbicacionAudio, string AudioIdioma)
        {
            try
            {

                string masterM3UPath = Path.Combine(videoPeticion.Directorio, videoPeticion.NombreUnico + "_master.m3u8"); // Nombre del archivo M3U maestro

                using (StreamWriter writer = new StreamWriter(masterM3UPath))
                {
                    int x = 0;
                    writer.WriteLine("#EXTM3U");
                    writer.WriteLine("#EXT-X-VERSION:6");
                    writer.WriteLine("#EXT-X-INDEPENDENT-SEGMENTS");

                    //VIDEO
                    foreach (var videoM3U8 in ListaDeM3U8Web)
                    {
                        string[] partes = videoM3U8.Resolucion.Split('x');

                        int Ancho = int.Parse(partes[0]);
                        int Alto = int.Parse(partes[1]);

                        writer.WriteLine($"#EXT-X-STREAM-INF:AVERAGE-BANDWIDTH={CalculateBandwidth(Ancho)},BANDWIDTH={CalculateBandwidth(Alto)},CODECS=\"avc1.640020\",RESOLUTION={Ancho}x{Alto},FRAME-RATE=60.000,AUDIO=\"aud0\"");
                        writer.WriteLine(videoM3U8.Ubicacion);
                    }
                    writer.WriteLine($"#EXT-X-MEDIA:TYPE=AUDIO,GROUP-ID=\"aud0\",LANGUAGE=\"{ObtenerCodigoDeIdioma(AudioIdioma)}\",NAME=\"{AudioIdioma}\",AUTOSELECT=YES,DEFAULT=YES,CHANNELS=\"2\",URI=\"{UbicacionAudio}\"");
                }

                return masterM3UPath;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static int CalculateBandwidth(double resolution)
        {
            try
            {
                // Calcular el ancho de banda basado en la resolución (ajusta el valor según sea necesario)
                int bandwidth = (int)(resolution * 1000000 / 2); // Ajusta este cálculo según tus necesidades.
                return bandwidth;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public static string ObtenerCodigoDeIdioma(string H)
        {
            switch (H)
            {
                case "Ingles":
                    return "en";
                case "Español":
                    return "es";
            }
            return "";
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

    }
}




