using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
/// <sumemary>
/// Cache various values which will be reused during the runtime.
/// </summary>
public class CalibrationData
{
    [SerializeField]
    public string parentn, childn, tparentn, tchildn; // for doing a lookup at runtime
    [System.NonSerialized]
    public Transform parent, child, tparent, tchild;
    [SerializeField]
    public Vector3 initialDir;
    [SerializeField]
    public Quaternion initialRotation;
    [SerializeField]
    public Quaternion targetRotation;

    public void Tick(Quaternion newTarget, float speed)
    {
        parent.rotation = newTarget;
        parent.rotation = Quaternion.Lerp(parent.rotation, targetRotation, Time.deltaTime * speed);
    }

    public Vector3 CurrentDirection => (tchild.position - tparent.position).normalized;

    public CalibrationData(Transform topParent, Transform fparent, Transform fchild, Transform tparent, Transform tchild)
    {
        initialDir = (tchild.position - tparent.position).normalized;
        initialRotation = fparent.rotation;

        this.parent = fparent;
        this.child = fchild;
        this.tparent = tparent;
        this.tchild = tchild;

        parentn = GetPath(parent);
        childn = GetPath(child);
        tparentn = GetPath(tparent);
        tchildn = GetPath(tchild);
    }
    public CalibrationData ReconstructReferences()
    {
        SetFromPath(parentn, out parent);
        SetFromPath(childn, out child);
        SetFromPath(tparentn, out tparent);
        SetFromPath(tchildn, out tchild);
        return this;
    }
    private void SetFromPath(string path, out Transform target)
    {
        if(path != null&&path != "")
        {
            target = GameObject.Find(path).transform;
            return;
        }
        target = null;
    }
    private string GetPath(Transform child)
    {
        List<Transform> chain = new List<Transform>();
        while (child != null)
        {
            chain.Add(child);
            child = child.parent;
        }
        chain.Reverse();

        string s = "";
        foreach (Transform t in chain)
        {
            s += t.name + "/";
        }
        return s;
    }

}