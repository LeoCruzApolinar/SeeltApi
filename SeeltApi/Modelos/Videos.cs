using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Data;

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
            using(SqlConnection sqlConnection = new SqlConnection()) 
            {
                sqlConnection.Open();
                using (SqlCommand sqlCommand = new SqlCommand("",sqlConnection)) 
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;

                }
            }
            return null; // trabajar
        }

    }
}
