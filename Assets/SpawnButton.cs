using UnityEngine;

public class SpawnButton : MonoBehaviour
{
    public GameObject button;
    public Transform player;
    public float distanceFromPlayer = 1.5f;
    public float heightOffset = 0f;

    void Start()
    {
        MoveButtonInFrontOfPlayer();
    }

    void MoveButtonInFrontOfPlayer()
    {
        if (button == null)
        {
            Debug.LogError("Button GameObject is not assigned!");
            return;
        }

        if (player == null)
        {
            Debug.LogError("Player transform is not assigned!");
            return;
        }

        // Calculate position in front of the player
        Vector3 newPosition = player.position + player.forward * distanceFromPlayer + Vector3.up * heightOffset;
        
        // Move the button to the new position
        button.transform.position = newPosition;
        
        // Make the button face the player
        button.transform.rotation = Quaternion.LookRotation(newPosition - player.position);
        
        Debug.Log("Button moved to: " + newPosition);
    }
}
