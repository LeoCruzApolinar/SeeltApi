using System.Data.SqlClient;

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

        //Guardar en base de datos
    }
}
