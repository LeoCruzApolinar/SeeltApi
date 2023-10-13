using LiteDB;
using System.Data.SqlClient;
using System.Data;

namespace SeeltApi.Modelos
{
    public class Notificacion
    {
        //Crea notificacion a cada usuario
        private void CrearNotificacion(int userID, string contenido, string url)
        {
            using (SqlConnection connection = new SqlConnection(General.CadenaConexion))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("EnviarNotificacion", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ID", userID);
                    command.Parameters.AddWithValue("@Contenido", contenido);
                    command.Parameters.AddWithValue("@URLM", url);

                    command.ExecuteNonQuery(); // Ejecuta la inserción
                }
            }
        }
        //Envia la notificaciones a cada usuario
        public void EnviarNotificacion(int IDCanal, string Contenido, string Url) 
        {
            try
            {
                Canales canales = new Canales();
                List<int> listaID = canales.ObtenerUsuariosPorCanal(IDCanal);
                foreach (int idCanal in listaID)
                {
                    CrearNotificacion(idCanal, Contenido, Url);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
