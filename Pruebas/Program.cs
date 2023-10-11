namespace Pruebas
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            SeeltApi.Modelos.Canales usuarios = new SeeltApi.Modelos.Canales();
            int a = usuarios.ObtenerIdCanal("MO0RNsEq7xUlGojbS3rKsuMJaQH3a");
            Console.WriteLine("Hello, World!");
        }
    }
}