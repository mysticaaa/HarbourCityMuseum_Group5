using UnityEngine;

namespace ZxhMuseum
{
    // Fixed-cabin teaching model; never moves the XR camera or the ship display model.
    public sealed class VoyageState : MonoBehaviour
    {
        public float Heading { get; private set; }
        public float WheelAngle { get; private set; }
        public float Speed { get; private set; }
        public float Power { get; private set; }
        public int Gear { get; private set; }
        public float Target { get; private set; } = 90;
        public bool TaskRunning { get; private set; }
        public bool Completed { get; private set; }
        public float HoldTime { get; private set; }
        public string Notice { get; private set; } = "Choose a route, then start the teaching engine.";
        public void SetWheel(float angle) => WheelAngle = Mathf.Clamp(angle, -135, 135);
        public bool SetGear(int value)
        {
            value = Mathf.Clamp(value, -1, 1);
            if (value != 0 && (Power > 0.05f || Mathf.Abs(Speed) > 0.1f || (Gear != 0 && Gear != value)))
            { Notice = "Reduce power, select neutral and wait until stopped."; return false; }
            Gear = value;
            if (Gear == 0) Power = 0;
            Notice = Gear == 0 ? "Neutral" : Gear > 0 ? "Ahead" : "Astern";
            return true;
        }
        public void SetPower(float value) { Power = Gear == 0 ? 0 : Mathf.Clamp01(value); }
        public void BeginRoute(float heading)
        {
            Target = Mathf.Repeat(heading, 360); TaskRunning = true; Completed = false; HoldTime = 0;
            Notice = "Reach the target within 10 degrees and hold for 3 seconds.";
        }
        public void CancelTask() { TaskRunning = false; HoldTime = 0; Notice = "Task skipped. Free exploration remains available."; }
        public void SetHeadingForDemonstration(float heading)
        {
            if (TaskRunning) { Notice = "Cancel the route before changing demonstration heading."; return; }
            Heading = Mathf.Repeat(heading, 360);
        }
        private void Update() => Step(Time.deltaTime);
        public void Step(float seconds)
        {
            if (Gear == 0) Power = 0;
            Speed = Mathf.MoveTowards(Speed, Gear * Power * 5, seconds);
            Heading = Mathf.Repeat(Heading + WheelAngle / 135f * 18 * (Speed / 5f) * seconds, 360);
            if (TaskRunning)
            {
                HoldTime = Mathf.Abs(Mathf.DeltaAngle(Heading, Target)) <= 10 ? HoldTime + seconds : 0;
                if (HoldTime >= 3) { Completed = true; TaskRunning = false; Notice = "Route completed! Ring the ship bell to celebrate."; }
            }
        }
        public void ResetState()
        {
            Heading = WheelAngle = Speed = Power = HoldTime = 0; Gear = 0;
            TaskRunning = Completed = false; Notice = "Reset. Choose a route.";
        }
    }
}
