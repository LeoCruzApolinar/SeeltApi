using FFmpeg.AutoGen;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace ProcesarVideosSeeltApi.Modelos
{
    public class IAMD
    {
        public static class Transcriber
        {
            public async static Task<(string, string)> DetectarIdiomaYTranscribir(string pathToAudioFile)
            {
                try
                {


                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", VideosMD.CloudCredenciales);
                    var speech = SpeechClient.Create();

                    var recognitionConfigEn = new RecognitionConfig
                    {
                        Encoding = RecognitionConfig.Types.AudioEncoding.EncodingUnspecified,
                        SampleRateHertz = 44100,
                        LanguageCode = "en-US",
                    };

                    var recognitionConfigEs = new RecognitionConfig
                    {
                        Encoding = RecognitionConfig.Types.AudioEncoding.EncodingUnspecified,
                        SampleRateHertz = 44100,
                        LanguageCode = "es-ES",
                    };

                    var recognizeEnTask = speech.RecognizeAsync(recognitionConfigEn, RecognitionAudio.FromFile(pathToAudioFile));
                    var recognizeEsTask = speech.RecognizeAsync(recognitionConfigEs, RecognitionAudio.FromFile(pathToAudioFile));

                    // Esperar a que ambas tareas se completen
                    await Task.WhenAll(recognizeEnTask, recognizeEsTask);

                    var response_en = recognizeEnTask.Result;
                    var response_es = recognizeEsTask.Result;

                    // Variables para almacenar el idioma y el texto transcrito
                    string idiomaDetectado = "";
                    string textoTranscrito = "";

                    // Verificar si hay resultados en inglés y español
                    if (response_en.Results.Count > 0 && response_es.Results.Count > 0)
                    {
                        float confidence_en = response_en.Results[0].Alternatives[0].Confidence;
                        float confidence_es = response_es.Results[0].Alternatives[0].Confidence;

                        // Determinar el idioma con mayor confianza
                        if (confidence_en > confidence_es && response_en.Results[0].Alternatives[0].Transcript.Length > response_es.Results[0].Alternatives[0].Transcript.Length)
                        {
                            idiomaDetectado = "Ingles";
                            textoTranscrito = response_en.Results[0].Alternatives[0].Transcript;
                        }
                        else
                        {
                            idiomaDetectado = "Español";
                            textoTranscrito = response_es.Results[0].Alternatives[0].Transcript;
                        }
                    }
                    else if (response_en.Results.Count > 0)
                    {
                        idiomaDetectado = "Ingles";
                        textoTranscrito = response_en.Results[0].Alternatives[0].Transcript;
                    }
                    else if (response_es.Results.Count > 0)
                    {
                        idiomaDetectado = "Español";
                        textoTranscrito = response_es.Results[0].Alternatives[0].Transcript;
                    }

                    // Retornar el idioma y el texto transcrito como una tupla
                    return (idiomaDetectado, textoTranscrito);
                }
                catch (Exception ex)
                {
                    return ("", ""); // En caso de error, retornar valores vacíos
                }
            }

        }
        public class ChatGPT
        {

            public async Task<string> GetVideoCategories(string videoTranscript)
            {    
                using HttpClient client = new HttpClient();
                string apiUrl = "https://api.openai.com/v1/engines/text-davinci-003/completions";
                List<string> categorias = GeneralesMD.ObtenerLasCategorias();
                string categoriasFormateadas = string.Join("\n", categorias);

                string prompt = "Selecciona un maximo de 4 categorias de esta lista:\n\n" + categoriasFormateadas  +
                                "\n\n Que encajen con el siguiente texto: \n\n" +
                                videoTranscript;

                var request = new
                {
                    prompt = prompt,
                    max_tokens = 30, // Ajusta este valor según tus necesidades
                };

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {"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"}");

                HttpResponseMessage response = await client.PostAsJsonAsync(apiUrl, request);
                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    // Aquí procesa la respuesta para extraer las categorías sugeridas
                    // Puedes usar alguna lógica para filtrar y formatear las categorías si es necesario
                    var data = JsonConvert.DeserializeObject<dynamic>(responseData);
                    return data.choices[0].text;
                }
                else
                {
                    Console.WriteLine($"Error en la solicitud: {response.StatusCode}");
                    return "";
                }
            }
            public List<string> ExtraerCategorias(string texto, List<string> categorias)
            {
                List<string> categoriasEncontradas = new List<string>();

                foreach (string categoria in categorias)
                {
                    string patron = "\\b" + Regex.Escape(categoria) + "\\b";
                    if (Regex.IsMatch(texto, patron, RegexOptions.IgnoreCase))
                    {
                        categoriasEncontradas.Add(categoria);
                    }
                }

                return categoriasEncontradas;
            }

            public List<string> EncontrarPalabrasComunes(params List<string>[] listas)
            {
                // Eliminar duplicados en cada lista antes de concatenarlas
                List<string> todasLasPalabras = listas.SelectMany(lista => lista.Distinct()).ToList();

                // Usar LINQ para encontrar las palabras que tienen al menos dos repeticiones
                var palabrasComunes = todasLasPalabras.GroupBy(palabra => palabra)
                    .Where(grupo => grupo.Count() >= 2)
                    .Select(grupo => grupo.Key)
                    .ToList();

                return palabrasComunes;
            }

            // Clase para deserializar la respuesta de OpenAI
            class OpenAIResponse
            {
                public List<Choice> choices { get; set; }
            }

            class Choice
            {
                public string text { get; set; }
            }
            public List<string> EncontrarPalabrasSimilares(string texto, List<string> categorias)
            {
                List<string> palabrasSimilares = new List<string>();
                string[] palabras = texto.Split(new char[] { ' ', '.', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string palabra in palabras)
                {
                    foreach (string categoria in categorias)
                    {
                        if (EsPalabraSimilar(palabra, categoria))
                        {
                            if (!palabrasSimilares.Contains(categoria))
                            {
                                palabrasSimilares.Add(categoria);
                            }
                            break; // Puedes detener la búsqueda después de encontrar una coincidencia
                        }
                    }
                }

                return palabrasSimilares;
            }

            public bool EsPalabraSimilar(string palabra, string categoria)
            {
                // Comparación insensible a mayúsculas y minúsculas
                return palabra.Equals(categoria, StringComparison.OrdinalIgnoreCase);
            }

            public  List<string> EliminarDuplicados(List<string> lista)
            {
                // Utilizar un HashSet para mantener un registro de las palabras únicas
                HashSet<string> palabrasUnicas = new HashSet<string>();

                List<string> listaSinDuplicados = new List<string>();

                foreach (string palabra in lista)
                {
                    // Agregar la palabra al HashSet, esto eliminará automáticamente duplicados
                    if (palabrasUnicas.Add(palabra))
                    {
                        // Si la palabra se agregó correctamente, también se agrega a la lista de salida
                        listaSinDuplicados.Add(palabra);
                    }
                }

                return listaSinDuplicados;
            }

        }
    }
}
