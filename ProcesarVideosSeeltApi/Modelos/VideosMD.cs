using Google.Cloud.Storage.V1;
using System.Diagnostics;
using System.Text.RegularExpressions;


namespace ProcesarVideosSeeltApi.Modelos
{
    public class VideosMD
    {
        private Dictionary<string, string> DResoluciones = new Dictionary<string, string>
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

        //Ubicacion de la credenciales de google cloud
        private static string CloudCredenciales = @".\seelt-987cd-7dbe2c8a55dc.json";
        //Nombre del bucket de google cloud
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
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", CloudCredenciales);

            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            var _task = Task.Run(async () =>
            {
                List<VideosCS> ListaVideosCS = ObtenerVideoEnDiferenteResoluciones(videoPeticion);
                return ListaVideosCS;
            });

            List<VideoInterval> ListaVideoIntervals = ObtenerIntervalos(videoPeticion.UbicacionVideoOriginal, TimeSpan.FromSeconds(2));
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

            stopwatch.Stop();
            string master = CrearMasterM3U8_Simple(videoPeticion, ListaDeM3U8Web);
            SubirArchivoGCS(master, videoPeticion.UID, $"{videoPeticion.NombreUnico}");
            Console.WriteLine("Tiempo transcurrido: " + stopwatch.Elapsed);

        }
        public void ConvertVideoToHLS(string UbicacionVideo, string UbicacionM3U8)
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

        public  TimeSpan ObtenerDuracion(string inputFilePath)
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

        public  List<VideoInterval> ObtenerIntervalos(string inputFilePath, TimeSpan intervalDuration)
        {
            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException("El archivo de video no existe.", inputFilePath);
            }

            List<VideoInterval> intervalos = new List<VideoInterval>();

            
            TimeSpan duracionTotal = ObtenerDuracion(inputFilePath);

            // Calcular los intervalos
            TimeSpan inicio = TimeSpan.Zero;

            while (inicio + intervalDuration <= duracionTotal)
            {
                intervalos.Add(new VideoInterval { Inicio = inicio, Duracion = intervalDuration });
                inicio += intervalDuration;
            }

            // Añadir el último intervalo si es necesario (con una duración diferente)
            if (inicio < duracionTotal)
            {
                TimeSpan duracionUltimoIntervalo = duracionTotal - inicio;
                intervalos.Add(new VideoInterval { Inicio = inicio, Duracion = duracionUltimoIntervalo });
            }

            return intervalos;
        }

        public  List<VideosCS> ObtenerVideoEnDiferenteResoluciones(VideoPeticion videoPeticion)
        {
            List<VideosCS> ListavideosCs = new List<VideosCS>();
            var Resoluciones = ObtenerResolucionVideo(videoPeticion.UbicacionVideoOriginal);

            Parallel.ForEach(Resoluciones,  resolucion =>
            {
                string Nombre = $"{resolucion}_{videoPeticion.NombreUnico}.{videoPeticion.Formato}";
                string _Ubicacion =  CambiarResolucion(videoPeticion.UbicacionVideoOriginal, Path.Combine(videoPeticion.Directorio, Nombre), resolucion, videoPeticion.Formato);
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

        public string CrearMasterM3U8_Simple(VideoPeticion videoPeticion, List<VideosCS> ListaDeM3U8Web)
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

                        writer.WriteLine($"#EXT-X-STREAM-INF:AVERAGE-BANDWIDTH={CalculateBandwidth(Ancho)},BANDWIDTH={CalculateBandwidth(Alto)},CODECS=\"avc1.640020\",RESOLUTION={Ancho}x{Alto},FRAME-RATE=60.000");
                        writer.WriteLine(videoM3U8.Ubicacion);
                    }
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
    }
}




