using Microsoft.AspNetCore.Builder.Extensions;
using System.ComponentModel.DataAnnotations;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin.Auth;
using System.Data.SqlClient;
using System.Data;

namespace SeeltApi.Modelos
{
    public class Usuarios
    {
        public class UsuarioRegistro
        {
            public int ID { get; set; }

            public int ID_PAIS { get; set; }

            public int ID_IDIOMA { get; set; }

            public int ID_ROL { get; set; }

            public string UID { get; set; }

            public string NOMBRES { get; set; }

            public string APELLIDOS { get; set; }

            public DateTime DATE_OF_BIRTH { get; set; }

            public DateTime FECHA_DE_INICIO { get; set; }
        }

        private async Task<bool> VerificarExistenciaUsuario(string uidToCheck) 
        {
            try
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(@".\seelt-987cd-7dbe2c8a55dc.json")
                });
                FirebaseAuth auth = FirebaseAuth.DefaultInstance;
                UserRecord userRecord = await auth.GetUserAsync(uidToCheck);
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }

        public int ObtenerIdUsuario(string UID) 
        {
            try
            {
                int id = 0;
                using (SqlConnection sqlConnection = new SqlConnection("Data Source=LeonardoPC;Initial Catalog=SeeltBD;Integrated Security=True"))
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

        public async Task<int> CrearUsuario(UsuarioRegistro usuarioRegistro)
        {


            string uidToCheck = usuarioRegistro.UID;

            try
            {
                FirebaseAuth auth = FirebaseAuth.DefaultInstance;
                UserRecord userRecord = await auth.GetUserAsync(uidToCheck);
                Console.WriteLine("El UID existe en Firebase Authentication.");
                using (SqlConnection sqlConnection = new SqlConnection("Data Source=LeonardoPC;Initial Catalog=SeeltBD;Integrated Security=True"))
                {
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

                        sqlCommand.ExecuteNonQuery();
                        Console.WriteLine("Usuario insertado correctamente.");

                    }
                }
                return 0;
            }
            catch (FirebaseAuthException e)
            {
                if (e.AuthErrorCode == AuthErrorCode.UserNotFound)
                {
                    Console.WriteLine("El UID no existe en Firebase Authentication.");
                    return 1;
                }
                else
                {
                    Console.WriteLine($"Ocurrió un error al verificar el UID: {e.Message}");
                    return 2;
                }
            }


        }

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
                        using (SqlConnection sqlConnection = new SqlConnection())
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
                        using (SqlConnection sqlConnection = new SqlConnection())
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
    }
}
