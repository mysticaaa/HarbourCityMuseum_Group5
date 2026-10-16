using TMPro;
using UnityEngine;

namespace ZzxWorkspace.FirstDay
{
    // Exhibit state only. No inputs, cameras, locomotion or scene transitions.
    public sealed class FirstDayJourney : MonoBehaviour
    {
        public TMP_Text feedback;
        public TMP_Text progress;
        public GameObject ending;
        public bool IsComplete { get; private set; }
        public int Attempts { get; private set; }
        private int lastFrame = -1;
        private void OnEnable() { ResetJourney(); }
        public void Choose(int berth)
        {
            if (IsComplete || lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            Attempts++;
            if (berth == 2)
            {
                IsComplete = true;
                feedback.text = "BERTH 02 - YOU FOUND HIS FIRST WORKPLACE\nThe letter mentions cargo work; the pass confirms Berth 02.";
                progress.text = "JOURNEY COMPLETE / Read what happened next";
                ending.SetActive(true);
            }
            else
            {
                feedback.text = berth == 1
                    ? "TRY AGAIN / The ferry landing is where he arrived.\nHis work pass identifies a different berth."
                    : "TRY AGAIN / The repair yard services boats.\nLook for the cargo berth number on his work pass.";
                progress.text = "COMPARE THE LETTER + WORK PASS / Then choose again";
            }
        }
        public void ResetJourney()
        {
            IsComplete = false;
            Attempts = 0;
            lastFrame = -1;
            if (ending != null) ending.SetActive(false);
            if (feedback != null) feedback.text = "Where did Liang report for work?\nUse the letter and work pass to choose a berth on the map.";
            if (progress != null) progress.text = "01 READ THE LETTER / 02 CHECK THE PASS / 03 FIND THE BERTH";
        }
    }
}
