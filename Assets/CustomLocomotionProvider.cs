using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

public class CustomLocomotionProvider : LocomotionProvider
{
    private bool isLocomotionActive = false;
    private bool leftControllerMoving = false;
    private bool rightControllerMoving = false;

    private bool locomotionEnabled = true;

    public void SetMoving(bool moving, bool isLeftController)
    {
        // Update the appropriate controller state
        if (isLeftController)
            leftControllerMoving = moving;
        else
            rightControllerMoving = moving;

        // Locomotion should be active if EITHER controller is moving AND locomotion is enabled
        bool shouldBeActive = (leftControllerMoving || rightControllerMoving) && locomotionEnabled;

        if (shouldBeActive == isLocomotionActive)
            return;

        isLocomotionActive = shouldBeActive;

        if (shouldBeActive)
        {
            // Start locomotion immediately (skips Preparing)
            TryStartLocomotionImmediately();
        }
        else
        {
            TryEndLocomotion();
        }
    }

    public bool IsMoving => isLocomotionActive;

    public void ToggleLocomotionEnabled()
    {
        locomotionEnabled = !locomotionEnabled;
        
        // If we're disabling locomotion while it's active, end it immediately
        if (!locomotionEnabled && isLocomotionActive)
        {
            isLocomotionActive = false;
            TryEndLocomotion();
        }
    }
}
