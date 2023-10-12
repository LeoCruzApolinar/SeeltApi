using System.ComponentModel.DataAnnotations;
using static SeeltApi.Modelos.Videos;
using System.Data.SqlClient;
using System.Data;

namespace SeeltApi.Modelos
{
    public class Canales
    {
        public class Canal
        {
            public string NOMBRE { get; set; }

            public string DESCRIPCION { get; set; }

            public DateTime FECHA_CREACION { get; set; }

            public string FOTO_PORTADA { get; set; }
        }
        //Obtiene la id del canal por el nombre el cual es unico
        public int ObtenerIdCanal(string Nombre)
        {
            try
            {
                int id = 0;
                using (SqlConnection sqlConnection = new SqlConnection(General.CadenaConexion))
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
