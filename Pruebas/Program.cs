namespace Pruebas
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            SeeltApi.Modelos.Paises usuarios = new SeeltApi.Modelos.Paises();
            string a = usuarios.ObtenerPaises();
            Console.WriteLine("Hello, World!");
        }
    }
}