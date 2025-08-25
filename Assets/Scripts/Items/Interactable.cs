// Interactable.cs
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [Tooltip("Shown in UI when you look at this object")]
    public string prompt = "Interact";
    public abstract void Interact(GameObject interactor);
}
