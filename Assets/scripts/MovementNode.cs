using UnityEngine;

/// <summary>
/// Marks a point the player can move to. Place on a GameObject with a Collider.
/// When the player looks at this and presses Interact, they move here.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MovementNode : MonoBehaviour
{
    [Header("Optional")]
    [Tooltip("Override position to move to (e.g. slightly in front of node). Leave empty to use transform.position")]
    [SerializeField] private Transform targetPoint;

    /// <summary>
    /// World position the player will move to.
    /// </summary>
    public Vector3 MoveTarget => targetPoint != null ? targetPoint.position : transform.position;

}
