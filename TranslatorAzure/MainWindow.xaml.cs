using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Newtonsoft.Json.Linq;

namespace TranslatorAzure
{
    public record Response(Translation[] translations);

    public record Translation(string text, string to);

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string SubscriptionKey = "<SubscriptionKey>";
        private static readonly string Endpoint = "https://api.cognitive.microsofttranslator.com/";

        private static readonly SpeechConfig SpeechConfig =
            SpeechConfig.FromSubscription("SubscriptionKey", "eastus");

        // Add your location, also known as region. The default is global.
        // This is required if using a Cognitive Services resource.
        private static readonly string location = "francecentral";

        public MainWindow()
        {
            InitializeComponent();
            GetLanguages();
        }

        private void Translate_OnClick(object sender, RoutedEventArgs e)
        {
            Translate();
        }

        private async void Translate()
        {
            // Input and output languages are defined as parameters.
            string route = $"/translate?api-version=3.0&from=en&to={Languages.SelectedItem}";
            string textToTranslate = Input.Text;
            object[] body = { new { Text = textToTranslate } };
            var requestBody = JsonSerializer.Serialize(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                // Build the request.
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(Endpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", location);

                // Send the request and get response.
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                // Read response as a string.
                var result = JsonSerializer.Deserialize<Response[]>(await response.Content.ReadAsStringAsync());

                Dispatcher.Invoke(() =>
                {
                    Result.Text = "";
                    foreach (var translation in result[0].translations)
                    {
                        Result.Text += $"{translation.text}\n";
                    }
                });
            }
        }

        private async void Listen_OnClick(object sender, RoutedEventArgs e)
        {
            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var recognizer = new SpeechRecognizer(SpeechConfig, audioConfig);
            ListenButton.Content = "Listening...";
            ListenButton.IsEnabled = false;
            var result = await recognizer.RecognizeOnceAsync();
            Dispatcher.Invoke(() => { Input.Text = result.Text; });
            ListenButton.Content = "Listen";
            ListenButton.IsEnabled = true;
        }

        private void GetLanguages()
        {
            string route = "/languages?api-version=3.0";

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                // Set the method to GET
                request.Method = HttpMethod.Get;
                // Construct the full URI
                request.RequestUri = new Uri(Endpoint + route);
                // Send request, get response
                var response = client.SendAsync(request).Result;
                var jsonResponse = response.Content.ReadAsStringAsync().Result;
                
                var result = JObject.Parse(jsonResponse)["translation"];

                Dispatcher.Invoke(() =>
                {
                    if (result == null) return;
                    foreach (var jToken in result)
                    {
                        var prop = (JProperty)jToken;
                        Languages.Items.Add(prop.Name);
                    }

                    Languages.SelectedIndex = 0;
                });
            }
        }
    }
}
