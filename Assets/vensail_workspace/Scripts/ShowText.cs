using System.Collections;
using UnityEngine;
public class ShowText : MonoBehaviour
{


    private void Awake()
    {
 
        Bounds bounds = ExhibitInspectionHint.GetBounds(transform);

        ExhibitInspectionHint.Create(transform, "Point ray at the ship, press the grab button to stop rotation.\n Point ray at the ship, press trigger to enter the ship.");
    }

}
