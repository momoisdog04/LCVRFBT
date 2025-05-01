using System.IO;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Networking;

/// <summary>
/// Handles networking for Full Body Tracking data
/// </summary>
public class FBTNetworking : MonoBehaviour
{
    // Reference to the FBTTracker component
    private FBTTracker fbtTracker;
    
    // Channel for FBT data
    private Channel fbtChannel;
    
    // Last sent FBT data
    private FBTData.FBTRigData lastSentData;
    
    // Time since last FBT data send
    private float timeSinceLastSend;
    
    // Send rate in seconds
    private const float SEND_RATE = 0.05f; // 20 times per second
    
    private void Awake()
    {
        fbtTracker = GetComponent<FBTTracker>();
    }
    
    private void Start()
    {
        // Create the FBT channel
        fbtChannel = NetworkSystem.Instance.OpenChannel(ChannelType.FBTRig, null);
        
        // Subscribe to FBT status changes
        fbtTracker.OnFBTStatusChanged += OnFBTStatusChanged;
        
        // Subscribe to packet received events
        fbtChannel.OnPacketReceived += OnFBTPacketReceived;
    }
    
    private void OnFBTStatusChanged(bool isActive)
    {
        // Send initial FBT status
        SendFBTData();
    }
    
    private void Update()
    {
        if (!fbtTracker.IsFBTActive)
            return;
            
        // Send FBT data at the specified rate
        timeSinceLastSend += Time.deltaTime;
        
        if (timeSinceLastSend >= SEND_RATE)
        {
            SendFBTData();
            timeSinceLastSend = 0f;
        }
    }
    
    private void SendFBTData()
    {
        // Create FBT data
        var data = new FBTData.FBTRigData
        {
            isActive = fbtTracker.IsFBTActive
        };
        
        if (fbtTracker.IsFBTActive)
        {
            // Populate with tracker data
            data.waistPosition = fbtTracker.waistTarget.localPosition;
            data.waistRotation = fbtTracker.waistTarget.localEulerAngles;
            
            data.leftFootPosition = fbtTracker.leftFootTarget.localPosition;
            data.leftFootRotation = fbtTracker.leftFootTarget.localEulerAngles;
            
            data.rightFootPosition = fbtTracker.rightFootTarget.localPosition;
            data.rightFootRotation = fbtTracker.rightFootTarget.localEulerAngles;
            
            data.leftKneePosition = fbtTracker.leftKneeTarget.localPosition;
            data.rightKneePosition = fbtTracker.rightKneeTarget.localPosition;
        }
        
        // Check if data has changed significantly
        if (!HasDataChangedSignificantly(data, lastSentData))
            return;
            
        // Serialize and send the data
        using var memStream = new MemoryStream();
        using var writer = new BinaryWriter(memStream);
        
        data.Serialize(writer);
        
        fbtChannel.SendPacket(memStream.ToArray());
        
        // Update last sent data
        lastSentData = data;
    }
    
    private bool HasDataChangedSignificantly(FBTData.FBTRigData newData, FBTData.FBTRigData oldData)
    {
        // If FBT status changed, data has changed significantly
        if (newData.isActive != oldData.isActive)
            return true;
            
        if (!newData.isActive)
            return false;
            
        // Check if positions have changed significantly
        const float positionThreshold = 0.01f; // 1cm
        const float rotationThreshold = 1.0f; // 1 degree
        
        return Vector3.SqrMagnitude(newData.waistPosition - oldData.waistPosition) > positionThreshold ||
               Vector3.SqrMagnitude(newData.waistRotation - oldData.waistRotation) > rotationThreshold ||
               Vector3.SqrMagnitude(newData.leftFootPosition - oldData.leftFootPosition) > positionThreshold ||
               Vector3.SqrMagnitude(newData.leftFootRotation - oldData.leftFootRotation) > rotationThreshold ||
               Vector3.SqrMagnitude(newData.rightFootPosition - oldData.rightFootPosition) > positionThreshold ||
               Vector3.SqrMagnitude(newData.rightFootRotation - oldData.rightFootRotation) > rotationThreshold ||
               Vector3.SqrMagnitude(newData.leftKneePosition - oldData.leftKneePosition) > positionThreshold ||
               Vector3.SqrMagnitude(newData.rightKneePosition - oldData.rightKneePosition) > positionThreshold;
    }
    
    private void OnFBTPacketReceived(ushort sender, BinaryReader reader)
    {
        // Deserialize the FBT data
        var data = FBTData.FBTRigData.Deserialize(reader);
        
        // Find the VRNetPlayer for this sender
        var netPlayer = NetworkSystem.Instance.GetNetPlayer(sender);
        
        if (netPlayer == null)
            return;
            
        // Apply the FBT data to the net player
        var fbtNetPlayer = netPlayer.gameObject.GetComponent<FBTNetPlayer>();
        
        if (fbtNetPlayer == null)
        {
            // Add the component if it doesn't exist
            fbtNetPlayer = netPlayer.gameObject.AddComponent<FBTNetPlayer>();
        }
        
        // Update the FBT data
        fbtNetPlayer.UpdateFBTData(data);
    }
    
    private void OnDestroy()
    {
        // Clean up the channel
        fbtChannel?.Dispose();
    }
}