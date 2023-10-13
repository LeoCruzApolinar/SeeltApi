using LiteDB;
using Microsoft.AspNetCore.Mvc;
using SeeltApi.Modelos;

namespace SeeltApi.Controllers
{
    [Route("api/General")]
    [ApiController]
    public class GeneralController : ControllerBase
    {
        [HttpGet("ObtenerPaises")]
        public IActionResult ObtenerPaises()
        {
            Paises paises = new Paises();
            string paisesJson = paises.ObtenerPaises();
            return Ok(paisesJson);
        }
        [HttpGet("ObtenerIdiomas")]
        public IActionResult ObtenerIdiomas()
        {

            Idiomas idiomas = new Idiomas();
            string idiomasJson = idiomas.ObtenerIdiomas();
            return Ok(idiomasJson);
        }
        [HttpGet("VerificarUserName")]
        public IActionResult VerificarUserName(string username)
        {
            Usuarios usuarios = new Usuarios();
            bool respuesta = usuarios.VerificarUserName(username);
            return Ok(respuesta);
        }
        [HttpGet("GetTipoDeVideoAsJSON")]
        public IActionResult GetTipoDeVideoAsJSON()
        {
            Videos videos = new Videos();
            return Ok(videos.GetTipoDeVideoAsJSON());
        }
        [HttpPost]
        [Route("InsertarEtiqueta")]
        public void InsertarEtiqueta([FromBody] General.EtiquetaData data)
        {
            // Llama al método InsertarEtiqueta para insertar la etiqueta en la base de datos
            General etiquetaManager = new General();
            Usuarios usuarios = new Usuarios();
            etiquetaManager.InsertarEtiqueta(usuarios.ObtenerIdUsuario(data.idCanal), data.nombre, data.color);
        }


    }
}
