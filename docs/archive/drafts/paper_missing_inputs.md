# Paper Inputs Still Needed

This repository is enough to write a system paper draft, but not enough to claim experimental results yet. Fill in the items below before converting the draft into a submission-ready paper.

## 1. Paper Metadata

- Final paper title.
- Author names.
- Affiliations.
- Target conference or journal.
- Required format: IEEE, ACM, Springer, thesis chapter, or internal report.

## 2. Research Positioning

- 5 to 10 related papers on:
  - XR adaptation
  - gaze-based interaction or attention modeling
  - multimodal context inference
  - immersive training systems
- Final novelty statement relative to those papers.

## 3. Experimental Details

- Was a user study already run?
- If yes, how many participants?
- Participant profile: age range, XR experience, recruitment source.
- Hardware used: exact Meta Quest model, controller/hand-tracking mode, desktop specs if relevant.
- Session length per participant.
- Number of trials per participant.
- Whether the adaptive condition was compared against a non-adaptive baseline.

## 4. Ground Truth and Metrics

- How will "Engaged", "Distracted", "Transitioning", and "Idle" be labeled for validation?
- Will labels come from:
  - manual video annotation
  - task events
  - self-report
  - expert coding
- Metrics to report:
  - accuracy
  - precision / recall / F1
  - confusion matrix
  - state-transition stability
  - task completion time
  - placement errors
  - subjective workload or usability

## 5. Results Needed

- Quantitative results table.
- Statistical significance test if there is a comparison study.
- At least one figure showing state behavior over time.
- At least one example screenshot from the XR scene.

## 6. Claims To Avoid Until Evidence Exists

- "Improves learning outcomes."
- "Outperforms existing methods."
- "Generalizes across XR tasks."
- "Achieves robust accuracy."

Use safer wording for now:

- "demonstrates"
- "supports"
- "enables"
- "provides a prototype for"
- "establishes a basis for evaluation"

## 7. Practical Next Step

If you want the fastest path to a submission-ready paper, do this next:

1. Decide the target venue and format.
2. Collect or organize any existing session logs and screenshots.
3. Add related work references.
4. Run at least a small pilot evaluation.
5. Replace the placeholder evaluation section in `docs/archive/drafts/paper_draft.md` with real results if this archived draft is reused.
