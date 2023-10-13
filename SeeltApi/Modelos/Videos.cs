using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Data;
using System.Text;

namespace SeeltApi.Modelos
{
    public class Videos
    {
        public class Video
        {
            public int ID { get; set; }

            public int ID_TIPO_DE_VIDEO { get; set; }

            public int ID_CANAL { get; set; }

            public int ID_VISIBILIDAD { get; set; }

            public string URL_VIDEO { get; set; }

            public string TITULO { get; set; }

            public string DESCRIPCION { get; set; }

            public TimeSpan DURACION { get; set; }

            public DateTime FECHA_DE_SUBIDA { get; set; }

            public string URL_MINIATURA { get; set; }
        }

        public List<Video> ObtenerVideosCanal(string Nombre)
        {
            using (SqlConnection sqlConnection = new SqlConnection())
            {
                sqlConnection.Open();
                using (SqlCommand sqlCommand = new SqlCommand("", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;

                }
            }
            return null; // trabajar
        }

        public string GetTipoDeVideoAsJSON()
        {
            string connectionString = General.CadenaConexion;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand cmd = new SqlCommand("GetTipoDeVideoAsJSON", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            StringBuilder jsonResult = new StringBuilder();
                            while (reader.Read())
                            {
                                jsonResult.Append(reader[0].ToString()); // Columna NOMBRE
                            }
                            return jsonResult.ToString();
                        }
                        else
                        {
                            return "No se encontraron resultados.";
                        }
                    }
                }
            }
        }

    }
}
