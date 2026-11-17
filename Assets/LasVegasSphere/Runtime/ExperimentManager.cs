using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs debug demonstrations and a blind participant sequence. Rating and CSV
/// collection intentionally remain outside this first prototype milestone.
/// </summary>
public sealed class ExperimentManager : MonoBehaviour
{
    [SerializeField] private DspEventScheduler scheduler;
    [SerializeField] private SeatPositionManager seatManager;
    [SerializeField] private ExperimentScenarioBase[] scenarios;
    [SerializeField] private ExperimentRunMode runMode = ExperimentRunMode.Debug;
    [SerializeField, Range(0, 4)] private int debugOffsetIndex = 2;
    [SerializeField] private SeatZone debugSeat = SeatZone.MiddleCenter;
    [SerializeField] private ExperimentSceneType debugScene = ExperimentSceneType.Clap;

    private readonly List<TrialCondition> participantTrials = new List<TrialCondition>();
    private ExperimentScenarioBase activeScenario;
    private int participantTrialIndex = -1;
    private bool participantStarted;
    private bool trialInProgress;
    private double trialReadyDspTime;
    private string status = "Debug mode ready";

    public void Configure(DspEventScheduler eventScheduler, SeatPositionManager positions,
        ExperimentScenarioBase[] availableScenarios)
    {
        scheduler = eventScheduler;
        seatManager = positions;
        scenarios = availableScenarios;
    }

    private void Start()
    {
        BuildParticipantTrials();
        SelectCondition(debugScene, debugSeat);
    }

    private void Update()
    {
        if (runMode == ExperimentRunMode.Debug) UpdateDebugControls();
        else UpdateParticipantControls();

        if (trialInProgress && AudioSettings.dspTime >= trialReadyDspTime)
        {
            trialInProgress = false;
            status = runMode == ExperimentRunMode.Participant
                ? "Trial complete. Press SPACE for the next trial."
                : "Ready";
        }
    }

    private void UpdateDebugControls()
    {
        if (Input.GetKeyDown(KeyCode.Q)) { debugScene = ExperimentSceneType.Clap; RefreshDebugCondition(); }
        if (Input.GetKeyDown(KeyCode.W)) { debugScene = ExperimentSceneType.Explosion; RefreshDebugCondition(); }
        if (Input.GetKeyDown(KeyCode.E)) { debugScene = ExperimentSceneType.Aircraft; RefreshDebugCondition(); }

        for (int i = 0; i < DspEventScheduler.OffsetsMilliseconds.Length; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) debugOffsetIndex = i;

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            debugSeat = (SeatZone)Mathf.Max(0, (int)debugSeat - 1);
            RefreshDebugCondition();
        }
        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            debugSeat = (SeatZone)Mathf.Min(8, (int)debugSeat + 1);
            RefreshDebugCondition();
        }
        if (Input.GetKeyDown(KeyCode.Space) && !trialInProgress)
            RunTrial(debugOffsetIndex, false);
        if (Input.GetKeyDown(KeyCode.P)) EnterParticipantMode();
    }

    private void UpdateParticipantControls()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) ExitParticipantMode();
        if (!participantStarted && Input.GetKeyDown(KeyCode.Return)) StartParticipantSequence();
        else if (participantStarted && !trialInProgress && Input.GetKeyDown(KeyCode.Space)) RunNextParticipantTrial();
    }

    private void RefreshDebugCondition()
    {
        SelectCondition(debugScene, debugSeat);
        status = "Debug condition ready";
    }

    private void SelectCondition(ExperimentSceneType scene, SeatZone seat)
    {
        if (scenarios == null) return;
        for (int i = 0; i < scenarios.Length; i++)
        {
            bool selected = scenarios[i] != null && scenarios[i].SceneType == scene;
            if (scenarios[i] != null) scenarios[i].gameObject.SetActive(selected);
            if (selected) activeScenario = scenarios[i];
        }
        if (activeScenario != null && seatManager != null)
            seatManager.Apply(seat);
    }

    private void RunTrial(int offsetIndex, bool blind)
    {
        if (scheduler == null || activeScenario == null) return;
        scheduler.Schedule(activeScenario, offsetIndex);
        trialInProgress = true;
        trialReadyDspTime = scheduler.LastVisualDspTime + 2.2;
        status = blind ? "Trial playing..." :
            "Playing " + activeScenario.SceneType + " / " + seatManager.CurrentZone + " / " +
            DspEventScheduler.OffsetsMilliseconds[offsetIndex].ToString("+0;-0;0") + " ms";
    }

    private void EnterParticipantMode()
    {
        runMode = ExperimentRunMode.Participant;
        participantStarted = false;
        participantTrialIndex = -1;
        trialInProgress = false;
        status = "Participant mode. Press ENTER to begin.";
    }

    private void ExitParticipantMode()
    {
        runMode = ExperimentRunMode.Debug;
        participantStarted = false;
        trialInProgress = false;
        RefreshDebugCondition();
    }

    private void StartParticipantSequence()
    {
        BuildParticipantTrials();
        participantStarted = true;
        participantTrialIndex = -1;
        RunNextParticipantTrial();
    }

    private void RunNextParticipantTrial()
    {
        participantTrialIndex++;
        if (participantTrialIndex >= participantTrials.Count)
        {
            participantStarted = false;
            status = "Participant sequence complete. Press ESC for debug mode.";
            return;
        }

        TrialCondition condition = participantTrials[participantTrialIndex];
        SelectCondition(condition.scene, condition.seat);
        RunTrial(condition.offsetIndex, true);
    }

    private void BuildParticipantTrials()
    {
        participantTrials.Clear();
        SeatZone[] representativeSeats =
        {
            SeatZone.FrontCenter,
            SeatZone.MiddleCenter,
            SeatZone.RearRight
        };
        foreach (ExperimentSceneType scene in Enum.GetValues(typeof(ExperimentSceneType)))
        foreach (SeatZone seat in representativeSeats)
        for (int offset = 0; offset < DspEventScheduler.OffsetsMilliseconds.Length; offset++)
            participantTrials.Add(new TrialCondition(scene, seat, offset));

        // Deterministic shuffle makes the prototype reproducible while hiding
        // the offset order from participants.
        var random = new System.Random(20261117);
        for (int i = participantTrials.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            TrialCondition temp = participantTrials[i];
            participantTrials[i] = participantTrials[j];
            participantTrials[j] = temp;
        }
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(20, 20, 510, runMode == ExperimentRunMode.Debug ? 210 : 165),
            "LAS VEGAS SPHERE · AUDIO/VISUAL PROTOTYPE");
        GUI.Label(new Rect(38, 50, 470, 24), "Mode: " + runMode);
        GUI.Label(new Rect(38, 76, 470, 24), status);

        if (runMode == ExperimentRunMode.Debug)
        {
            GUI.Label(new Rect(38, 105, 470, 24), "Q/W/E: Clap / Explosion / Aircraft");
            GUI.Label(new Rect(38, 130, 470, 24), "1–5: offset    [ / ]: move through 9 seats    SPACE: play");
            GUI.Label(new Rect(38, 155, 470, 24), "P: participant mode");
            GUI.Label(new Rect(38, 180, 470, 24),
                "Current: " + debugScene + " · " + debugSeat + " · " +
                DspEventScheduler.OffsetsMilliseconds[debugOffsetIndex].ToString("+0;-0;0") + " ms");
        }
        else
        {
            string progress = participantStarted
                ? "Trial " + (participantTrialIndex + 1) + " / " + participantTrials.Count
                : "Offsets are hidden in participant mode";
            GUI.Label(new Rect(38, 105, 470, 24), progress);
            GUI.Label(new Rect(38, 130, 470, 24), "ENTER: start    SPACE: next trial    ESC: leave participant mode");
        }
    }
}
