using Microsoft.AspNetCore.Mvc;
using SeeltApi.Modelos;
using static SeeltApi.Modelos.Usuarios;

namespace SeeltApi.Controllers
{
    [ApiController]
    public class CanalController : ControllerBase
    {
        [HttpPost("CrearEtiqueta")]
        public async Task<IActionResult> CrearEtiqueta(string NombreCanal, string NombreEtiqueta, string Color)
        {
            try
            {
                Canales canal = new Canales();
                bool Exitosa = canal.CrearEtiqueta(canal.ObtenerIdCanal(NombreCanal), NombreEtiqueta, Color);

                return Ok(Exitosa);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error en el servidor: " + ex.Message);
            }
        }
        [HttpPost("RegistrarCanal")]
        public async Task<IActionResult> RegistrarCanal()
        {
            Usuarios usuario = new Usuarios();
            var uid = Request.Form["UID"];
            var IDUsuario = usuario.ObtenerIdUsuario(uid);
            var Nombre = Request.Form["Nombre"];
            var Descripcion = Request.Form["Descripcion"];
            var FechaCreacion = DateTime.Now;
            var FotoPortada = Request.Form.Files["FotoPortada"];
            var FotoLogo = Request.Form.Files["FotoLogo"];

            try
            {
                Canales.RegCanal CanalRegistro = new Canales.RegCanal()
                {
                    IDUsuario = IDUsuario,
                    Nombre = Nombre,
                    Descripcion = Descripcion,
                    FechaCreacion = FechaCreacion,
                    FotoPortada = FotoPortada,
                    FotoLogo = FotoLogo
                };

                Canales canales = new Canales(); // Instancia de tu clase que contiene el método CrearUsuario.
                bool creado =  canales.IngresarCanal(CanalRegistro, uid);

                if (creado)
                {
                    return Ok(true);
                }
                else
                {
                    return BadRequest(false);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
        [HttpGet("ObtenerCanalPorIdUsuario")]
        public IActionResult ObtenerCanalPorIdUsuario(string UID)
        {
            Canales canal = new Canales();
            Usuarios usuarios = new Usuarios();
            int Id = usuarios.ObtenerIdUsuario(UID);
            string Json = canal.ObtenerCanalPorIdUsuario(Id);
            return Ok(Json);
        }
    }
}
