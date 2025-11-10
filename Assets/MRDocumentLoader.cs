using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System;

public class MRDocumentLoader : MonoBehaviour
{
    public string query = "wine production Switzerland";
    public GameObject cardPrefab;      // Prefab with a MeshRenderer for image + TextMeshPro for text
    public Transform player;

    // When enabled, if the API request fails, returns empty, or parsing fails,
    // we will proceed by spawning 5 clearly marked SAMPLE cards so the scene remains usable.
    public bool useSampleDataInCaseOfTimeout = true;

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
                Debug.LogError("Fetch error: " + www.error + (useSampleDataInCaseOfTimeout ? "; using 5 sample cards to proceed." : "; sample fallback disabled; aborting."));
                if (useSampleDataInCaseOfTimeout)
                {
                    var sampleDocs = BuildSampleDocs(5, "Network error: " + www.error);
                    yield return StartCoroutine(SpawnCards(sampleDocs));
                }
                yield break;
            }
            else
            {
                List<Metadata> docsToSpawn = null;
                try
                {
                    var json = www.downloadHandler.text;
                    SearchResponse resp = JsonConvert.DeserializeObject<SearchResponse>(json);

                    // Flatten metadata (because it's nested)
                    List<Metadata> allDocs = new List<Metadata>();
                    if (resp?.metadata != null)
                    {
                        foreach (var docList in resp.metadata)
                        {
                            if (docList != null)
                                allDocs.AddRange(docList);
                        }
                    }

                    docsToSpawn = allDocs;
                }
                catch (Exception ex)
                {
                    Debug.LogError("Response parsing error: " + ex.Message + (useSampleDataInCaseOfTimeout ? "; will use 5 sample cards to proceed." : "; sample fallback disabled; aborting."));
                    if (useSampleDataInCaseOfTimeout)
                    {
                        docsToSpawn = BuildSampleDocs(5, "Parse error: " + ex.Message);
                    }
                    else
                    {
                        docsToSpawn = null;
                    }
                }

                if (docsToSpawn == null || docsToSpawn.Count == 0)
                {
                    if (useSampleDataInCaseOfTimeout)
                    {
                        Debug.LogWarning("No documents returned; using 5 sample cards to proceed.");
                        docsToSpawn = BuildSampleDocs(5, docsToSpawn == null ? "Empty or null API response" : "Empty documents list");
                    }
                    else
                    {
                        Debug.LogWarning("No documents returned and sample fallback disabled; aborting.");
                        yield break;
                    }
                }

                yield return StartCoroutine(SpawnCards(docsToSpawn));
            }
        }
    }

    IEnumerator SpawnCards(List<Metadata> docs)
    {
        if (docs == null || docs.Count == 0)
        {
            Debug.LogWarning("SpawnCards called with no documents; nothing to spawn.");
            yield break;
        }
        float angleStep = 360f / Mathf.Max(1, docs.Count);
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
                string header = !string.IsNullOrEmpty(doc.Location) ? doc.Location : "Unknown";
                string body = string.IsNullOrEmpty(doc.text) ? "" : doc.text;
                texts[0].text = header;
                texts[1].text = body.Length > 200 ? body.Substring(0, 200) + "..." : body;
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

    /// <summary>
    /// Builds a list of sample Metadata entries to allow the app to proceed when
    /// network or parsing errors occur. Each item is clearly marked as SAMPLE.
    /// </summary>
    private List<Metadata> BuildSampleDocs(int count, string reason = null)
    {
        var list = new List<Metadata>(Mathf.Max(1, count));
        for (int i = 0; i < Mathf.Max(1, count); i++)
        {
            list.Add(new Metadata
            {
                Location = $"[SAMPLE] Location {i + 1}",
                title = $"Sample Card {i + 1}",
                text = $"[SAMPLE] This is placeholder content for card {i + 1}. {(string.IsNullOrEmpty(reason) ? "" : "Reason: " + reason)}",
                pdf_url = string.Empty,
                image_url = string.Empty,
                image_render_url = string.Empty,
                coordinates = string.Empty
            });
        }
        return list;
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
