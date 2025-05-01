using UnityEngine;
using UnityEngine.Animations.Rigging;
using LCVR.Player;

namespace LCVR.Networking;

/// <summary>
/// Handles FBT data for network players
/// </summary>
public class FBTNetPlayer : MonoBehaviour
{
    // Reference to the VRNetPlayer component
    private VRNetPlayer vrNetPlayer;
    
    // Transforms for the tracked body parts
    private Transform waistTarget;
    private Transform leftFootTarget;
    private Transform rightFootTarget;
    private Transform leftKneeTarget;
    private Transform rightKneeTarget;
    
    // IK constraints for legs
    private TwoBoneIKConstraint leftLegIK;
    private TwoBoneIKConstraint rightLegIK;
    
    // IK constraint for the waist/spine
    private ChainIKConstraint spineIK;
    
    // Flag to indicate if FBT is active
    private bool isFBTActive;
    
    // Flag to indicate if the rig is initialized
    private bool isInitialized;
    
    private void Awake()
    {
        vrNetPlayer = GetComponent<VRNetPlayer>();
    }
    
    /// <summary>
    /// Updates the FBT data for this network player
    /// </summary>
    public void UpdateFBTData(FBTData.FBTRigData data)
    {
        // Check if FBT status changed
        if (data.isActive != isFBTActive)
        {
            isFBTActive = data.isActive;
            
            if (isFBTActive && !isInitialized)
            {
                InitializeRig();
            }
            
            // Enable/disable the IK constraints based on FBT status
            if (leftLegIK != null) leftLegIK.weight = isFBTActive ? 1f : 0f;
            if (rightLegIK != null) rightLegIK.weight = isFBTActive ? 1f : 0f;
            if (spineIK != null) spineIK.weight = isFBTActive ? 1f : 0f;
        }
        
        if (!isFBTActive)
            return;
            
        // Update target positions and rotations
        waistTarget.localPosition = data.waistPosition;
        waistTarget.localEulerAngles = data.waistRotation;
        
        leftFootTarget.localPosition = data.leftFootPosition;
        leftFootTarget.localEulerAngles = data.leftFootRotation;
        
        rightFootTarget.localPosition = data.rightFootPosition;
        rightFootTarget.localEulerAngles = data.rightFootRotation;
        
        leftKneeTarget.localPosition = data.leftKneePosition;
        rightKneeTarget.localPosition = data.rightKneePosition;
    }
    
    private void InitializeRig()
    {
        if (isInitialized)
            return;
            
        // Create targets if they don't exist
        CreateTargets();
        
        // Create IK constraints
        CreateIKConstraints();
        
        isInitialized = true;
        Logger.LogInfo($"FBT rig initialized for network player {vrNetPlayer.name}");
    }
    
    private void CreateTargets()
    {
        // Create waist target
        var waistObj = new GameObject("WaistTarget");
        waistTarget = waistObj.transform;
        waistTarget.parent = transform;
        
        // Create left foot target
        var leftFootObj = new GameObject("LeftFootTarget");
        leftFootTarget = leftFootObj.transform;
        leftFootTarget.parent = transform;
        
        // Create right foot target
        var rightFootObj = new GameObject("RightFootTarget");
        rightFootTarget = rightFootObj.transform;
        rightFootTarget.parent = transform;
        
        // Create left knee target
        var leftKneeObj = new GameObject("LeftKneeTarget");
        leftKneeTarget = leftKneeObj.transform;
        leftKneeTarget.parent = transform;
        
        // Create right knee target
        var rightKneeObj = new GameObject("RightKneeTarget");
        rightKneeTarget = rightKneeObj.transform;
        rightKneeTarget.parent = transform;
    }
    
    private void CreateIKConstraints()
    {
        // Get the player model
        Transform playerModel = transform.Find("PlayerModel");
        
        if (playerModel == null)
        {
            Logger.LogWarning("Could not find player model for FBT rig");
            return;
        }
        
        // Find the metarig
        Transform metarig = playerModel.Find("metarig");
        
        if (metarig == null)
        {
            Logger.LogWarning("Could not find metarig for FBT rig");
            return;
        }
        
        // Find the rig
        Transform rig = metarig.Find("Rig 1");
        
        if (rig == null)
        {
            Logger.LogWarning("Could not find rig for FBT rig");
            return;
        }
        
        // Find the leg bones in the player model
        Transform leftUpperLeg = FindBone(metarig, "spine/spine.001/spine.002/thigh.L");
        Transform leftLowerLeg = FindBone(leftUpperLeg, "shin.L");
        Transform leftFoot = FindBone(leftLowerLeg, "foot.L");
        
        Transform rightUpperLeg = FindBone(metarig, "spine/spine.001/spine.002/thigh.R");
        Transform rightLowerLeg = FindBone(rightUpperLeg, "shin.R");
        Transform rightFoot = FindBone(rightLowerLeg, "foot.R");
        
        // Find the spine bones
        Transform spine = FindBone(metarig, "spine");
        Transform spine1 = FindBone(spine, "spine.001");
        Transform spine2 = FindBone(spine1, "spine.002");
        
        if (leftUpperLeg != null && leftLowerLeg != null && leftFoot != null)
        {
            // Create left leg IK
            var leftLegRig = new GameObject("LeftLegIK");
            leftLegRig.transform.parent = rig;
            
            leftLegIK = leftLegRig.AddComponent<TwoBoneIKConstraint>();
            
            // Set up the IK constraint
            var leftData = new TwoBoneIKConstraintData
            {
                root = leftUpperLeg,
                mid = leftLowerLeg,
                tip = leftFoot,
                target = leftFootTarget,
                hint = leftKneeTarget,
                targetPositionWeight = 1f,
                targetRotationWeight = 1f,
                hintWeight = 1f
            };
            
            leftLegIK.data = leftData;
            leftLegIK.weight = 0f; // Start disabled
        }
        
        if (rightUpperLeg != null && rightLowerLeg != null && rightFoot != null)
        {
            // Create right leg IK
            var rightLegRig = new GameObject("RightLegIK");
            rightLegRig.transform.parent = rig;
            
            rightLegIK = rightLegRig.AddComponent<TwoBoneIKConstraint>();
            
            // Set up the IK constraint
            var rightData = new TwoBoneIKConstraintData
            {
                root = rightUpperLeg,
                mid = rightLowerLeg,
                tip = rightFoot,
                target = rightFootTarget,
                hint = rightKneeTarget,
                targetPositionWeight = 1f,
                targetRotationWeight = 1f,
                hintWeight = 1f
            };
            
            rightLegIK.data = rightData;
            rightLegIK.weight = 0f; // Start disabled
        }
        
        if (spine != null && spine1 != null && spine2 != null)
        {
            // Create spine IK
            var spineRigObj = new GameObject("SpineIK");
            spineRigObj.transform.parent = rig;
            
            spineIK = spineRigObj.AddComponent<ChainIKConstraint>();
            
            // Set up the chain IK constraint
            var spineData = new ChainIKConstraintData
            {
                root = spine,
                tip = spine2,
                target = waistTarget,
                chainRotationWeight = 1f,
                tipRotationWeight = 1f
            };
            
            spineIK.data = spineData;
            spineIK.weight = 0f; // Start disabled
        }
    }
    
    private Transform FindBone(Transform parent, string path)
    {
        return parent.Find(path);
    }
}