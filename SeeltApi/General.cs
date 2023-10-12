using Google.Cloud.Storage.V1;

namespace SeeltApi
{
    public class General
    {
        public static string CloudCredenciales = @".\seelt-987cd-7dbe2c8a55dc.json";

        private static string NombreDelBucket = @"seelt-987cd.appspot.com";

        public static string CadenaConexion = "Data Source=baseseelt.database.windows.net;Initial Catalog=SeeltBD;Persist Security Info=True;User ID=leonardo;Password=Seelt1234";

        public string SubirArchivoGCS(IFormFile archivo, string UID, string CarpetaDeAlojamiento)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", CloudCredenciales);
            var storageClient = StorageClient.Create();
            string NombreDelObjeto = Path.GetFileName(archivo.FileName);
            try
            {
                using (Stream stream = archivo.OpenReadStream())
                {
                    var objectPredefinedAcl = PredefinedObjectAcl.PublicRead;
                    var Configuracion = new UploadObjectOptions
                    {
                        PredefinedAcl = objectPredefinedAcl,
                    };
                    NombreDelObjeto = $"{UID}/{CarpetaDeAlojamiento}/{NombreDelObjeto}";

                    // Subir el objeto a GCS
                    storageClient.UploadObject(NombreDelBucket, NombreDelObjeto, null, stream, Configuracion);
                }
                // Construir y retornar la URL pública del objeto
                string urlPublica = $"https://storage.googleapis.com/{NombreDelBucket}/{NombreDelObjeto}";
                return urlPublica;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }
        public string GetClientIpAddress(HttpRequest request)
        {
            // Intenta obtener la dirección IP desde las cabeceras de la solicitud
            string ipAddress = request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = request.HttpContext.Connection.RemoteIpAddress.ToString();
            }

            return ipAddress;
        }


    }
}
