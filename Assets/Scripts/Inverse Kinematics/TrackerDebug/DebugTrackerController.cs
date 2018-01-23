using UnityEngine;
using Valve.VR;

public class DebugTrackerController : MonoBehaviour
{
    public TrackerDebugObject trackerObject;
    public Mesh tracker;
    public Mesh controller;

    private TrackerDebugObject[] trackerDebugObjects = new TrackerDebugObject[16];

    public TrackerDebugObject[] CreateTrackerModels()
    {
        for (int i = 1; i < 16; i++)
        {
            var tracker = Instantiate(trackerObject);
            tracker.tracker.SetDeviceIndex(i);
            tracker.Text.text = i.ToString();
            trackerDebugObjects[i] = tracker;
        }
        return trackerDebugObjects;
    }

    public void SetTrackerModels(ETrackedDeviceClass deviceClass, int index)
    {
        if (OpenVR.System.GetTrackedDeviceClass((uint)index) == ETrackedDeviceClass.Controller)
        {
            trackerDebugObjects[index].gameObject.GetComponent<MeshFilter>().mesh = controller;
        }
        if (OpenVR.System.GetTrackedDeviceClass((uint)index) == ETrackedDeviceClass.GenericTracker)
        {
            trackerDebugObjects[index].gameObject.GetComponent<MeshFilter>().mesh = tracker;
        }
    }
}
