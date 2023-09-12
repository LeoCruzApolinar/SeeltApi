using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcesarVideosSeeltApi.Modelos;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ProcesarVideosSeeltApi.Controladores
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideosController : ControllerBase
    {
        [HttpPost("ProcesarSolicitudVideo")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> ProcesarSolicitudVideo(IFormFile archivoVideo)
        {
            try
            {
                VideosMD.VideoPeticion videoPeticion = new VideosMD.VideoPeticion();
                if (archivoVideo == null || archivoVideo.Length == 0)
                {
                    return BadRequest("Solicitud de video no válida");
                }

                // Generar un nombre de archivo único para el video.
                string nombreArchivo = Guid.NewGuid().ToString();
                videoPeticion.NombreUnico = nombreArchivo;
                videoPeticion.Directorio = GeneralesMD.CrearDirectorioTemporal().path;
                videoPeticion.UID = "MO0RNsEq7xUlGojbS3rKsuMJaQH3";
                videoPeticion.Formato = "mp4";
                // Ruta para guardar el video en el servidor (personaliza la ubicación según tus necesidades).
                string rutaArchivo = Path.Combine(videoPeticion.Directorio, nombreArchivo + "." + videoPeticion.Formato);

                videoPeticion.UbicacionVideoOriginal = rutaArchivo;
                using (var stream = new FileStream(rutaArchivo, FileMode.Create))
                {
                    await archivoVideo.CopyToAsync(stream);
                }
                VideosMD videosMD = new VideosMD();
                videosMD.ProcesarVideoGeneral(videoPeticion);
                // Realizar alguna acción con el video (por ejemplo, procesarlo con FFMpegCore).

                // Devolver una respuesta exitosa.
                return Ok($"Video recibido y guardado como {nombreArchivo}");
            }
            catch (Exception ex)
            {
                // Manejar errores aquí según tus necesidades.
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


    }
}
