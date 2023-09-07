using FFMpegCore.Enums;
using Microsoft.AspNetCore.Mvc;
using ProcesarVideosSeeltApi.Modelos;
using System.Diagnostics;
using System.IO;

namespace ProcesarVideosSeeltApi.Controladores
{
    public class VideosCT : Controller
    {
        [HttpPost("ProcesarSolicitudVideo")]
        public   bool ProcesarSolicitudVideo(/*[FromBody] GeneralesMD.Solicitud solicitud*/)
        {
            try
            {
                //Si la solicitud llega como 0 significa que es tipo Libre(Creador de contenido), 1 Pesonalizada (en esta el json contiene diferentes audio y subtitulos)
                switch (0)
                {
                    case 0:

                        // Crear una instancia de Stopwatch
                        Stopwatch stopwatch = new Stopwatch();

                        // Iniciar el cronómetro
                        stopwatch.Start();

                        VideosMD.ProcesarVideoGeneral("Vídeo.mp4", "MO0RNsEq7xUlGojbS3rKsuMJaQH3");

                        // Detener el cronómetro
                        stopwatch.Stop();

                        // Mostrar el tiempo transcurrido
                        Console.WriteLine("Tiempo transcurrido: " + stopwatch.Elapsed);
                        break;
                  
                }
            }
            catch (Exception)
            {

                throw;
            }
            return true;
        }
    }
}
