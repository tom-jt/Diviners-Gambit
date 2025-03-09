using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class VfxUIManager : NetworkBehaviour
{
    private List<GameObject> spawnedArrows = new List<GameObject>();
    private IEnumerator coroutine;

    [Header("Visuals")]
    [SerializeField] [Range(0f, 1f)]
    private float arrowPosLerp;

    [Header("Assignments")]
    [SerializeField]
    private Transform arrowRoot;
    [SerializeField]
    private float arrowAnimDelay;
    [SerializeField]
    private GameObject arrowPrefab;

    public void SpawnArrow(Vector2 startPos, Vector2 endPos)
    {
        GameObject vfx = Instantiate(arrowPrefab, arrowRoot);

        Vector2 position = Vector2.Lerp(startPos, endPos, arrowPosLerp);

        float xDiff = endPos.x - startPos.x;
        float yDiff = endPos.y - startPos.y;
        float angle = Mathf.Atan(yDiff / xDiff);

        if (xDiff < 0)
            angle += Mathf.PI;

        Quaternion quadAngle = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);

        vfx.transform.SetPositionAndRotation(position, quadAngle);

        coroutine = ArrowAnimation(vfx);
        StartCoroutine(coroutine);
        spawnedArrows.Add(vfx);
    }
    
    private IEnumerator ArrowAnimation(GameObject vfx)
    {
        Animator[] animators = vfx.GetComponentsInChildren<Animator>();

        for (int index = 0; index < animators.Length; index++)
        {
            animators[index].Play("ArrowTarget");

            yield return new WaitForSeconds(arrowAnimDelay);
        }
    }

    public void DestroyArrows()
    {
        if (coroutine != null)
            StopCoroutine(coroutine);

        for (int arrow = 0; arrow < spawnedArrows.Count; arrow++)
            Destroy(spawnedArrows[arrow]);

        spawnedArrows.Clear();
    }
}
