using UnityEngine;
using UnityEngine.UI;

public class AgentUI : MonoBehaviour
{
    public Text timeText;
    public Text distanceText;
    public Text deliveredText;
    public Text stateText;
    public Text cargoText;

    public CargoCubeController cubeController;

    void Update()
    {
        if (cubeController == null)
        {
            Debug.LogWarning("CubeController not assigned to the UI.");
            return;
        }

        timeText.text = "Time: " + cubeController.timer.ToString("F1") + "s";
        distanceText.text = "Distance: " + cubeController.totalDistance.ToString("F1") + "m";
        deliveredText.text = "Delivered: " + cubeController.tasksCompleted;
        stateText.text = "State: " + cubeController.GetCurrentState();
        
        if (cargoText != null)
        {
            cargoText.text = "Cargo Left: " + cubeController.cargoCount;
        }
    }
}
