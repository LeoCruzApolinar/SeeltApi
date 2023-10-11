using System.ComponentModel.DataAnnotations;

namespace SeeltApi.Modelos
{
    public class Posts
    {
        public class Post
        {
            public int ID { get; set; }

            public string TITULO { get; set; }

            public string CONTENIDO { get; set; }

            public string URL_MULTIMEDIA { get; set; }

            public DateTime FECHA_DE_SUBIDA { get; set; }
        }
    }
}
