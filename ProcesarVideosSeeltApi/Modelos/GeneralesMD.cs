namespace ProcesarVideosSeeltApi.Modelos
{
    public class GeneralesMD
    {
        public class Solicitud
        {
            public int ID_Canal { get; set; }
            public string NombreVideo { get; set; }
            public string FormatoVideo { get; set; }
            public string URL_Video { get; set; }
            public int TipoDeProcesado { get; set; }
            public string URL_Miniatura { get; set; }
            public (int ID_Idioma, string URL_AUDIO)[] Audios { get; set; }
            public (int ID_Idioma, string URL_Sub)[] Subtitulos { get; set; }
        }
        public static (bool success, string path) CrearDirectorioTemporal()
        {
            try
            {
                string tempDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDirectoryPath);
                return (true, tempDirectoryPath);
            }
            catch (Exception)
            {
                return (false, "");
            }
        }

        public static bool EliminarDirectorio(string tempDirectory)
        {
            try
            {
                Directory.Delete(tempDirectory, true);
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }

    }
}
