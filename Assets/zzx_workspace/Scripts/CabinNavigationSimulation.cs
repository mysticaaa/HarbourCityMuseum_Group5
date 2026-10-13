using UnityEngine;

namespace ZzxWorkspace
{
    // Shared teaching simulation, deliberately independent of the visitor camera.
    public class CabinNavigationSimulation : MonoBehaviour
    {
        public float rudderAngle;
        public float turnRateAtFullRudder = 18f;
        public float fullRudderAngle = 90f;
        public float Heading { get; private set; }
        public int Gear { get; private set; }
        public float Throttle { get; private set; }
        public float Speed { get; private set; }
        public bool requireEngineControl;
        public string Instruction { get; private set; } = "Neutral";
        public bool SetGear(int gear)
        {
            gear = Mathf.Clamp(gear, -1, 1);
            if (gear != 0 && (Throttle > 0.05f || Mathf.Abs(Speed) > 0.1f || (Gear != 0 && gear != Gear)))
            {
                Instruction = "Reduce power, select neutral, wait for speed to fall.";
                return false;
            }
            Gear = gear;
            Instruction = gear == 0 ? "Neutral: propulsion disconnected" : gear > 0 ? "Ahead selected" : "Astern selected";
            return true;
        }
        public void SetThrottle(float value) { Throttle = Gear == 0 ? 0f : Mathf.Clamp01(value); }
        private void Update()
        {
            if (Gear == 0) Throttle = 0;
            Speed = Mathf.MoveTowards(Speed, Gear * Throttle * 5f, Time.deltaTime);
            Heading = Mathf.Repeat(Heading + Mathf.Clamp(rudderAngle / Mathf.Max(1f, fullRudderAngle), -1f, 1f)
                * turnRateAtFullRudder * (requireEngineControl ? Speed / 5f : 1f) * Time.deltaTime, 360f);
        }
        public void ResetNavigation() { Heading = rudderAngle = Speed = Throttle = 0f; Gear = 0; Instruction = "Neutral"; }
        private void OnDisable() { rudderAngle = 0f; }
        private void OnGUI()
        {
            float width = Mathf.Min(220f, Screen.width * 0.24f);
            Rect panel = new Rect(Screen.width - width - 10, 10, width, 300);
            GUI.Box(panel, "Ship heading / Compass");
            GUI.Label(new Rect(panel.x + 10, 35, width - 20, 25), "Heading: " + Heading.ToString("F0") + " degrees");
            GUI.Label(new Rect(panel.x + 10, 60, width - 20, 25), "Rudder: " + rudderAngle.ToString("F0"));
            Vector2 centre = new Vector2(panel.center.x, 130);
            GUI.Label(new Rect(centre.x - 7, 82, 25, 20), "N");
            GUI.Label(new Rect(centre.x + 42, 121, 25, 20), "E");
            GUI.Label(new Rect(centre.x - 7, 167, 25, 20), "S");
            GUI.Label(new Rect(centre.x - 52, 121, 25, 20), "W");
            Matrix4x4 matrix = GUI.matrix;
            Color colour = GUI.color;
            GUIUtility.RotateAroundPivot(Heading, centre);
            GUI.color = Color.cyan;
            GUI.DrawTexture(new Rect(centre.x - 5, centre.y - 25, 10, 48), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centre.x - 12, centre.y - 28, 24, 7), Texture2D.whiteTexture);
            GUI.matrix = matrix;
            GUI.color = colour;
            GUI.Label(new Rect(panel.x + 8, 188, width - 16, 20), "Compass needle (ship-relative)");
            Vector2 needleCentre = new Vector2(panel.center.x, 247);
            GUIUtility.RotateAroundPivot(-Heading, needleCentre);
            GUI.color = Color.red;
            GUI.DrawTexture(new Rect(needleCentre.x - 3, needleCentre.y - 25, 6, 25), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(needleCentre.x - 3, needleCentre.y, 6, 25), Texture2D.whiteTexture);
            GUI.matrix = matrix;
            GUI.color = colour;
            GUI.Label(new Rect(panel.x + 8, 283, width - 16, 20), "Teaching simulation only");
            if (requireEngineControl)
            {
                GUI.Box(new Rect(panel.x, 315, width, 110), "Engine response");
                GUI.Label(new Rect(panel.x + 8, 340, width - 16, 25), "Gear: " + (Gear == 0 ? "Neutral" : Gear > 0 ? "Ahead" : "Astern"));
                GUI.Label(new Rect(panel.x + 8, 365, width - 16, 25), "Power: " + (Throttle * 100).ToString("F0") + "%");
                GUI.Label(new Rect(panel.x + 8, 390, width - 16, 25), "Simulated speed: " + Speed.ToString("F1") + " m/s");
            }
        }
    }
}
