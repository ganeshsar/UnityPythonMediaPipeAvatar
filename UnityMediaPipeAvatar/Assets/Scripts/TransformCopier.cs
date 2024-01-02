using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Simply copy's a transform and it's children on to another same object.
// Optionally generates a free parent to arbitrarily move/rotate.
public class TransformCopier : MonoBehaviour
{
    public Transform source; // The source. Probably the transform with the Avatar component.
    public Transform destination; // The destination. Duplicate the source, disable the Avatar, assign this new transform here.

    private Dictionary<Transform, Transform> transforms = new Dictionary<Transform, Transform>();

    private void Start()
    {
        if (source.name == destination.name)
        {
            destination.name += "(dst)";
        }

        Transform[] all = source.GetComponentsInChildren<Transform>();
        Transform[] alld = destination.GetComponentsInChildren<Transform>();
        foreach (Transform t in all)
        {
            if (t.GetComponent<SkinnedMeshRenderer>() != null) continue;
            Transform match = null;
            foreach(Transform t1 in alld)
            {
                if (t.name == t1.name)
                {
                    match = t1;
                    break;
                }
            }
            transforms.Add(t, match);
        }
    }

    private void LateUpdate()
    {
        foreach(KeyValuePair<Transform, Transform> k in transforms)
        {
            if (k.Value == null) continue;
            k.Value.localPosition = k.Key.localPosition;
            k.Value.localRotation = k.Key.localRotation;
            k.Value.localScale = k.Key.localScale;
        }
    }
}
