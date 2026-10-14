# Shawn's Voyage XR

A mixed reality brain anatomy game for Meta Quest. Players are guided through a series of goals, each asking them to find and touch a specific region of a 3D brain model, using hand tracking or Touch controllers.

Built in Unity 6 with the XR Interaction Toolkit and Universal Render Pipeline with a three and 1/2 year development period starting in 2023. Beta under the project name, 2025_Brain_X.

![Status: Beta](https://img.shields.io/badge/status-beta-orange)
---

## How it plays

1. **Start** from the menu with *Intro* (plays an intro video first) or *Skip* (spawns the brain in front of you).
2. **Pick a difficulty**: Easy, Medium, or Hard.
3. **Follow each goal.** A voiceover and on-screen instruction name a brain region to find.
4. **Hold a fingertip on the correct region.** The region fills with color as you hover, and the goal completes after about 1.5 seconds.
5. **Avoid wrong regions.** Touching the wrong structure flashes it red, uses up an attempt, and costs points.
6. **Finish the sequence** to see your total time and score, enter your initials, and view the leaderboard for that difficulty.

### Difficulty

| Mode   | Attempts per goal | Penalty per wrong touch |
|--------|-------------------|-------------------------|
| Easy   | 6                 | −5                      |
| Medium | 3                 | −10                     |
| Hard   | 1                 | −30                     |

### Scoring

Each completed goal awards base points, plus an optional time bonus for finishing quickly. Wrong touches subtract points live during the goal. Scores are committed to the running total when each goal completes.

High scores are saved locally on the headset (top 10, filterable by difficulty).

### Wrist menu

- **Reset**: returns all brain slices to their starting positions
- **Move brain**: toggles a handle for repositioning the whole brain
- **Menu**: reloads the main menu
- **Quit**: exits the app

---

## Requirements

- **Unity 6** with **Android Build Support** (including OpenJDK and Android SDK/NDK tools). See `ProjectSettings/ProjectVersion.txt` for the exact editor version.
- **Git LFS**, required to download models, textures, audio, and video correctly
- A **Meta Quest** headset with Developer Mode enabled

Key packages (full list in `Packages/manifest.json`):
- XR Interaction Toolkit
- XR Hands
- OpenXR / Meta Quest support
- AR Foundation
- Input System
- TextMeshPro
- Universal Render Pipeline

---

## Getting started

### 1. Clone with LFS

Install [Git LFS](https://git-lfs.com) **before** cloning. Otherwise model and image files will download as small text pointers and appear broken in Unity.

```bash
git lfs install
git clone <repository-url>
```

With GitHub Desktop, LFS is handled automatically. Choose *Initialize Git LFS* if prompted.

### 2. Open in Unity

1. In Unity Hub, click **Add → Add project from disk** and select the cloned folder.
2. Open it with the matching Unity 6 version.
3. The first open takes a while because Unity rebuilds the `Library` folder.

### 3. Build to Quest

1. Connect the headset over USB and allow USB debugging in the headset.
2. Go to **File → Build Profiles**, select **Android**, and click **Switch Platform** if needed.
3. Make sure the main scene is in the scene list.
4. Click **Build and Run**.

---

## Project structure

Main gameplay scripts:

| Script | Role |
|--------|------|
| `GoalSequenceController` | Runs the sequence of goals, sets difficulty, handles end-of-game score and leaderboard flow |
| `StepController` | Runs a single goal: instruction VO, timer, attempts, hover-to-complete, wrong-touch handling |
| `FingerContactService` | Detects which tagged brain regions each index fingertip is touching |
| `TwoFingerWrongTouchDetector` | Detects contact with incorrect regions |
| `GoalObjectColorFeedback` | Hover fill, correct and incorrect color feedback on brain regions |
| `GoalSequencePlayer` | Plays the image sequence and audio celebration after a goal |
| `ScoreManager` | Tracks per-goal and total score |
| `HighScoreManager` | Saves and loads the leaderboard |
| `WristButtonManager` | Wrist menu actions |
| `ResetButton` / `BackUpResettable` | Resets brain slices to their starting transforms |
| `PinchRotateInteractable` / `PinchTranslateInteractable` | Custom XRI grab behavior for moving and rotating brain pieces |
| `EventSystem` | Main menu, intro video, and brain spawning |

UI scripts are prefixed with `UI_` (timer, attempt counter, score display, leaderboard, initials input, VR keyboard).

`VRDebugText` provides an in-headset debug log, which is useful when testing on device without a connected editor.

---

## Notes for contributors

- **Scenes and prefabs don't merge well.** Coordinate who is editing a scene before committing changes to it.
- **Don't commit generated folders** (`Library/`, `Temp/`, `Logs/`, `.utmp/`, build outputs). The `.gitignore` covers these.
- **New large binary types** (a new model or media format) should be added to `.gitattributes` so they're stored with LFS.

---

## Credits

Developed by Professor Alex M. Lee at Arizona State University and Dr. Lauren Petley at Clarkson University.

Secondary developers include: Bo Li, Shreyak Duggirala, Hemanthsai Konatham, Jack Rich.

More information can be found on our website: https://webspace.clarkson.edu/projects/HAL/public_html/BVXR_index.html


## License

MIT License


