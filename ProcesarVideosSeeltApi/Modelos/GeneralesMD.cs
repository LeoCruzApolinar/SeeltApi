using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using static MediaToolkit.Model.Metadata;

namespace ProcesarVideosSeeltApi.Modelos
{
    public class GeneralesMD
    {
        public class VideoUp
        {
            public int TipoDeVideoId { get; set; }
            public int CanalId { get; set; }
            public int VisibilidadId { get; set; }
            public string UrlVideo { get; set; }
            public string Titulo { get; set; }
            public string Descripcion { get; set; }
            public TimeSpan Duracion { get; set; }
            public DateTime FechaSubida { get; set; }
            public string UrlMiniatura { get; set; }
        }


        public static string CadenaConexion = "Data Source=baseseelt.database.windows.net;Initial Catalog=SeeltBD;Persist Security Info=True;User ID=leonardo;Password=Seelt1234";
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

            string connectionString =CadenaConexion; // Reemplaza con tu cadena de conexión a la base de datos SQL Server
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

        public int InsertarVideo(VideoUp video)
        {
            int nuevaID = 0;

            using (SqlConnection connection = new SqlConnection(CadenaConexion))
            {
                using (SqlCommand cmd = new SqlCommand("InsertarVideo", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@ID_TIPO_DE_VIDEO", SqlDbType.Int)).Value = video.TipoDeVideoId;
                    cmd.Parameters.Add(new SqlParameter("@ID_CANAL", SqlDbType.Int)).Value = video.CanalId;
                    cmd.Parameters.Add(new SqlParameter("@ID_VISIBILIDAD", SqlDbType.Int)).Value = video.VisibilidadId;
                    cmd.Parameters.Add(new SqlParameter("@URL_VIDEO", SqlDbType.NVarChar, 500)).Value = video.UrlVideo;
                    cmd.Parameters.Add(new SqlParameter("@TITULO", SqlDbType.VarChar, 100)).Value = video.Titulo;
                    cmd.Parameters.Add(new SqlParameter("@DESCRIPCION", SqlDbType.NVarChar, 1500)).Value = video.Descripcion;
                    cmd.Parameters.Add(new SqlParameter("@DURACION", SqlDbType.Time)).Value = video.Duracion;
                    cmd.Parameters.Add(new SqlParameter("@FECHA_DE_SUBIDA", SqlDbType.DateTime)).Value = video.FechaSubida;
                    cmd.Parameters.Add(new SqlParameter("@URL_MINIATURA", SqlDbType.NVarChar, 500)).Value = video.UrlMiniatura;

                    // Parámetro de salida para la nueva ID
                    var newIdParam = new SqlParameter("@NuevaID", SqlDbType.Int);
                    newIdParam.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(newIdParam);

                    connection.Open();
                    cmd.ExecuteNonQuery();

                    // Recupera la nueva ID generada
                    nuevaID = (int)cmd.Parameters["@NuevaID"].Value;
                }
            }

            return nuevaID;
        }

        public int ObtenerIdCategoriaPorNombre(string nombreCategoria)
        {
            int idCategoria = -1; // Valor predeterminado en caso de que no se encuentre la categoría

            using (SqlConnection connection = new SqlConnection(CadenaConexion))
            {
                connection.Open();

                using (SqlCommand cmd = new SqlCommand("SELECT [ID_CATEGORIA] FROM [VIDEOS_CATEGORIAS] WHERE [NOMBRE] = @Nombre", connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@Nombre", SqlDbType.NVarChar, 100)).Value = nombreCategoria;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idCategoria = reader.GetInt32(0);
                        }
                    }
                }
            }

            return idCategoria;
        }

        public void InsertarVideoCategoria(int idVideo, int idCategoria)
        {
            using (SqlConnection connection = new SqlConnection(CadenaConexion))
            {
                using (SqlCommand cmd = new SqlCommand("InsertarVideoCategoria", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@ID_VIDEO", SqlDbType.Int)).Value = idVideo;
                    cmd.Parameters.Add(new SqlParameter("@ID_CATEGORIA", SqlDbType.Int)).Value = idCategoria;

                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public int ObtenerIdCanal(string Nombre)
        {
            try
            {
                int id = 0;
                using (SqlConnection sqlConnection = new SqlConnection(CadenaConexion))
                {
                    sqlConnection.Open();
                    using (SqlCommand cmd = new SqlCommand("ObtenerIdCanalConNombre", sqlConnection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@NombreCanal", SqlDbType.NVarChar, 255).Value = Nombre;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                id = reader.GetInt32(0); // Lee el valor de la primera columna (ID)
                            }
                        }
                    }
                    sqlConnection.Close();
                }
                return id;
            }
            catch (Exception)
            {

                return 0;
            }
        }

    }
}
