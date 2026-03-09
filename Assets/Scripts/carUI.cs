using UnityEngine;
using TMPro;
public class carUI : MonoBehaviour
{
    public car carController;
    public TextMeshProUGUI rpmText;
    public TextMeshProUGUI gearText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI speedText;


    void Update()
    {
      if (carController != null && carController.e != null)
        {
            // Update RPM
            if (rpmText != null)
            {
                rpmText.text = $"RPM: {carController.e.GetRPM():F0}";
            }

            // Update Gear
            if (gearText != null)
            {
                gearText.text = $"Gear: {carController.e.GetCurrentGear()}";
            }

            // Update Gear
            if (hpText != null)
            {
                hpText.text = $"{carController.GetHP()}";
            }

            if (speedText != null)
            {
                speedText.text = $" {carController.getSpeed():F0} km/h";
            }

        }   
    }
}
