using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (GameplayInputBlocker.IsBlocked) return;

        if (Input.GetKeyDown(KeyCode.K))
        {
            LevelWorldManager.Instance.RequestTotalSwitch();
        }
    }
}
