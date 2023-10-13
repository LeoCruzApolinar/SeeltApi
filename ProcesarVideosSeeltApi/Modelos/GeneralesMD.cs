using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace ProcesarVideosSeeltApi.Modelos
{
    public class GeneralesMD
    {

        public static (bool success, string path) CrearDirectorioTemporal()
        {
            try
            {
                string tempDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDirectoryPath);
                return (true, tempDirectoryPath);
            }
            catch (Exception)
            {
                return (false, "");
            }
        }

        public static bool EliminarDirectorio(string tempDirectory)
        {
            try
            {
                Directory.Delete(tempDirectory, true);
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }

        public static List<string> ObtenerLasCategorias()
        {
            List<string> categorias = new List<string>();

            string connectionString = "Data Source=LeonardoPC;Initial Catalog=SeeltBD;Integrated Security=True"; // Reemplaza con tu cadena de conexión a la base de datos SQL Server
            string query = "SELECT NOMBRE FROM CATEGORIAS";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string categoria = reader["NOMBRE"].ToString();
                            categorias.Add(categoria);
                        }
                    }
                }
            }

            return categorias;
        }


        public static async Task<string> GuardarArchivoVideo(string ubicacion, IFormFile archivoVideo)
        {
            try
            {
                if (archivoVideo != null && archivoVideo.Length > 0)
                {
                    var rutaArchivo = Path.Combine(ubicacion, archivoVideo.FileName);

                    using (var stream = new FileStream(rutaArchivo, FileMode.Create))
                    {
                        await archivoVideo.CopyToAsync(stream);
                    }

                    return rutaArchivo;
                }
                else
                {
                    throw new ArgumentException("No se proporcionó un archivo de video.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar el archivo de video: {ex.Message}");
            }
        }

        public static string ObtenerContenidoEntreCorchetes(string nombreArchivo)
        {
            // Utiliza una expresión regular para buscar contenido entre corchetes [..]
            string patron = @"\[(.*?)\]";
            Match match = Regex.Match(nombreArchivo, patron);

            if (match.Success)
            {
                // El contenido entre corchetes se encuentra en match.Groups[1].Value
                return match.Groups[1].Value;
            }

            // Si no se encuentra contenido entre corchetes, devolvemos una cadena vacía
            return string.Empty;
        }

        //Guardar en base de datos
    }
}
