using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class PlayerHandManager : NetworkBehaviour
{
    [SerializeField]
    private GameObject handPrefab;
    [SerializeField]
    private float showHandDelay;
    [SerializeField]
    private TextMeshProUGUI genText;

    private readonly SyncDictionary<int, Transform> generationHands = new SyncDictionary<int, Transform>();

    private int handIndex;
    public int HandIndex { set { handIndex = value; genText.text = handIndex >= 0 ? "Card Pool " + (handIndex + 1) : "Diviner Section"; } }

    private bool waiting = true;

    public Transform CreateNewHand(int index)
    {
        if (!generationHands.TryGetValue(index, out Transform _))
        {
            GameObject instantiatedHand = Instantiate(handPrefab);
            NetworkServer.Spawn(instantiatedHand);

            generationHands.Add(index, instantiatedHand.transform);
        }

        return GetHand(index);
    }

    public Transform GetHand(int index) => generationHands[index].transform;

    [ClientRpc]
    public void RpcHandAnimations()
    {
        StartCoroutine(WaitForSyncDictionary());
    }

    private IEnumerator WaitForSyncDictionary()
    {
        yield return null;

        foreach (int key in generationHands.Keys)
        {
            generationHands[key].GetComponent<GenerationHandScript>().ToggleHand(key == handIndex);
        }

        waiting = false;
    }

    [ClientRpc]
    public void RpcChangeHandIndex(int index)
    {
        HandIndex = index;
    }

    private void Update()
    {
        if (waiting)
            return;

        if (Input.GetAxis("Mouse ScrollWheel") != 0f)
        {
            ChangeHand(Input.GetAxis("Mouse ScrollWheel") <= 0f);
        }
    }

    private void ChangeHand(bool increase)
    {
        generationHands[handIndex].GetComponent<GenerationHandScript>().ToggleHand(false);

        //find the next hand index
        int toggleInt = increase ? 1 : -1;
        int smallest = CardDatabaseManager.DefaultCardPool.Length;
        int largest = -1;
        int closest = increase ? smallest : largest;
        foreach (int key in generationHands.Keys)
        {
            if ((key * toggleInt > handIndex * toggleInt) && (key * toggleInt < closest * toggleInt))
            {
                closest = key;
            }

            if (key < smallest)
            {
                smallest = key;
            }

            if (key > largest)
            {
                largest = key;
            }
        }

        if (increase && (handIndex == largest))
            HandIndex = smallest;
        else if (!increase && (handIndex == smallest))
            HandIndex = largest;
        else
            HandIndex = closest;

        StartCoroutine(ShowHandDelay());
    }

    private IEnumerator ShowHandDelay()
    {
        waiting = true;
        yield return new WaitForSeconds(showHandDelay);

        generationHands[handIndex].GetComponent<GenerationHandScript>().ToggleHand(true);
        waiting = false;
    }
}
