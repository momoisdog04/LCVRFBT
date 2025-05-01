using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using LCVR.Networking;

namespace LCVR.Player;

/// <summary>
/// Handles full body tracking for VR players
/// </summary>
public class FBTTracker : MonoBehaviour
{
    // Constants for tracker roles
    public const string TRACKER_ROLE_WAIST = "waist";
    public const string TRACKER_ROLE_LEFT_FOOT = "left_foot";
    public const string TRACKER_ROLE_RIGHT_FOOT = "right_foot";
    public const string TRACKER_ROLE_LEFT_KNEE = "left_knee";
    public const string TRACKER_ROLE_RIGHT_KNEE = "right_knee";

    // Dictionary to store active trackers by role
    private Dictionary<string, TrackerInfo> activeTrackers = new();
    
    // Reference to the VRPlayer component
    private VRPlayer vrPlayer;
    
    // Transforms for the tracked body parts
    public Transform waistTarget;
    public Transform leftFootTarget;
    public Transform rightFootTarget;
    public Transform leftKneeTarget;
    public Transform rightKneeTarget;
    
    // Calibration offsets
    private Vector3 waistOffset = Vector3.zero;
    private Vector3 leftFootOffset = Vector3.zero;
    private Vector3 rightFootOffset = Vector3.zero;
    private Vector3 leftKneeOffset = Vector3.zero;
    private Vector3 rightKneeOffset = Vector3.zero;
    
    // Calibration rotation offsets
    private Quaternion waistRotOffset = Quaternion.identity;
    private Quaternion leftFootRotOffset = Quaternion.identity;
    private Quaternion rightFootRotOffset = Quaternion.identity;
    private Quaternion leftKneeRotOffset = Quaternion.identity;
    private Quaternion rightKneeRotOffset = Quaternion.identity;
    
    // Flag to indicate if FBT is active
    public bool IsFBTActive { get; private set; }
    
    // Event triggered when FBT status changes
    public event Action<bool> OnFBTStatusChanged;

    private void Awake()
    {
        vrPlayer = GetComponent<VRPlayer>();
    }

    private void Start()
    {
        // Initialize targets if they don't exist
        InitializeTargets();
        
        // Only start scanning for trackers if FBT is enabled in config
        if (Plugin.Instance.Config.EnableFBT.Value)
        {
            // Start looking for trackers
            StartCoroutine(ScanForTrackers());
            Logger.LogInfo("FBT tracking enabled, scanning for trackers...");
        }
        else
        {
            Logger.LogInfo("FBT tracking disabled in config");
        }
    }

    private void InitializeTargets()
    {
        // Create targets if they don't exist
        if (waistTarget == null)
        {
            var waistObj = new GameObject("WaistTarget");
            waistTarget = waistObj.transform;
            waistTarget.parent = transform;
        }
        
        if (leftFootTarget == null)
        {
            var leftFootObj = new GameObject("LeftFootTarget");
            leftFootTarget = leftFootObj.transform;
            leftFootTarget.parent = transform;
        }
        
        if (rightFootTarget == null)
        {
            var rightFootObj = new GameObject("RightFootTarget");
            rightFootTarget = rightFootObj.transform;
            rightFootTarget.parent = transform;
        }
        
        if (leftKneeTarget == null)
        {
            var leftKneeObj = new GameObject("LeftKneeTarget");
            leftKneeTarget = leftKneeObj.transform;
            leftKneeTarget.parent = transform;
        }
        
        if (rightKneeTarget == null)
        {
            var rightKneeObj = new GameObject("RightKneeTarget");
            rightKneeTarget = rightKneeObj.transform;
            rightKneeTarget.parent = transform;
        }
    }

    private System.Collections.IEnumerator ScanForTrackers()
    {
        while (true)
        {
            // Check for connected trackers
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevices(inputDevices);
            
            bool foundTrackers = false;
            
            foreach (var device in inputDevices)
            {
                // Skip non-trackers
                if (device.characteristics.HasFlag(InputDeviceCharacteristics.TrackedDevice) && 
                    !device.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted) &&
                    !device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
                {
                    // Try to determine the role of this tracker
                    string role = DetermineTrackerRole(device);
                    
                    if (!string.IsNullOrEmpty(role))
                    {
                        // Add or update the tracker
                        if (!activeTrackers.ContainsKey(role))
                        {
                            activeTrackers[role] = new TrackerInfo { Device = device, Role = role };
                            Logger.LogInfo($"Found tracker for {role}: {device.name}");
                            foundTrackers = true;
                        }
                    }
                }
            }
            
            // Update FBT status
            bool newStatus = HasMinimumTrackersForFBT();
            if (newStatus != IsFBTActive)
            {
                IsFBTActive = newStatus;
                OnFBTStatusChanged?.Invoke(IsFBTActive);
                
                if (IsFBTActive)
                {
                    Logger.LogInfo("Full Body Tracking is now active");
                    CalibrateTrackers();
                }
                else
                {
                    Logger.LogInfo("Full Body Tracking is now inactive");
                }
            }
            
            yield return new WaitForSeconds(foundTrackers ? 5f : 1f);
        }
    }

    private string DetermineTrackerRole(InputDevice device)
    {
        // Try to determine the role based on the device name or characteristics
        string deviceName = device.name.ToLower();
        
        if (deviceName.Contains("waist") || deviceName.Contains("hip"))
            return TRACKER_ROLE_WAIST;
        else if (deviceName.Contains("left") && (deviceName.Contains("foot") || deviceName.Contains("ankle")))
            return TRACKER_ROLE_LEFT_FOOT;
        else if (deviceName.Contains("right") && (deviceName.Contains("foot") || deviceName.Contains("ankle")))
            return TRACKER_ROLE_RIGHT_FOOT;
        else if (deviceName.Contains("left") && deviceName.Contains("knee"))
            return TRACKER_ROLE_LEFT_KNEE;
        else if (deviceName.Contains("right") && deviceName.Contains("knee"))
            return TRACKER_ROLE_RIGHT_KNEE;
        
        // If we can't determine the role, return null
        return null;
    }

    private bool HasMinimumTrackersForFBT()
    {
        // At minimum, we need waist and feet trackers for basic FBT
        return activeTrackers.ContainsKey(TRACKER_ROLE_WAIST) &&
               activeTrackers.ContainsKey(TRACKER_ROLE_LEFT_FOOT) &&
               activeTrackers.ContainsKey(TRACKER_ROLE_RIGHT_FOOT);
    }

    private void CalibrateTrackers()
    {
        // Show calibration prompt if enabled in config
        if (Plugin.Instance.Config.ShowFBTCalibrationPrompt.Value)
        {
            HUDManager.Instance.DisplayTip("Full Body Tracking Detected", "Stand in T-pose for calibration", true, false, "FBT");
        }
        
        // Get the player's head position as reference
        Transform head = vrPlayer.PlayerController.gameplayCamera.transform;
        Vector3 headPosition = head.position;
        Quaternion headRotation = head.rotation;
        
        // Calculate forward direction (ignoring Y)
        Vector3 forward = head.forward;
        forward.y = 0;
        forward.Normalize();
        
        // Calculate the calibration offsets
        if (activeTrackers.TryGetValue(TRACKER_ROLE_WAIST, out var waistTracker))
        {
            if (waistTracker.Device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                waistTracker.Device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                // Store the offset from the head
                waistOffset = headPosition - position;
                waistOffset.y = 0; // Keep the same height
                
                // Calculate rotation offset
                Quaternion forwardRotation = Quaternion.LookRotation(forward, Vector3.up);
                waistRotOffset = Quaternion.Inverse(rotation) * forwardRotation;
            }
        }
        
        // Similar calibration for other trackers
        if (activeTrackers.TryGetValue(TRACKER_ROLE_LEFT_FOOT, out var leftFootTracker))
        {
            if (leftFootTracker.Device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                leftFootTracker.Device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                leftFootOffset = headPosition - position;
                
                Quaternion forwardRotation = Quaternion.LookRotation(forward, Vector3.up);
                leftFootRotOffset = Quaternion.Inverse(rotation) * forwardRotation;
            }
        }
        
        if (activeTrackers.TryGetValue(TRACKER_ROLE_RIGHT_FOOT, out var rightFootTracker))
        {
            if (rightFootTracker.Device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                rightFootTracker.Device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                rightFootOffset = headPosition - position;
                
                Quaternion forwardRotation = Quaternion.LookRotation(forward, Vector3.up);
                rightFootRotOffset = Quaternion.Inverse(rotation) * forwardRotation;
            }
        }
        
        if (activeTrackers.TryGetValue(TRACKER_ROLE_LEFT_KNEE, out var leftKneeTracker))
        {
            if (leftKneeTracker.Device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                leftKneeTracker.Device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                leftKneeOffset = headPosition - position;
                
                Quaternion forwardRotation = Quaternion.LookRotation(forward, Vector3.up);
                leftKneeRotOffset = Quaternion.Inverse(rotation) * forwardRotation;
            }
        }
        
        if (activeTrackers.TryGetValue(TRACKER_ROLE_RIGHT_KNEE, out var rightKneeTracker))
        {
            if (rightKneeTracker.Device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                rightKneeTracker.Device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                rightKneeOffset = headPosition - position;
                
                Quaternion forwardRotation = Quaternion.LookRotation(forward, Vector3.up);
                rightKneeRotOffset = Quaternion.Inverse(rotation) * forwardRotation;
            }
        }
        
        Logger.LogInfo("FBT trackers calibrated");
    }

    private void Update()
    {
        if (!IsFBTActive)
            return;
            
        UpdateTrackerPositions();
    }

    private void UpdateTrackerPositions()
    {
        Transform head = vrPlayer.PlayerController.gameplayCamera.transform;
        
        // Update waist position and rotation
        if (activeTrackers.TryGetValue(TRACKER_ROLE_WAIST, out var waistTracker))
        {
            if (waistTracker.Device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                waistTracker.Device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                waistTarget.position = position + waistOffset;
                waistTarget.rotation = rotation * waistRotOffset;
            }
        }
        
        // Update left foot position and rotation
        if (activeTrackers.TryGetValue(TRACKER_ROLE_LEFT_FOOT, out var leftFootTracker))
        {
            if (leftFootTracker.Device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                leftFootTracker.Device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                leftFootTarget.position = position + leftFootOffset;
                leftFootTarget.rotation = rotation * leftFootRotOffset;
            }
        }
        
        // Update right foot position and rotation
        if (activeTrackers.TryGetValue(TRACKER_ROLE_RIGHT_FOOT, out var rightFootTracker))
        {
            if (rightFootTracker.Device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                rightFootTracker.Device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                rightFootTarget.position = position + rightFootOffset;
                rightFootTarget.rotation = rotation * rightFootRotOffset;
            }
        }
        
        // Update left knee position and rotation
        if (activeTrackers.TryGetValue(TRACKER_ROLE_LEFT_KNEE, out var leftKneeTracker))
        {
            if (leftKneeTracker.Device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                leftKneeTracker.Device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                leftKneeTarget.position = position + leftKneeOffset;
                leftKneeTarget.rotation = rotation * leftKneeRotOffset;
            }
        }
        else if (activeTrackers.ContainsKey(TRACKER_ROLE_LEFT_FOOT))
        {
            // If we don't have a knee tracker but have a foot tracker, estimate knee position
            leftKneeTarget.position = Vector3.Lerp(waistTarget.position, leftFootTarget.position, 0.5f);
            leftKneeTarget.rotation = Quaternion.Slerp(waistTarget.rotation, leftFootTarget.rotation, 0.5f);
        }
        
        // Update right knee position and rotation
        if (activeTrackers.TryGetValue(TRACKER_ROLE_RIGHT_KNEE, out var rightKneeTracker))
        {
            if (rightKneeTracker.Device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                rightKneeTracker.Device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                rightKneeTarget.position = position + rightKneeOffset;
                rightKneeTarget.rotation = rotation * rightKneeRotOffset;
            }
        }
        else if (activeTrackers.ContainsKey(TRACKER_ROLE_RIGHT_FOOT))
        {
            // If we don't have a knee tracker but have a foot tracker, estimate knee position
            rightKneeTarget.position = Vector3.Lerp(waistTarget.position, rightFootTarget.position, 0.5f);
            rightKneeTarget.rotation = Quaternion.Slerp(waistTarget.rotation, rightFootTarget.rotation, 0.5f);
        }
    }

    // Helper class to store tracker information
    private class TrackerInfo
    {
        public InputDevice Device;
        public string Role;
    }
}