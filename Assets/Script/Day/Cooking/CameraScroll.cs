using UnityEngine;

/// <summary>
/// Attach to Main Camera.
/// Move left/right using:
///   A or Left Arrow  — scroll left
///   D or Right Arrow — scroll right
/// Camera stays within MinX / MaxX bounds.
/// </summary>
public class CameraScroll : MonoBehaviour
{
    [Header("Bounds")]
    public float minX = 0f;
    public float maxX = 20f;

    [Header("Feel")]
    [Tooltip("How fast the camera moves (units per second).")]
    public float moveSpeed = 8f;
    [Tooltip("How quickly the camera glides to a stop after releasing the key.")]
    public float deceleration = 10f;

    private float _velocity = 0f;

    // IsDragging is kept for compatibility with IngredientDragHandler
    // (always false now since we no longer use mouse drag)
    public bool IsDragging => false;

    void Update()
    {
        float input = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            input = -1f;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            input = 1f;

        if (input != 0f)
        {
            // Accelerate toward target velocity
            _velocity = Mathf.MoveTowards(_velocity, input * moveSpeed, deceleration * Time.deltaTime);
        }
        else
        {
            // Decelerate to zero when no key is held
            _velocity = Mathf.MoveTowards(_velocity, 0f, deceleration * Time.deltaTime);
        }

        if (Mathf.Abs(_velocity) > 0.001f)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x + _velocity * Time.deltaTime, minX, maxX);
            transform.position = pos;
        }
    }
}