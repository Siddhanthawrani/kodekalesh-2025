using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;  // Add this if using TextMeshPro

public class PerplexityAPIClient : MonoBehaviour
{
    private readonly string apiUrl = "https://api.perplexity.ai/chat/completions";
    private string apiKey = "";

    [System.Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class RequestBody
    {
        public string model = "sonar-pro";
        public Message[] messages;
    }

    [System.Serializable]
    public class ResponseChoice
    {
        public Message message;
    }

    [System.Serializable]
    public class ApiResponse
    {
        public ResponseChoice[] choices;
    }

    // Attach your UI Text GameObject here in the Inspector
    public TMP_Text responseText;

    public void CallPerplexityAPI(string userQuestion)
    {
        StartCoroutine(PostRequest(userQuestion));
    }

    private IEnumerator PostRequest(string userQuestion)
    {
        RequestBody requestBody = new RequestBody
        {
            messages = new[]
            {
                new Message { role = "system", content = "You are a helpful assistant." },
                new Message { role = "user", content = userQuestion }
            }
        };

        string jsonData = JsonUtility.ToJson(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ApiResponse response = JsonUtility.FromJson<ApiResponse>(request.downloadHandler.text);
                // Update the UI Text with the response content
                responseText.text = response.choices[0].message.content;
            }
            else
            {
                responseText.text = "Error: " + request.error;
            }
        }
    }
}
