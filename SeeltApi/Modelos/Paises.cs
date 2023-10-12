using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;

namespace SeeltApi.Modelos
{
    public class Paises
    {
        public class Pais
        {
            public int ID { get; set; }
            public string NOMBRE { get; set; }
        }
        public  string ObtenerPaises()
        {
            string connectionString = General.CadenaConexion; // Reemplaza con tu cadena de conexión a SQL Server
            string storedProcedureName = "ObtenerPaises";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(storedProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        DataTable dataTable = new DataTable();
                        dataTable.Load(reader);

                        // Convierte el DataTable en una lista de objetos Pais
                        List<Pais> paises = new List<Pais>();
                        foreach (DataRow row in dataTable.Rows)
                        {
                            paises.Add(new Pais
                            {
                                ID = Convert.ToInt32(row["ID"]),
                                NOMBRE = row["NOMBRE"].ToString()
                            });
                        }

                        // Convierte la lista de objetos Pais en JSON
                        string json = JsonConvert.SerializeObject(paises, Formatting.Indented);

                        return json;
                    }
                }
            }
        }

    }
}
