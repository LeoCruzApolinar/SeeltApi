using Microsoft.AspNetCore.Mvc;
using SeeltApi.Modelos;
using static SeeltApi.Modelos.Usuarios;

namespace SeeltApi.Controllers
{
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        [HttpPost("RegistrarUsuario")]
        public async Task<IActionResult> RegistrarUsuario()
        {
            var ID_PAIS = Convert.ToInt32(Request.Form["ID_PAIS"]); // Asigna el valor del campo ID_PAIS
            var ID_IDIOMA = Convert.ToInt32(Request.Form["ID_IDIOMA"]); // Asigna el valor del campo ID_IDIOMA
            var ID_ROL = 1; // Asigna un valor fijo para ID_ROL
            var UID = Request.Form["UID"]; // Asigna el valor del campo UID
            var NOMBRES = Request.Form["NOMBRES"]; // Asigna el valor del campo NOMBRES
            var APELLIDOS = Request.Form["APELLIDOS"]; // Asigna el valor del campo APELLIDOS
            var DATE_OF_BIRTH = DateTime.Parse(Request.Form["DATE_OF_BIRTH"]); // Asigna el valor del campo DATE_OF_BIRTH como una fecha
            var FECHA_DE_INICIO = DateTime.Now; // Asigna el valor del campo FECHA_DE_INICIO como una fecha
            var USERNAME = Request.Form["USERNAME"]; // Asigna el valor del campo USERNAME
            var Foto = Request.Form.Files["FOTO"]; // Asigna la imagen a la propiedad FOTO

            try
            {
                UsuarioRegistro usuarioRegistro = new UsuarioRegistro()
                {
                    ID_PAIS = ID_PAIS,
                    ID_IDIOMA = ID_IDIOMA,
                    ID_ROL = ID_ROL,
                    UID = UID,
                    NOMBRES = NOMBRES,
                    APELLIDOS = APELLIDOS,
                    DATE_OF_BIRTH = DATE_OF_BIRTH,
                    FECHA_DE_INICIO = FECHA_DE_INICIO,
                    USERNAME = USERNAME,
                    FOTO = Foto
                };

                Usuarios usuarios = new Usuarios(); // Instancia de tu clase que contiene el método CrearUsuario.
                bool creado = await usuarios.CrearUsuario(usuarioRegistro);

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
        [HttpPost("LogUsuario")]
        public void InsertarSesion(string UID)
        {
            // Genera la fecha y hora actual
            General general = new General();
            DateTime loginDatetime = DateTime.Now;
            Usuarios usuarios = new Usuarios();
            string ipAddress = general.GetClientIpAddress(Request);
            int idUsuario = usuarios.ObtenerIdUsuario(UID);
            // Llama al método para insertar el registro de inicio de sesión
            usuarios.InsertarRegistroInicioSesion(idUsuario, loginDatetime, ipAddress);
        }
        [HttpGet("ObtenerDatosUsuario")]
        public IActionResult ObtenerDatosUsuario(string UID)
        {
            Usuarios usuarios = new Usuarios();
            int Id = usuarios.ObtenerIdUsuario(UID);
            string Json = usuarios.ObtenerUsuarioPorID(Id);
            return Ok(Json);
        }
        [HttpPost("Suscribir")] 
        public async Task<IActionResult> SuscribirUsuarioACanal(string NombreCanal, string UID)
        {
            try
            {
                Usuarios usuarios = new Usuarios();
                bool suscripcionExitosa = await usuarios.Suscribir(NombreCanal, UID);
                return Ok(suscripcionExitosa);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error en el servidor: " + ex.Message);
            }
        }
        [HttpPost("EliminarSuscripcion")]
        public async Task<IActionResult> EliminarSuscripcion(string NombreCanal, string UID)
        {
            try
            {
                Usuarios usuarios = new Usuarios();
                bool EliminarSuscripcionExitosa = await usuarios.EliminarSuscripcion(NombreCanal, UID);
                return Ok(EliminarSuscripcionExitosa);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error en el servidor: " + ex.Message);
            }
        }
        [HttpGet("ObtenerCanalesSuscritos")]
        public IActionResult ObtenerCanalesSuscritos(string UID)
        {
            Usuarios usuarios = new Usuarios();
            int Id = usuarios.ObtenerIdUsuario(UID);
            string Json = usuarios.ObtenerCanalesSuscritos(Id);
            return Ok(Json);
        }
        [HttpPost("RegistrarReaccion")]
        public async Task<IActionResult> RegistrarReaccion(int IdVideo, string UID, int TipoReaccion)
        {
            try
            {
                Usuarios usuarios = new Usuarios();
                
                bool Exitosa =  usuarios.RegistrarReaccion(usuarios.ObtenerIdUsuario(UID), IdVideo, TipoReaccion);

                return Ok(Exitosa);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error en el servidor: " + ex.Message);
            }
        }
    }
}
