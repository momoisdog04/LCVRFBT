# Full Body Tracking (FBT) Feature

The Full Body Tracking (FBT) feature allows players to use VR trackers to track their full body movements in Lethal Company VR.

## Requirements

To use FBT, you need:

1. A VR headset compatible with LCVR
2. At least 3 VR trackers:
   - Waist/hip tracker
   - Left foot/ankle tracker
   - Right foot/ankle tracker
3. Optional additional trackers:
   - Left knee tracker
   - Right knee tracker

## Supported Trackers

The FBT feature supports any OpenXR-compatible trackers, including:

- HTC Vive Trackers
- Tundra Trackers
- SlimeVR trackers (with OpenXR driver)
- Other OpenXR-compatible trackers

## Setup

1. Make sure your trackers are properly set up and recognized by your VR runtime (SteamVR, Oculus, etc.)
2. Launch Lethal Company with LCVR mod
3. The mod will automatically detect your trackers and enable FBT if they are found
4. When trackers are detected, you'll see a calibration prompt (if enabled in settings)
5. Stand in a T-pose for calibration

## Configuration

The following configuration options are available in the BepInEx config file:

```
[FBT]
# Enables Full Body Tracking (FBT) when trackers are detected
EnableFBT = true

# Shows a calibration prompt when FBT trackers are detected
ShowFBTCalibrationPrompt = true
```

## Troubleshooting

If your trackers are not being detected:

1. Make sure they are properly connected and recognized by your VR runtime
2. Check that the trackers have appropriate roles assigned (waist/hip, left foot, right foot, etc.)
3. Verify that FBT is enabled in the LCVR config
4. Restart the game after connecting trackers

## Multiplayer Compatibility

The FBT feature is fully compatible with multiplayer. Other players will see your full body movements, including:

- Waist/hip movement
- Leg and foot movements
- Full body posture

This works with both VR and non-VR players in the same lobby.

## Known Issues

- Some trackers may require specific naming conventions to be properly detected
- FBT calibration may need to be performed multiple times for optimal results
- In rare cases, the IK system may produce unnatural poses

## Future Improvements

Planned improvements for the FBT feature include:

- Better calibration system
- Support for more tracker configurations
- Improved IK for more natural movement
- Custom poses and gestures