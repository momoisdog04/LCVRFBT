using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LCVR.Player;

/// <summary>
/// Handles the IK rigging for full body tracking
/// </summary>
public class FBTRig : MonoBehaviour
{
    // Reference to the FBTTracker component
    private FBTTracker fbtTracker;
    
    // Reference to the VRPlayer component
    private VRPlayer vrPlayer;
    
    // Reference to the player's bones
    private Bones bones;
    
    // IK constraints for legs
    private TwoBoneIKConstraint leftLegIK;
    private TwoBoneIKConstraint rightLegIK;
    
    // IK constraint for the waist/spine
    private ChainIKConstraint spineIK;
    
    // Targets for the IK constraints
    private Transform leftFootIKTarget;
    private Transform rightFootIKTarget;
    private Transform leftKneeIKHint;
    private Transform rightKneeIKHint;
    private Transform spineIKTarget;
    
    // Flag to indicate if the rig is initialized
    private bool isInitialized = false;
    
    private void Awake()
    {
        fbtTracker = GetComponent<FBTTracker>();
        vrPlayer = GetComponent<VRPlayer>();
    }
    
    private void Start()
    {
        // Subscribe to FBT status changes
        fbtTracker.OnFBTStatusChanged += OnFBTStatusChanged;
    }
    
    private void OnFBTStatusChanged(bool isActive)
    {
        if (isActive && !isInitialized)
        {
            InitializeRig();
        }
        
        // Enable/disable the IK constraints based on FBT status
        if (leftLegIK != null) leftLegIK.weight = isActive ? 1f : 0f;
        if (rightLegIK != null) rightLegIK.weight = isActive ? 1f : 0f;
        if (spineIK != null) spineIK.weight = isActive ? 1f : 0f;
    }
    
    private void InitializeRig()
    {
        if (isInitialized)
            return;
            
        bones = vrPlayer.Bones;
        
        // Create IK targets if they don't exist
        CreateIKTargets();
        
        // Create IK constraints
        CreateIKConstraints();
        
        isInitialized = true;
        Logger.LogInfo("FBT rig initialized");
    }
    
    private void CreateIKTargets()
    {
        // Create left foot IK target
        var leftFootObj = new GameObject("LeftFootIKTarget");
        leftFootIKTarget = leftFootObj.transform;
        leftFootIKTarget.parent = transform;
        
        // Create right foot IK target
        var rightFootObj = new GameObject("RightFootIKTarget");
        rightFootIKTarget = rightFootObj.transform;
        rightFootIKTarget.parent = transform;
        
        // Create left knee hint
        var leftKneeObj = new GameObject("LeftKneeIKHint");
        leftKneeIKHint = leftKneeObj.transform;
        leftKneeIKHint.parent = transform;
        
        // Create right knee hint
        var rightKneeObj = new GameObject("RightKneeIKHint");
        rightKneeIKHint = rightKneeObj.transform;
        rightKneeIKHint.parent = transform;
        
        // Create spine IK target
        var spineObj = new GameObject("SpineIKTarget");
        spineIKTarget = spineObj.transform;
        spineIKTarget.parent = transform;
    }
    
    private void CreateIKConstraints()
    {
        // Find the leg bones in the player model
        Transform leftUpperLeg = FindBone(bones.Metarig, "spine/spine.001/spine.002/thigh.L");
        Transform leftLowerLeg = FindBone(leftUpperLeg, "shin.L");
        Transform leftFoot = FindBone(leftLowerLeg, "foot.L");
        
        Transform rightUpperLeg = FindBone(bones.Metarig, "spine/spine.001/spine.002/thigh.R");
        Transform rightLowerLeg = FindBone(rightUpperLeg, "shin.R");
        Transform rightFoot = FindBone(rightLowerLeg, "foot.R");
        
        // Find the spine bones
        Transform spine = FindBone(bones.Metarig, "spine");
        Transform spine1 = FindBone(spine, "spine.001");
        Transform spine2 = FindBone(spine1, "spine.002");
        
        if (leftUpperLeg != null && leftLowerLeg != null && leftFoot != null)
        {
            // Create left leg IK
            var leftLegRig = new GameObject("LeftLegIK");
            leftLegRig.transform.parent = bones.Rig;
            
            leftLegIK = leftLegRig.AddComponent<TwoBoneIKConstraint>();
            
            // Set up the IK constraint
            var leftData = new TwoBoneIKConstraintData
            {
                root = leftUpperLeg,
                mid = leftLowerLeg,
                tip = leftFoot,
                target = leftFootIKTarget,
                hint = leftKneeIKHint,
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
            rightLegRig.transform.parent = bones.Rig;
            
            rightLegIK = rightLegRig.AddComponent<TwoBoneIKConstraint>();
            
            // Set up the IK constraint
            var rightData = new TwoBoneIKConstraintData
            {
                root = rightUpperLeg,
                mid = rightLowerLeg,
                tip = rightFoot,
                target = rightFootIKTarget,
                hint = rightKneeIKHint,
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
            spineRigObj.transform.parent = bones.Rig;
            
            spineIK = spineRigObj.AddComponent<ChainIKConstraint>();
            
            // Set up the chain IK constraint
            var spineData = new ChainIKConstraintData
            {
                root = spine,
                tip = spine2,
                target = spineIKTarget,
                chainRotationWeight = 1f,
                tipRotationWeight = 1f
            };
            
            spineIK.data = spineData;
            spineIK.weight = 0f; // Start disabled
        }
    }
    
    private void Update()
    {
        if (!fbtTracker.IsFBTActive || !isInitialized)
            return;
            
        // Update IK targets based on tracker positions
        UpdateIKTargets();
    }
    
    private void UpdateIKTargets()
    {
        // Update foot targets
        leftFootIKTarget.position = fbtTracker.leftFootTarget.position;
        leftFootIKTarget.rotation = fbtTracker.leftFootTarget.rotation;
        
        rightFootIKTarget.position = fbtTracker.rightFootTarget.position;
        rightFootIKTarget.rotation = fbtTracker.rightFootTarget.rotation;
        
        // Update knee hints
        leftKneeIKHint.position = fbtTracker.leftKneeTarget.position;
        rightKneeIKHint.position = fbtTracker.rightKneeTarget.position;
        
        // Update spine target
        spineIKTarget.position = fbtTracker.waistTarget.position;
        spineIKTarget.rotation = fbtTracker.waistTarget.rotation;
    }
    
    private Transform FindBone(Transform parent, string path)
    {
        return parent.Find(path);
    }
}