using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

public class CustomLocomotionProvider : LocomotionProvider
{
    private bool isLocomotionActive = false;
    private bool leftControllerMoving = false;
    private bool rightControllerMoving = false;

    public void SetMoving(bool moving, bool isLeftController)
    {
        // Update the appropriate controller state
        if (isLeftController)
            leftControllerMoving = moving;
        else
            rightControllerMoving = moving;

        // Locomotion should be active if EITHER controller is moving
        bool shouldBeActive = leftControllerMoving || rightControllerMoving;

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
}
