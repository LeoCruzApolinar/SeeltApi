using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;

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
                        if (confidence_en > confidence_es)
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
                catch (Exception)
                {
                    return ("", ""); // En caso de error, retornar valores vacíos
                }
            }


        }
    }
}
