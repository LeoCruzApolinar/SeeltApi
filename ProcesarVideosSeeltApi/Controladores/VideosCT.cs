using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProcesarVideosSeeltApi.Modelos;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static ProcesarVideosSeeltApi.Modelos.IAMD;

namespace ProcesarVideosSeeltApi.Controladores
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideosController : ControllerBase
    {
        [HttpPost("ProcesarSolicitudVideoGeneral")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> ProcesarSolicitudVideoGeneral()
        {
            IAMD.ChatGPT chatGPT = new ChatGPT();
            Console.WriteLine("Waiting for response...");
            try
            {
                var ArchivoVideo = Request.Form.Files["ArchivoVideo"];
                var TituloVideo = Request.Form["TituloVideo"];
                var Descripcion = Request.Form["Descripcion"];
                var UID = Request.Form["UID"];
                var URL_Miniatura = Request.Form["URL_Miniatura"];
                Console.WriteLine("Peticion recibida");
                if (ArchivoVideo == null || ArchivoVideo.Length == 0)
                    return BadRequest("Solicitud de video no válida");

                var extension = Path.GetExtension(ArchivoVideo.FileName);
                var nombreArchivo = Guid.NewGuid().ToString();
                var directorio = GeneralesMD.CrearDirectorioTemporal().path;
                var rutaArchivo = Path.Combine(directorio, $"{nombreArchivo}{extension}");

                await using (var stream = new FileStream(rutaArchivo, FileMode.Create))
                {
                    await ArchivoVideo.CopyToAsync(stream);
                }

                var videoPeticion = new VideosMD.VideoPeticion
                {
                    NombreUnico = nombreArchivo,
                    Directorio = directorio,
                    Formato = extension.TrimStart('.'),
                    UbicacionVideoOriginal = rutaArchivo,
                    UID = UID,
                    TituloVideo = TituloVideo,
                    Descripcion = Descripcion,
                    URL_Miniatura = URL_Miniatura,
                };

                var videosMD = new VideosMD();
                videosMD.ProcesarVideoGeneral(videoPeticion);

                return Ok($"Video recibido y guardado como {nombreArchivo}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("ProcesarSolicitudVideoAvanzada")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> ProcesarSolicitudVideoAvanzada()
        {
            try
            {


                var ArchivoVideo = Request.Form.Files["ArchivoVideo"];

                var ListaSubtitulos = Request.Form["ListaSubtitulos"];
                var ListaAudios = Request.Form["ListaAudios"];

                var TituloVideo = Request.Form["TituloVideo"];
                var Descripcion = Request.Form["Descripcion"];
                var UID = Request.Form["UID"];
                var URL_Miniatura = Request.Form["URL_Miniatura"];

                VideosMD.VideoPeticion videoPeticionAvanzada = new VideosMD.VideoPeticion();
                videoPeticionAvanzada.UID = UID;
                videoPeticionAvanzada.Descripcion = Descripcion;
                videoPeticionAvanzada.TituloVideo = TituloVideo;
                videoPeticionAvanzada.URL_Miniatura = URL_Miniatura;
                videoPeticionAvanzada.Directorio = GeneralesMD.CrearDirectorioTemporal().path;

                var extension = Path.GetExtension(ArchivoVideo.FileName);
                var nombreArchivo = Guid.NewGuid().ToString();
                videoPeticionAvanzada.Formato = extension.TrimStart('.');

                var rutaArchivo = Path.Combine(videoPeticionAvanzada.Directorio, $"{nombreArchivo}{extension}");

                await using (var stream = new FileStream(rutaArchivo, FileMode.Create))
                {
                    await ArchivoVideo.CopyToAsync(stream);
                }

                videoPeticionAvanzada.UbicacionVideoOriginal = rutaArchivo;

                var ListaAudioFile = JsonConvert.DeserializeObject<List<VideosMD.AudioFile>>(ListaAudios);

                foreach (var item in ListaAudioFile)
                {
                    VideosMD.Audio audio = new VideosMD.Audio();
                    var extensionaudio = Path.GetExtension(item.Archivo.FileName);
                    var nombreArchivoaudio = Guid.NewGuid().ToString();
                    var rutaArchivoAudio = Path.Combine(videoPeticionAvanzada.Directorio, $"{nombreArchivoaudio}{extensionaudio}");

                    await using (var stream = new FileStream(rutaArchivoAudio, FileMode.Create))
                    {
                        await item.Archivo.CopyToAsync(stream);
                    }

                    audio.Ubicacion = rutaArchivoAudio;
                    audio.Idioma = item.Idioma;
                    videoPeticionAvanzada.ListaAudios.Add(audio);
                }

                var ListaSubFile = JsonConvert.DeserializeObject<List<VideosMD.SubtituloFile>>(ListaSubtitulos);

                foreach (var item in ListaSubFile)
                {
                    VideosMD.Subtitulo subtitulo = new VideosMD.Subtitulo();
                    var extensionSub = Path.GetExtension(item.Archivo.FileName);
                    var nombreArchivoSub = Guid.NewGuid().ToString();
                    var rutaArchivoAudio = Path.Combine(videoPeticionAvanzada.Directorio, $"{nombreArchivoSub}{extensionSub}");

                    await using (var stream = new FileStream(rutaArchivoAudio, FileMode.Create))
                    {
                        await item.Archivo.CopyToAsync(stream);
                    }

                    subtitulo.Ubicacion = rutaArchivoAudio;
                    subtitulo.Idioma = item.Idioma;
                    videoPeticionAvanzada.ListaSubtitulos.Add(subtitulo);
                }
                VideosMD videosMD = new VideosMD();
                videosMD.ProcesarVideoAvanzado(videoPeticionAvanzada);
                return Ok($"Video recibido y guardado como");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


    }
}
