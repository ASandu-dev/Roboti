using UnityEngine;
using UnityEngine.UI;

public class AgentUI : MonoBehaviour
{
    public Text timeText;
    public Text distanceText;
    public Text tasksText;
    public Text stateText;
    public Text cargoText; // New field for cargo count

    public CargoCubeController cubeController;

    void Update()
    {
        if (cubeController == null)
        {
            Debug.LogWarning("CubeController not assigned to the UI.");
            return;
        }

        // Using public properties from CargoCubeController
        timeText.text = "Time: " + cubeController.timer.ToString("F2") + "s";
        distanceText.text = "Distance: " + cubeController.totalDistance.ToString("F2") + "m";
        tasksText.text = "Tasks This Run: " + cubeController.GetTasksCompleted() + "/9";
        stateText.text = "State: " + cubeController.GetCurrentState();
        
        // Update cargo text
        if (cargoText != null)
        {
            cargoText.text = "Cargo: " + cubeController.cargoCount;
        }
    }
}
