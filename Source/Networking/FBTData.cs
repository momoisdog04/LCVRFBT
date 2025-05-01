using System.IO;
using UnityEngine;

namespace LCVR.Networking;

/// <summary>
/// Contains data structures for FBT networking
/// </summary>
public static class FBTData
{
    /// <summary>
    /// Represents the full body tracking data for a player
    /// </summary>
    public struct FBTRigData
    {
        public bool isActive;
        public Vector3 waistPosition;
        public Vector3 waistRotation;
        public Vector3 leftFootPosition;
        public Vector3 leftFootRotation;
        public Vector3 rightFootPosition;
        public Vector3 rightFootRotation;
        public Vector3 leftKneePosition;
        public Vector3 rightKneePosition;

        /// <summary>
        /// Serializes the FBT data to a binary writer
        /// </summary>
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(isActive);
            
            if (isActive)
            {
                // Write waist data
                writer.Write(waistPosition.x);
                writer.Write(waistPosition.y);
                writer.Write(waistPosition.z);
                writer.Write(waistRotation.x);
                writer.Write(waistRotation.y);
                writer.Write(waistRotation.z);
                
                // Write left foot data
                writer.Write(leftFootPosition.x);
                writer.Write(leftFootPosition.y);
                writer.Write(leftFootPosition.z);
                writer.Write(leftFootRotation.x);
                writer.Write(leftFootRotation.y);
                writer.Write(leftFootRotation.z);
                
                // Write right foot data
                writer.Write(rightFootPosition.x);
                writer.Write(rightFootPosition.y);
                writer.Write(rightFootPosition.z);
                writer.Write(rightFootRotation.x);
                writer.Write(rightFootRotation.y);
                writer.Write(rightFootRotation.z);
                
                // Write knee positions
                writer.Write(leftKneePosition.x);
                writer.Write(leftKneePosition.y);
                writer.Write(leftKneePosition.z);
                
                writer.Write(rightKneePosition.x);
                writer.Write(rightKneePosition.y);
                writer.Write(rightKneePosition.z);
            }
        }

        /// <summary>
        /// Deserializes the FBT data from a binary reader
        /// </summary>
        public static FBTRigData Deserialize(BinaryReader reader)
        {
            var data = new FBTRigData
            {
                isActive = reader.ReadBoolean()
            };
            
            if (data.isActive)
            {
                // Read waist data
                data.waistPosition.x = reader.ReadSingle();
                data.waistPosition.y = reader.ReadSingle();
                data.waistPosition.z = reader.ReadSingle();
                data.waistRotation.x = reader.ReadSingle();
                data.waistRotation.y = reader.ReadSingle();
                data.waistRotation.z = reader.ReadSingle();
                
                // Read left foot data
                data.leftFootPosition.x = reader.ReadSingle();
                data.leftFootPosition.y = reader.ReadSingle();
                data.leftFootPosition.z = reader.ReadSingle();
                data.leftFootRotation.x = reader.ReadSingle();
                data.leftFootRotation.y = reader.ReadSingle();
                data.leftFootRotation.z = reader.ReadSingle();
                
                // Read right foot data
                data.rightFootPosition.x = reader.ReadSingle();
                data.rightFootPosition.y = reader.ReadSingle();
                data.rightFootPosition.z = reader.ReadSingle();
                data.rightFootRotation.x = reader.ReadSingle();
                data.rightFootRotation.y = reader.ReadSingle();
                data.rightFootRotation.z = reader.ReadSingle();
                
                // Read knee positions
                data.leftKneePosition.x = reader.ReadSingle();
                data.leftKneePosition.y = reader.ReadSingle();
                data.leftKneePosition.z = reader.ReadSingle();
                
                data.rightKneePosition.x = reader.ReadSingle();
                data.rightKneePosition.y = reader.ReadSingle();
                data.rightKneePosition.z = reader.ReadSingle();
            }
            
            return data;
        }
    }
}