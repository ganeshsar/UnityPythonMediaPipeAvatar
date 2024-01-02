using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewCamera : MonoBehaviour
{
    public Camera previewCamera;
    public Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        // The tracking of the camera.
        if (previewCamera&&animator&&animator.isActiveAndEnabled)
        {
            Quaternion q = Quaternion.LookRotation((animator.GetBoneTransform(HumanBodyBones.Chest).transform.position - previewCamera.transform.position).normalized, Vector3.up);
            previewCamera.transform.rotation = Quaternion.Lerp(previewCamera.transform.rotation, q, Time.deltaTime * 3f);
        }
    }
}
