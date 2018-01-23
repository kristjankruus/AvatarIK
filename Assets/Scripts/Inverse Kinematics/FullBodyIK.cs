using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

public class FullBodyIK : MonoBehaviour
{
    public GameObject leftHandOffsetObject;
    public GameObject rightHandOffsetObject;
    public GameObject leftElbowOffsetObject;
    public GameObject rightElbowOffsetObject;
    public GameObject leftKneeOffsetObject;
    public GameObject rightKneeOffsetObject;
    public GameObject leftFootOffsetObject;
    public GameObject rightFootOffsetObject;
    public GameObject markerHead;
    public Text logText;
    public Text numberOfDevicesText;
    public bool profileExists;
    public List<GameObject> characters;
    public DebugTrackerController debugTrackerController;
    public GameObject headset;

    IKModelState initModelState;
    ProfileController profileController;

    private float initHeight;
    private float profileHeight;
    private Transform eyeTransform;
    private TrackerDebugObject[] debugTrackersModels;
    private List<ThreePointIK> ikComponents;
    private GameObject currentCharacter;
    private bool turnedOffDeviceLogged = true;
    private Calibration calibration;
    private Profile savedProfile;

    private Dictionary<int, SteamVR_Controller.Device> trackedDevices;
    private Dictionary<int, SteamVR_Controller.Device> previousTrackedDevices;
    private Dictionary<CalibrationPointType, Transform> targets;
    List<OffsetTracking> offsetTrackedList = new List<OffsetTracking>();

    // Use this for initialization
    void Start()
    {
        calibration = new Calibration();
        profileController = new ProfileController();
        trackedDevices = new Dictionary<int, SteamVR_Controller.Device>();
        previousTrackedDevices = new Dictionary<int, SteamVR_Controller.Device>();
        debugTrackersModels = debugTrackerController.CreateTrackerModels();
        currentCharacter = characters.First();
        ikComponents = new List<ThreePointIK>();
        eyeTransform = currentCharacter.GetComponent<HeadIK>().EyeTransform;

        targets = new Dictionary<CalibrationPointType, Transform>
        {
            { CalibrationPointType.Head,Camera.main.transform },
            { CalibrationPointType.LeftHand,leftHandOffsetObject.transform },
            { CalibrationPointType.RightHand,rightHandOffsetObject.transform },
            { CalibrationPointType.LeftElbow,leftElbowOffsetObject.transform },
            { CalibrationPointType.RightElbow,rightElbowOffsetObject.transform },
            { CalibrationPointType.LowerBack,currentCharacter.transform },
            { CalibrationPointType.LeftKnee,leftKneeOffsetObject.transform },
            { CalibrationPointType.RightKnee,rightKneeOffsetObject.transform },
            { CalibrationPointType.LeftFoot,leftFootOffsetObject.transform },
            { CalibrationPointType.RightFoot,rightFootOffsetObject.transform }
        };

        RecordInitModelState();
        savedProfile = profileController.LoadProfileByName("testur");
        if (savedProfile != null)
        {
            //SetCharacterFromProfile(savedProfile);
            //profileExists = true;
        }
    }

    bool updateIK = false;
    bool calibrated = false;
    bool chartcterSwitcher = false;

    // Update is called once per frame
    void Update()
    {
        UpdateIK();

        if (!profileExists)
        {
            AutoAdjustHeight();
        }

        bool triggerClicked = false;
        bool gripClicked = false;
        bool touchClicked = false;

        int currentTrackedDeviceCount = 0;

        for (int i = 1; i < (int)SteamVR_TrackedObject.EIndex.Device15; i++)
        {
            var device = SteamVR_Controller.Input(i);
            if (device.hasTracking && device.connected && device.valid)
            {
                var deviceClass = OpenVR.System.GetTrackedDeviceClass((uint)i);
                if (deviceClass == ETrackedDeviceClass.Controller || deviceClass == ETrackedDeviceClass.GenericTracker)
                {
                    if (!trackedDevices.ContainsKey(i))
                    {
                        trackedDevices.Add(i, device);
                        debugTrackerController.SetTrackerModels(deviceClass, i);
                        logText.text += "\n Found device with number " + i + ", type " + deviceClass;
                    }

                    currentTrackedDeviceCount++;
                    triggerClicked |= device.GetPressUp(SteamVR_Controller.ButtonMask.Trigger);
                    gripClicked |= device.GetPressUp(SteamVR_Controller.ButtonMask.Touchpad);
                    touchClicked |= device.GetPressUp(SteamVR_Controller.ButtonMask.Grip);
                }
            }
        }

        numberOfDevicesText.text = string.Format("Number of devices: {0}", currentTrackedDeviceCount);

        if (!turnedOffDeviceLogged && trackedDevices.Count > 0)
        {
            foreach (var trackedDevice in previousTrackedDevices)
            {
                if (!trackedDevices.ContainsKey(trackedDevice.Key))
                {
                    logText.text += "\n " + (SteamVR_TrackedObject.EIndex)trackedDevice.Key + ", type = " + trackedDevice.Value + "turned off";
                    Debug.Log((SteamVR_TrackedObject.EIndex)trackedDevice.Key + ", type = " + trackedDevice.Value + " turned off");
                }
            }
            turnedOffDeviceLogged = true;
        }

        //If some device is turned off, entire device list is recalculated inside SteamVR
        if (currentTrackedDeviceCount < trackedDevices.Count)
        {
            previousTrackedDevices = trackedDevices;
            trackedDevices = new Dictionary<int, SteamVR_Controller.Device>();
            turnedOffDeviceLogged = false;
        }

        if (gripClicked)
        {
            ////var offset = leftHandOffsetObject.transform.position - savedProfile.CalibrationPoints.FirstOrDefault(x => x.Type == CalibrationPointType.LeftHand).DevicePosition;

            //var calibrationPoints = calibration.Calibrate(trackedDevices);
            ////leftHandOffsetObject.transform.position = calibrationPoints.Where(x => x.Type == CalibrationPointType.LeftHand).FirstOrDefault().Position - offset;
            ////rightHandOffsetObject.transform.position = calibrationPoints.Where(x => x.Type == CalibrationPointType.RightHand).FirstOrDefault().Position - offset;
            //updateIK = true;
            //gripClicked = false;

            gripClicked = false;
            updateIK = false;
            chartcterSwitcher = true;
            //offsetTrackedList = new List<OffsetTracking>();
            currentCharacter.SetActive(false);
            var chaarterIndex = characters.IndexOf(currentCharacter) + 1;

            currentCharacter = characters[chaarterIndex == -1 ? 0 : chaarterIndex % characters.Count];
            currentCharacter.SetActive(true);
            eyeTransform = currentCharacter.GetComponent<HeadIK>().EyeTransform;
            if (calibrated)
            {
                var root = offsetTrackedList.FirstOrDefault(x => x.pointType == CalibrationPointType.LowerBack);
                if (root != null)
                {
                    root.targetTrans = currentCharacter.transform;
                    root.targetTrans.rotation = Quaternion.identity;
                }
                StartIK();
                updateIK = true;
            }
        }

        //if (touchClicked)
        //{
        //    var calibrationPoints = calibration.Calibrate(trackedDevices);
        //    StartTracking2(calibrationPoints);
        //}

        if (triggerClicked && !calibrated)
        {
            var calibrationPoints = calibration.Calibrate(trackedDevices);
            StartTracking(calibrationPoints);
            var root = offsetTrackedList.FirstOrDefault(x => x.pointType == CalibrationPointType.LowerBack);
            if (root != null)
            {
                root.targetTrans = currentCharacter.transform;
                root.targetTrans.rotation = Quaternion.identity;
            }
            StartIK();
            updateIK = true;
            calibrated = true;
            profileExists = true;
            headset.SetActive(false);
            //SaveProfile(calibrationPoints);
        }
        if (updateIK)
        {
            if (chartcterSwitcher)
            {
                leftHandOffsetObject.transform.localPosition = initModelState.handMarkerLeftPos;
                rightHandOffsetObject.transform.localPosition = initModelState.handMarkerRightPos;
                leftHandOffsetObject.transform.localRotation = Quaternion.Euler(0, 90, 0);
                rightHandOffsetObject.transform.localRotation = Quaternion.Euler(0, -90, 0);
            }
            UpdateOffsetTracking();
        }
    }

    private void StartIK()
    {
        int headIndex = -1;

        var ikComps = currentCharacter.GetComponents<ThreePointIK>().ToList();
        var calibrationPoints = calibration.GetCalibrationPoints();
        for (int i = 0; i < ikComps.Count; i++)
        {
            var ikComponent = ikComps[i];
            if (ikComponent.bendNormalStrategy == ThreePointIK.BendNormalStrategy.leftArm && calibrationPoints.All(x => x.Type != CalibrationPointType.LeftHand) ||
                ikComponent.bendNormalStrategy == ThreePointIK.BendNormalStrategy.rightArm && calibrationPoints.All(x => x.Type != CalibrationPointType.RightHand))
            {
                ikComps.Remove(ikComponent);
            }
        }
        foreach (var item in ikComps)
        {
            item.manualUpdateIK = true;
            //item.trackSecondBone = (item.bendNormalStrategy == ThreePointIK.BendNormalStrategy.followTarget);
            item.enabled = true;

            ikComponents.Add(item);
        }

        headIndex = ikComponents.FindIndex(item => item.bendNormalStrategy == ThreePointIK.BendNormalStrategy.spine);
        if (headIndex >= 0)
            Swap(ikComponents, 0, headIndex);
    }

    void Swap<T>(List<T> list, int indexA, int indexB)
    {
        T temp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = temp;
    }

    private void UpdateIK()
    {
        foreach (var item in ikComponents)
        {
            item.UpdateIK();
        }
    }

    void RecordInitModelState()
    {
        //Siin saab initsialiseerida konfist
        initModelState = new IKModelState();
        initHeight = currentCharacter.GetComponentInChildren<SkinnedMeshRenderer>().bounds.size.y;
        initModelState.eyePos = eyeTransform.position;
        initModelState.ankleMarkerLeftPos = leftFootOffsetObject.transform.position;
        initModelState.ankleMarkerRightPos = rightFootOffsetObject.transform.position;
        initModelState.handMarkerLeftPos = leftHandOffsetObject.transform.position;
        initModelState.handMarkerRightPos = rightHandOffsetObject.transform.position;
        initModelState.markerHeadPos = markerHead.transform.position;
        initModelState.modelScale = currentCharacter.transform.localScale;
    }

    void AutoAdjustHeight()
    {
        float actualEyeHeight = Camera.main.transform.position.y;

        float eyeHeightToBodyHeadRatio = initModelState.eyePos.y / initHeight;
        profileHeight = actualEyeHeight / eyeHeightToBodyHeadRatio;

        SetCharacterHeight(profileHeight);
    }

    void AutoAdjustArmsLength()
    {
        float actualEyeHeight = Camera.main.transform.position.y;

        float eyeHeightToBodyHeadRatio = initModelState.eyePos.y / initHeight;
        profileHeight = actualEyeHeight / eyeHeightToBodyHeadRatio;

        SetCharacterHeight(profileHeight);
    }

    void SetCharacterHeight(float customHeight)
    {
        float ratio = customHeight / initHeight;
        leftFootOffsetObject.transform.position = initModelState.ankleMarkerLeftPos * ratio;
        rightFootOffsetObject.transform.position = initModelState.ankleMarkerRightPos * ratio;
        markerHead.transform.position = initModelState.markerHeadPos * ratio;
        currentCharacter.transform.localScale = initModelState.modelScale * ratio;
    }

    private void StartTracking(List<CalibrationPoint> calibrationPoints)
    {
        foreach (var calibrationPoint in calibrationPoints)
        {
            if (calibrationPoint.DeviceIndex >= 0)
            {
                var debugModel = debugTrackersModels[calibrationPoint.DeviceIndex];
                calibrationPoint.DevicePosition = debugModel.transform.position;

                var trackedInfo = new OffsetTracking();
                trackedInfo.deviceIndex = calibrationPoint.DeviceIndex;
                trackedInfo.targetTrans = targets[calibrationPoint.Type];
                trackedInfo.pointType = calibrationPoint.Type;
                trackedInfo.deviceTransform = debugModel.transform;
                if (calibrationPoint.Type == CalibrationPointType.LeftHand)
                {
                    Extensions.AssignChildAndKeepLocalTransform(debugModel.transform, leftHandOffsetObject.transform);
                }
                if (calibrationPoint.Type == CalibrationPointType.RightHand)
                {
                    Extensions.AssignChildAndKeepLocalTransform(debugModel.transform, rightHandOffsetObject.transform);
                }
                if (calibrationPoint.Type == CalibrationPointType.LeftElbow)
                {
                    Extensions.AssignChildAndKeepLocalTransform(debugModel.transform, leftElbowOffsetObject.transform);
                }
                trackedInfo.StartTracking();
                offsetTrackedList.Add(trackedInfo);
                debugModel.Text.text = calibrationPoint.Type.ToString();
                logText.text += "\n " + "Calibrated device " + calibrationPoint.Type + " device class " + OpenVR.System.GetTrackedDeviceClass((uint)calibrationPoint.DeviceIndex);
            }
        }
        markerHead.transform.parent = Camera.main.transform;
    }


    private void StartTracking2(List<CalibrationPoint> calibrationPoints)
    {
        //foreach (var calibrationPoint in calibrationPoints)
        //{
        //    if (calibrationPoint.DeviceIndex >= 0)
        //    {
        //        var debugModel = debugTrackersModels[calibrationPoint.DeviceIndex];
        //        calibrationPoint.DevicePosition = debugModel.transform.position;

        //        var trackedInfo = new OffsetTracking();
        //        trackedInfo.deviceIndex = calibrationPoint.DeviceIndex;
        //        trackedInfo.targetTrans = targets[calibrationPoint.Type];
        //        trackedInfo.pointType = calibrationPoint.Type;
        //        trackedInfo.deviceTransform = debugModel.transform;
        //        trackedInfo.StartTracking();
        //        offsetTrackedList.Add(trackedInfo);
        //        debugModel.Text.text = calibrationPoint.Type.ToString();
        //    }
        //}
        //markerHead.transform.parent = Camera.main.transform;
        foreach (var calibrationPoint in calibrationPoints.Where(x => x.Type == CalibrationPointType.LeftHand))
        {
            if (calibrationPoint.DeviceIndex >= 0)
            {
                var debugModel = debugTrackersModels[calibrationPoint.DeviceIndex];
                if (calibrationPoint.Type == CalibrationPointType.LeftHand)
                {
                    Extensions.AssignChildAndKeepLocalTransform(debugModel.transform, leftHandOffsetObject.transform);
                }
            }
        }

        var ikComps = currentCharacter.GetComponents<ThreePointIK>().FirstOrDefault(x => x.bendNormalStrategy == ThreePointIK.BendNormalStrategy.leftArm);
        if (ikComps != null)
        {
            ikComps.manualUpdateIK = true;
            ikComps.enabled = true;
            updateIK = true;
            calibrated = true;
            profileExists = true;
        }
    }

    void UpdateOffsetTracking()
    {
        foreach (var item in offsetTrackedList.Where(x => x.pointType == CalibrationPointType.LowerBack || x.pointType == CalibrationPointType.LeftFoot || x.pointType == CalibrationPointType.RightFoot))
            item.UpdateOffsetTracking();

        foreach (var item in offsetTrackedList.Where(x => x.pointType == CalibrationPointType.LeftElbow))
        {
            item.UpdateOffsetPosition();
        }
    }

    #region Profile
    private void SaveProfile(List<CalibrationPoint> calibrationPoints)
    {
        savedProfile = new Profile
        {
            Height = profileHeight,
            CalibrationPoints = calibrationPoints,
            ProfileName = "testur",
            LastSavedTime = DateTime.Now
        };
        profileController.SaveProfile(savedProfile);
        profileExists = true;
    }

    void SetCharacterFromProfile(Profile profile)
    {
        //markerHead.transform.position = Camera.main.transform.position;
        float ratio = (profile.Height / initHeight) == 0 ? profile.Height / initHeight : 1.0f;
        currentCharacter.transform.localScale = initModelState.modelScale * ratio;
        StartTracking2(profile.CalibrationPoints);
        StartIK();
        //updateIK = true;
    }
    #endregion
}
