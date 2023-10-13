using System.ComponentModel.DataAnnotations;
using static SeeltApi.Modelos.Videos;
using System.Data.SqlClient;
using System.Data;
using LiteDB;
using Newtonsoft.Json;

namespace SeeltApi.Modelos
{
    public class Canales
    {
        public class Canal
        {
            public bool CANAL_EXISTE { get; set; }

            public string NOMBRE { get; set; }

            public string DESCRIPCION { get; set; }

            public DateTime FECHA_CREACION { get; set; }

            public string FOTO_PORTADA { get; set; }

            public string FOTO_LOGO { get; set; }
        }

        public class RegCanal
        {
            public int IDUsuario { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public DateTime FechaCreacion { get; set; }
            public IFormFile FotoPortada { get; set; }
            public IFormFile FotoLogo { get; set; }
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
        //Obtine los Usuario de un canalEliminarSuscripcion
        public List<int> ObtenerUsuariosPorCanal(int idCanal)
        {
            List<int> usuarios = new List<int>();

            using (SqlConnection connection = new SqlConnection(General.CadenaConexion))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("ObtenerUsuariosPorCanal", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@IdCanal", idCanal);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int userID = Convert.ToInt32(reader["ID_USUARIO"]);
                            usuarios.Add(userID);
                        }
                    }
                }
            }

            return usuarios;
        }
        //Crea las etiquetas
        public bool CrearEtiqueta(int canalID, string nombreEtiqueta, string color)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(General.CadenaConexion))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("CrearEtiqueta", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@IDCanal", canalID);
                        command.Parameters.AddWithValue("@NombreEtiqueta", nombreEtiqueta);
                        command.Parameters.AddWithValue("@COLOR", color);

                        command.ExecuteNonQuery(); // Ejecutar el procedimiento almacenado
                    }
                }

                return true; // Creación exitosa
            }
            catch (Exception)
            {
                return false; // Error en la creación
            }
        }
        //Crea un canal
        public bool IngresarCanal(RegCanal canal, string UID)
        {
            try
            {
                General general = new General();
                using (SqlConnection connection = new SqlConnection(General.CadenaConexion))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("IngresarCanal", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@IDUsuario", canal.IDUsuario);
                        command.Parameters.AddWithValue("@Nombre", canal.Nombre);
                        command.Parameters.AddWithValue("@Descripcion", canal.Descripcion);
                        command.Parameters.AddWithValue("@FechaCreacion", canal.FechaCreacion);
                        command.Parameters.AddWithValue("@FotoPortadaUrl", general.SubirArchivoGCS(canal.FotoPortada, UID, "CanalUsuario"));
                        command.Parameters.AddWithValue("@FotoLogoUrl", general.SubirArchivoGCS(canal.FotoPortada, UID, "CanalUsuario"));

                        command.ExecuteNonQuery(); // Ejecuta el procedimiento almacenado
                    }
                }

                return true; // Inserción exitosa
            }
            catch (Exception)
            {
                return false; // Error en la inserción
            }
        }
        //Obtener canl
        public string ObtenerCanalPorIdUsuario(int idUsuario)
        {
            string connectionString = General.CadenaConexion; // Reemplaza con tu cadena de conexión a la base de datos
            string procedureName = "ObtenerCanalPorUsuario";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(procedureName, connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@IdUsuario", idUsuario));

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            var canal = new Canal();

                            while (reader.Read())
                            {
                                canal.CANAL_EXISTE = true;
                                canal.NOMBRE = reader["NOMBRE"].ToString();
                                canal.DESCRIPCION = reader["DESCRIPCION"].ToString();
                                canal.FECHA_CREACION = (DateTime)reader["FECHA_CREACION"];
                                canal.FOTO_PORTADA = reader["FOTO_PORTADA_URL"].ToString();
                                canal.FOTO_LOGO = reader["FOTO_LOGO_URL"].ToString();
                            }

                            // Convertir el objeto Canal a formato JSON
                            string jsonResult = JsonConvert.SerializeObject(canal);
                            return jsonResult;
                        }
                        else
                        {
                            return "{\"CANAL_EXISTE\": false}"; // Canal no encontrado
                        }
                    }
                }
            }
        }

    }
}

