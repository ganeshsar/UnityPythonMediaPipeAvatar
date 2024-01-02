using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CalibrationTimer : MonoBehaviour
{
    public PipeServer server;
    public int timer = 5;
    public KeyCode calibrationKey = KeyCode.C;
    public TextMeshProUGUI text;

    private bool calibrated;

    private void Start()
    {
        bool shouldEnable = false;
        Avatar[] a = FindObjectsByType<Avatar>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Avatar aa in a)
        {
            if (!aa.isActiveAndEnabled) continue;
            if (!aa.calibrationData)
            {
                shouldEnable = true;
                break;
            }
        }
        text.text = shouldEnable ? "Press " + calibrationKey + " to start calibration timer." : "";

        gameObject.SetActive(shouldEnable);
        if (!shouldEnable)
        {
            server.SetVisible(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(calibrationKey))
        {
            if(!calibrated)
            {
                calibrated = true;
                StartCoroutine(Timer());
            }
            else
            {
                StartCoroutine(Notify());
            }
        }
    }
    private IEnumerator Timer()
    {
        int t = timer;
        while (t > 0)
        {
            text.text = "Copy the avatars starting pose: "+t.ToString();
            yield return new WaitForSeconds(1f);
            --t;
        }
        Avatar[] a = FindObjectsByType<Avatar>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach(Avatar aa in a)
        {
            if (!aa.isActiveAndEnabled) continue;
            aa.Calibrate();
        }
        if (a.Length>0)
        {
            text.text = "Calibration Completed";
            server.SetVisible(false);
        }
        else
        {
            text.text = "Avatar in scene not found...";
        }
        yield return new WaitForSeconds(1.5f);
        text.text = "";
    }
    private IEnumerator Notify()
    {
        text.text = "Must restart instance to recalibrate."; // currently a limitation of the way things are set up
        yield return new WaitForSeconds(3f);
        text.text = "";
    }
}
