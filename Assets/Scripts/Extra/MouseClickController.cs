using UnityEngine;
using UnityEngine.Events;

public class MouseClickController : MonoBehaviour
{
    public Vector3 clickPosition;
    public UnityEvent<Vector3> OnClick = new UnityEvent<Vector3>();
    
    private Ray lastValidRay;
    private bool hasValidClick = false;
    
    void Update() { 
        // Get the mouse click position in world space 
        if (Input.GetMouseButtonDown(0)) { 
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition); 
            if (Physics.Raycast(mouseRay, out RaycastHit hitInfo)) { 
                clickPosition = hitInfo.point;
                lastValidRay = mouseRay;
                hasValidClick = true;
                
                // Invoke the OnClick event with the click position
                OnClick.Invoke(clickPosition);
            } 
        } 
        
        // Visual debugging
        if (hasValidClick)
        {
            // Draw the last valid ray
            Debug.DrawRay(lastValidRay.origin, lastValidRay.direction * 100f, Color.red);
            
            // Draw a wire sphere at the click position
            DebugExtension.DebugWireSphere(clickPosition, Color.green, 0.5f);
        }
    }
}
