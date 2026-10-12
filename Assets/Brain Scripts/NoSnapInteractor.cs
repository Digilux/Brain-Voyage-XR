using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Hands;

public class NoSnapInteractor : UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor
{
    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        // Skip setting attach transform
        attachTransform = null;
        base.OnSelectEntering(args);
    }
}
