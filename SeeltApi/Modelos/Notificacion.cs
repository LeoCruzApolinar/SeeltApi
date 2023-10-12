using LiteDB;
using System.Data.SqlClient;
using System.Data;

namespace SeeltApi.Modelos
{
    public class Notificacion
    {
        public void EnviarNotificacion(string userID, string contenido)
        {
            using (SqlConnection connection = new SqlConnection(General.CadenaConexion))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("EnviarNotificacion", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ID", userID);
                    command.Parameters.AddWithValue("@Contenido", contenido);

                    command.ExecuteNonQuery(); // Ejecuta la inserción
                }
            }
        }
    }
}
