using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class MRDocumentLoader : MonoBehaviour
{
    public string query = "wine production Switzerland";
    public GameObject cardPrefab;      // Prefab with a MeshRenderer for image + TextMeshPro for text
    public Transform player;

    public float radius = 2.0f;
    private string apiBase = "https://stergios.hopto.org:5020/search/";

    void Start()
    {
        StartCoroutine(FetchDocuments());
    }

    IEnumerator FetchDocuments()
    {
        string url = apiBase + UnityWebRequest.EscapeURL(query);
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Fetch error: " + www.error);
            }
            else
            {
                var json = www.downloadHandler.text;
                SearchResponse resp = JsonConvert.DeserializeObject<SearchResponse>(json);

                // Flatten metadata (because it's nested)
                List<Metadata> allDocs = new List<Metadata>();
                foreach (var docList in resp.metadata)
                    allDocs.AddRange(docList);

                StartCoroutine(SpawnCards(allDocs));
            }
        }
    }

    IEnumerator SpawnCards(List<Metadata> docs)
    {
        radius = 2.0f;
        float angleStep = 360f / docs.Count;
        int i = 0;

        foreach (var doc in docs)
        {
            Vector3 pos = player.position + Quaternion.Euler(0, angleStep * i, 0) * (Vector3.forward * radius);
            // GameObject card = Instantiate(cardPrefab, pos, Quaternion.LookRotation(player.position - pos));
            // 1. Reverse the direction
            GameObject card = Instantiate(cardPrefab, pos, Quaternion.LookRotation(pos - player.position));


            // Set text on card
            var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2)
            {
                texts[0].text = !string.IsNullOrEmpty(doc.Location) ? doc.Location : "Unknown";
                texts[1].text = doc.text.Length > 200 ? doc.text.Substring(0, 200) + "..." : doc.text;
            }
            Debug.Log(texts.Length);
            Debug.Log(doc.text);

            // Download and apply image if available
            if (!string.IsNullOrEmpty(doc.image_render_url))
            {
                UnityWebRequest imgRequest = UnityWebRequestTexture.GetTexture(doc.image_render_url);
                yield return imgRequest.SendWebRequest();

                if (imgRequest.result == UnityWebRequest.Result.Success)
                {
                    Texture2D tex = DownloadHandlerTexture.GetContent(imgRequest);
                    UnityEngine.UI.RawImage rawImage = card.GetComponentInChildren<UnityEngine.UI.RawImage>();
                    if (rawImage != null)
                    {
                        rawImage.texture = tex;
                    }
                }
                else
                {
                    Debug.LogWarning("Failed to load image: " + imgRequest.error);
                }
            }

            i++;
        }
    }

    //update query
    public void UpdateQuery(string newQuery)
    {
        query = newQuery;
        // Optionally, you can restart the document fetching process here
        StartCoroutine(FetchDocuments());
    }
}

[System.Serializable]
public class SearchResponse
{
    public string answer;
    public List<List<Metadata>> metadata;
}

[System.Serializable]
public class Metadata
{
    public string Location;
    public string text;
    public string title;
    public string pdf_url;
    public string image_url;
    public string image_render_url;
    public string coordinates;
}
