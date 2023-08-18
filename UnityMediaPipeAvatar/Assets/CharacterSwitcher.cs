using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSwitcher : MonoBehaviour
{
    public Avatar[] avatars;

    int index = -1;
    bool initialized;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if(!initialized)
            {
                foreach(Avatar a in avatars)
                {
                    a.gameObject.SetActive(false);
                }
                initialized = true;
            }
            if(index >=0)
                avatars[index].gameObject.SetActive(false);
            ++index;
            if (index >= avatars.Length) index = 0;
            avatars[index].gameObject.SetActive(true);
        }
    }
}
