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
using static ProcesarVideosSeeltApi.Modelos.VideosMD;

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
                /*
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
                 */

                var ArchivoVideo = Request.Form.Files["ArchivoVideo"];
                var ListaSubtitulos = Request.Form.Files.GetFiles("ListaSubtitulos[]");
                var ListaAudios = Request.Form.Files.GetFiles("ListaAudios[]");
                var TituloVideo = Request.Form["TituloVideo"];
                var Descripcion = Request.Form["Descripcion"];
                var UID = Request.Form["UID"];
                var URL_Miniatura = Request.Form["URL_Miniatura"];
                string Directorio = GeneralesMD.CrearDirectorioTemporal().path;

                VideosMD.VideoPeticion videoPeticion = new VideosMD.VideoPeticion()
                {
                    NombreUnico = Guid.NewGuid().ToString(),
                    Directorio = Directorio,
                    TituloVideo = TituloVideo,
                    Descripcion = Descripcion,
                    UID = UID,
                    URL_Miniatura = URL_Miniatura,
                    UbicacionVideoOriginal = await GeneralesMD.GuardarArchivoVideo(Directorio, ArchivoVideo),
                    Formato = Path.GetExtension(ArchivoVideo.FileName),
                };

                List<VideosMD.Subtitulo> Lsubtitulos = new List<VideosMD.Subtitulo>();
                foreach (var item in ListaSubtitulos)
                {
                    VideosMD.Subtitulo subtitulo = new VideosMD.Subtitulo()
                    {
                        Ubicacion = await GeneralesMD.GuardarArchivoVideo(Directorio, item),
                        Idioma = GeneralesMD.ObtenerContenidoEntreCorchetes(item.FileName)
                    };
                    Lsubtitulos.Add(subtitulo);
                }
                List<VideosMD.Audio> Laudios = new List<VideosMD.Audio>();
                foreach (var item in ListaAudios)
                {
                    VideosMD.Audio audio = new VideosMD.Audio()
                    {
                        Ubicacion = await GeneralesMD.GuardarArchivoVideo(Directorio, item),
                        Idioma = GeneralesMD.ObtenerContenidoEntreCorchetes(item.FileName),
                    };
                    Laudios.Add(audio);
                }
                videoPeticion.ListaSubtitulos = Lsubtitulos;
                videoPeticion.ListaAudios = Laudios;
                VideosMD videosMD = new VideosMD();
                videosMD.ProcesarVideoAvanzado(videoPeticion);
                return Ok($"Video recibido y guardado como");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


    }
}
