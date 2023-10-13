using Microsoft.AspNetCore.Builder.Extensions;
using System.ComponentModel.DataAnnotations;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin.Auth;
using System.Data.SqlClient;
using System.Data;
using LiteDB;
using Newtonsoft.Json;
using static SeeltApi.Modelos.Canales;

namespace SeeltApi.Modelos
{
    public class Usuarios
    {
        public class Usuario
        {
            public string Idioma { get; set; }
            public string Pais { get; set; }
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public string UserName { get; set; }
            public DateTime FechaNacimiento { get; set; }
            public DateTime FechaDeInicio { get; set; }
            public string UrlFoto { get; set; }
        }
        public class UsuarioRegistro
        {

            public int ID_PAIS { get; set; }

            public int ID_IDIOMA { get; set; }

            public int ID_ROL { get; set; }

            public string UID { get; set; }

            public string NOMBRES { get; set; }

            public string APELLIDOS { get; set; }

            public DateTime DATE_OF_BIRTH { get; set; }

            public DateTime FECHA_DE_INICIO { get; set; }

            public string USERNAME { get; set; }

            public IFormFile? FOTO { get; set; }
        }

        public class CanalSuscritos
        {
            public int CanalID { get; set; }
            public string NombreCanal { get; set; }
        }

        //Verifica con firebase la existencia de usuario
        private async Task<bool> VerificarExistenciaUsuario(string uidToCheck)
        {
            try
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(@".\seelt-987cd-7dbe2c8a55dc.json")
                });
                FirebaseAuth auth = FirebaseAuth.GetAuth(FirebaseApp.DefaultInstance);
                UserRecord userRecord = await auth.GetUserAsync(uidToCheck);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        //Con el UID obtiene la id del usuario
        public int ObtenerIdUsuario(string UID) 
        {
            try
            {
                int id = 0;
                using (SqlConnection sqlConnection = new SqlConnection(General.CadenaConexion))
                {
                    sqlConnection.Open();
                    using(SqlCommand  cmd = new SqlCommand("ObtenerIdUsuarioConUID", sqlConnection)) 
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@UID", SqlDbType.NVarChar, 255).Value = UID;

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
        //Crea los usuario
        public async Task<bool> CrearUsuario(UsuarioRegistro usuarioRegistro)
        {
            if (await VerificarExistenciaUsuario(usuarioRegistro.UID))
            {
                using (SqlConnection sqlConnection = new SqlConnection(General.CadenaConexion))
                {
                    sqlConnection.Open ();
                    General general = new General();
                    string urlFoto = general.SubirArchivoGCS(usuarioRegistro.FOTO, usuarioRegistro.UID, "UserData");
                    using (SqlCommand sqlCommand = new SqlCommand("InsertarUsuario", sqlConnection))
                    {
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.Parameters.Add(new SqlParameter("@ID_PAIS", usuarioRegistro.ID_PAIS));
                        sqlCommand.Parameters.Add(new SqlParameter("@ID_IDIOMA", usuarioRegistro.ID_IDIOMA));
                        sqlCommand.Parameters.Add(new SqlParameter("@ID_ROL", usuarioRegistro.ID_ROL));
                        sqlCommand.Parameters.Add(new SqlParameter("@UID", usuarioRegistro.UID));
                        sqlCommand.Parameters.Add(new SqlParameter("@NOMBRES", usuarioRegistro.NOMBRES));
                        sqlCommand.Parameters.Add(new SqlParameter("@APELLIDOS", usuarioRegistro.APELLIDOS));
                        sqlCommand.Parameters.Add(new SqlParameter("@DATE_OF_BIRTH", usuarioRegistro.DATE_OF_BIRTH));
                        sqlCommand.Parameters.Add(new SqlParameter("@FECHA_DE_INICIO", usuarioRegistro.FECHA_DE_INICIO));
                        sqlCommand.Parameters.Add(new SqlParameter("@USERNAME", usuarioRegistro.USERNAME));
                        sqlCommand.Parameters.Add(new SqlParameter("@FOTO_URL", urlFoto));

                        sqlCommand.ExecuteNonQuery();
                        Console.WriteLine("Usuario insertado correctamente.");

                    }
                    sqlConnection.Close();
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        //Con el nombre del canal consigue la id y con uid consigue la id del usuario luego realiza la relacion en la tabla sucripcion
        public async Task<bool> Suscribir(string NombreCanal, string UID) 
        {
            try
            {
                if (await VerificarExistenciaUsuario(UID))
                {
                    Canales canales = new Canales();
                    Usuarios usuarios = new Usuarios();
                    int IdCanal = canales.ObtenerIdCanal(NombreCanal);
                    int IdUsuario = canales.ObtenerIdCanal(UID);
                    if (IdCanal != 0 && IdUsuario != 0)
                    {
                        using (SqlConnection sqlConnection = new SqlConnection(General.CadenaConexion))
                        {
                            sqlConnection.Open();
                            using (SqlCommand sqlCommand = new SqlCommand("RealizarSuscripcion", sqlConnection))
                            {
                                sqlCommand.CommandType = CommandType.StoredProcedure;
                                sqlCommand.Parameters.Add(new SqlParameter("@ID_CANAL", IdCanal));
                                sqlCommand.Parameters.Add(new SqlParameter("@ID_USUARIO", IdUsuario));
                                sqlCommand.ExecuteNonQuery();
                            }
                            sqlConnection.Close();
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {

                return false;
            }
        }
        //Con el nombre del canal consigue la id y con uid consigue la id del usuario luego elimina la relacion en la tabla sucripcion
        public async Task<bool> EliminarSuscripcion(string NombreCanal, string UID)
        {
            try
            {
                if (await VerificarExistenciaUsuario(UID))
                {
                    Canales canales = new Canales();
                    Usuarios usuarios = new Usuarios();
                    int IdCanal = canales.ObtenerIdCanal(NombreCanal);
                    int IdUsuario = canales.ObtenerIdCanal(UID);
                    if (IdCanal != 0 && IdUsuario != 0)
                    {
                        using (SqlConnection sqlConnection = new SqlConnection(General.CadenaConexion))
                        {
                            sqlConnection.Open();
                            using (SqlCommand sqlCommand = new SqlCommand("EliminarSuscripcion", sqlConnection))
                            {
                                sqlCommand.CommandType = CommandType.StoredProcedure;
                                sqlCommand.Parameters.Add(new SqlParameter("@ID_CANAL", IdCanal));
                                sqlCommand.Parameters.Add(new SqlParameter("@ID_USUARIO", IdUsuario));
                                sqlCommand.ExecuteNonQuery();
                            }
                            sqlConnection.Close();
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {

                return false;
            }
        }
        //Verifica si el nombre de usuario esta en uso
        public bool VerificarUserName(string username)
        {
            bool a = false;
            using (SqlConnection connection = new SqlConnection(General.CadenaConexion))
            {
                connection.Open();

                string query = "SELECT 1 FROM [USUARIOS] WHERE [USERNAME] = @Username";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        a =  reader.HasRows; // Devuelve true si el nombre de usuario existe, false si no existe.
                    }
                }
                connection.Close();

            }
            return a;
        }
        //Guardar log Usuario
        public void InsertarRegistroInicioSesion(int idUsuario, DateTime loginDatetime, string ipAddress)
        {
            using (SqlConnection connection = new SqlConnection(General.CadenaConexion))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("InsertRegistroInicioSesion", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Parámetros del stored procedure
                    command.Parameters.AddWithValue("@ID_USUARIO", idUsuario);
                    command.Parameters.AddWithValue("@LOGIN_DATETIME", loginDatetime);
                    command.Parameters.AddWithValue("@IP_ADDRESS", ipAddress);

                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }
        //Obtener usuario
        public string ObtenerUsuarioPorID(int userID)
        {
            string connectionString = General.CadenaConexion; // Reemplaza con tu cadena de conexión a la base de datos
            string jsonResult = string.Empty;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("ObtenerUsuarioPorID", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ID", userID);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Usuario usuario = new Usuario
                            {
                                Idioma = reader["Idioma"].ToString(),
                                Pais = reader["Pais"].ToString(),
                                Nombre = reader["Nombre"].ToString(),
                                Apellido = reader["Apellido"].ToString(),
                                FechaNacimiento = Convert.ToDateTime(reader["FechaNacimiento"]),
                                FechaDeInicio = Convert.ToDateTime(reader["FechaDeInicio"]),
                                UrlFoto = reader["UrlFoto"].ToString(),
                                UserName = reader["USERNAME"].ToString()
                            };

                            jsonResult = JsonConvert.SerializeObject(usuario);
                        }
                    }
                }
                connection.Close();
            }

            return jsonResult;
        }
        //Obtener canales suscritos
        public string ObtenerCanalesSuscritos(int userID)
        {
            List<CanalSuscritos> canalesSuscritos = new List<CanalSuscritos>();

            using (SqlConnection connection = new SqlConnection(General.CadenaConexion))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("ObtenerCanalesSuscritos", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ID", userID);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CanalSuscritos canal = new CanalSuscritos
                            {
                                CanalID = Convert.ToInt32(reader["CanalID"]),
                                NombreCanal = reader["NombreCanal"].ToString()
                            };
                            canalesSuscritos.Add(canal);
                        }
                    }
                }
            }

            // Serializar la lista de canales como JSON
            string canalesJson = JsonConvert.SerializeObject(canalesSuscritos);

            return canalesJson;
        }
        //Registrar raccion
        public bool RegistrarReaccion(int usuarioID, int videoID, int tipoReaccionID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(General.CadenaConexion))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("RegistrarReaccion", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@UsuarioID", usuarioID);
                        command.Parameters.AddWithValue("@IDVideo", videoID);
                        command.Parameters.AddWithValue("@IDTipoReaccion", tipoReaccionID);

                        command.ExecuteNonQuery(); // Ejecutar el procedimiento almacenado
                    }
                }

                return true; // Registro exitoso
            }
            catch (Exception)
            {
                return false; // Error en el registro
            }
        }
    }
}
