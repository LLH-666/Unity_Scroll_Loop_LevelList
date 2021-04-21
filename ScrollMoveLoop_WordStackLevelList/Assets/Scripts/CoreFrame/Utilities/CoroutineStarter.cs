using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineStarter : MonoBehaviour
{
    #region Public Methods

    public static void Start(IEnumerator routine)
    {
        new GameObject("routine").AddComponent<CoroutineStarter>().RunCoroutine(routine);
    }

    #endregion

    #region Private Methods

    private void RunCoroutine(IEnumerator routine)
    {
        StartCoroutine(RunCoroutineHelper(routine));
    }

    private IEnumerator RunCoroutineHelper(IEnumerator routine)
    {
        yield return routine;

        Destroy(gameObject);
    }

    #endregion
}