using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class EsportUITutorial : MonoBehaviour
{
    public enum InputMode
    {
        BlockAll,          // hammasini bloklaydi, faqat Next/Skip ishlaydi
        AllowTargetOnly    // target bosiladi (qolgan joy blok)
    }
    public enum HandAnimType
    {
        Tap,
        DragUp,
        DragDown,
        DragLeft,
        DragRight
    }
    [Serializable]
    public class Step
    {
        public HandAnimType handAnimType = HandAnimType.Tap;
        public float dragAmount = 80f;
        public RectTransform target;     // highlight qilinadigan UI
        public int titleId;
        public int descriptionId;

        public bool requireTargetClick;  // true bo‘lsa: faqat target bosilganda next
        public Vector2 tooltipOffset = new Vector2(0, -180); // targetdan qayerda turadi
        public Vector2 holePadding = new Vector2(28, 22);    // highlight atrofida padding
        public bool showHandPointer = true;
    }

    [Header("Steps")]
    public List<Step> steps = new List<Step>();
    public InputMode inputMode = InputMode.BlockAll;
    [SerializeField] private RectTransform canvasRect;

    [Header("Mask Panels (4-panel hole)")]
    public RectTransform maskTop;
    public RectTransform maskBottom;
    public RectTransform maskLeft;
    public RectTransform maskRight;

    [Header("Visuals")]
    public Image overlay;                 // full-screen dark
    public RectTransform highlightBorder; // neon border
    public CanvasGroup tooltipGroup;      // tooltip panel cg
    public RectTransform tooltipPanel;    // tooltip root
    public TMP_Text titleText;
    public TMP_Text descText;
    [SerializeField] private float tooltipCanvasMargin = 24f;
    [SerializeField] private float tooltipTargetGap = 36f;

    [Header("Buttons")]
    public Button btnNext;
    public Button btnSkip;

    [Header("Optional FX")]
    public RectTransform sweepGlow;       // light sweep image
    public RectTransform handPointer;     // hand pointer
    public float animDuration = 0.35f;

    [Header("Click Blocker (AllowTargetOnly mode)")]
    public RectTransform passThroughArea; // target click area (Image with raycast) optional
    public Image passThroughImage;        // to enable/disable raycast

    int _index = -1;
    Tween _pulseTween;
    Tween _sweepTween;
    Tween _handTween;
    Sequence _showSeq;

    bool _running;
    Action _onFinished;

    void Awake()
    {
        if (btnNext) btnNext.onClick.AddListener(Next);
        if (btnSkip) btnSkip.onClick.AddListener(Skip);

        gameObject.SetActive(false);
    }

    public void StartTutorial(Action onFinished = null)
    {
        if (steps == null || steps.Count == 0) return;

        _onFinished = onFinished;
        _running = true;
        _index = -1;

        gameObject.SetActive(true);

        // UI initial state
        if (overlay) overlay.color = new Color(0, 0, 0, 0);
        if (tooltipGroup)
        {
            tooltipGroup.alpha = 0;
            tooltipGroup.blocksRaycasts = true;
            tooltipGroup.interactable = true;
        }

        if (highlightBorder) highlightBorder.gameObject.SetActive(true);
        if (sweepGlow) sweepGlow.gameObject.SetActive(true);
        if (handPointer) handPointer.gameObject.SetActive(true);

        // default pass-through off
        SetPassThrough(false);

        Next(); // show first
    }

    public void Skip()
    {
        Finish();
    }

    public void Next()
    {
        if (!_running) return;

        _index++;
        if (_index >= steps.Count)
        {
            Finish();
            return;
        }

        ShowStepInternal(steps[_index]);
    }

    public void Finish()
    {
        _running = false;
        UnhookTargetClick(null);
        SetPassThrough(false);
        KillTweens();

        // hide with small fade
        var seq = DOTween.Sequence().SetUpdate(true);
        if (overlay) seq.Join(overlay.DOFade(0f, 0.2f).SetUpdate(true));
        if (tooltipGroup) seq.Join(tooltipGroup.DOFade(0f, 0.2f).SetUpdate(true));

        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
            _onFinished?.Invoke();
        });
    }

    //void ShowStep(Step step)
    //{
    //    if (step == null || step.target == null) { Next(); return; }

    //    KillTweens();

    //    // texts
    //    if (titleText) titleText.text = step.title;
    //    if (descText) descText.text = step.description;

    //    // Next button logic
    //    if (btnNext)
    //        btnNext.gameObject.SetActive(!step.requireTargetClick);

    //    // Input mode
    //    if (inputMode == InputMode.AllowTargetOnly || step.requireTargetClick)
    //    {
    //        // faqat target bosilsin
    //        SetPassThrough(true);
    //        FitPassThroughToTarget(step.target, step.holePadding);
    //    }
    //    else
    //    {
    //        // hammasi blok
    //        SetPassThrough(false);
    //    }

    //    // Position mask hole + highlight
    //    FitHoleToTarget(step.target, step.holePadding);
    //    FitHighlightToTarget(step.target, step.holePadding);

    //    // Tooltip position
    //    if (tooltipPanel)
    //    {
    //        tooltipPanel.position = step.target.position;
    //        tooltipPanel.anchoredPosition += step.tooltipOffset;
    //    }

    //    // Main show anim
    //    _showSeq = DOTween.Sequence().SetUpdate(true);

    //    if (overlay)
    //        _showSeq.Join(overlay.DOFade(0.75f, animDuration).SetEase(Ease.OutQuad).SetUpdate(true));

    //    if (tooltipGroup)
    //        _showSeq.Join(tooltipGroup.DOFade(1f, animDuration).SetEase(Ease.OutQuad).SetUpdate(true));

    //    if (highlightBorder)
    //    {
    //        highlightBorder.localScale = Vector3.one * 0.92f;
    //        _showSeq.Join(highlightBorder.DOScale(1f, animDuration).SetEase(Ease.OutBack).SetUpdate(true));
    //    }

    //    // Esport pulse (neon border)
    //    StartPulse();

    //    // Sweep glow
    //    StartSweep();

    //    // Hand pointer
    //    if (handPointer && step.showHandPointer)
    //        StartHand(step.target, step.holePadding);
    //    else if (handPointer)
    //        handPointer.gameObject.SetActive(false);

    //    // If requireTargetClick => hook target button
    //    if (step.requireTargetClick)
    //        HookTargetClick(step.target);
    //    else
    //        UnhookTargetClick(step.target);
    //}
    public void ShowStep(int index)
    {
        if (steps == null || steps.Count == 0) return;
        if (index < 0 || index >= steps.Count) return;

        _running = true;
        _index = index;

        gameObject.SetActive(true);
        ShowStepInternal(steps[index]);
    }
    void ShowStepInternal(Step step)
    {
        if (step == null || step.target == null) { return; }

        KillTweens();

        if (titleText) titleText.text = LanguageManager.Instance.GetText(step.titleId);
        if (descText) descText.text = LanguageManager.Instance.GetText(step.descriptionId);

        if (btnNext)
            btnNext.gameObject.SetActive(!step.requireTargetClick);

        if (inputMode == InputMode.AllowTargetOnly || step.requireTargetClick)
        {
            SetPassThrough(true);
            FitPassThroughToTarget(step.target, step.holePadding);
        }
        else
        {
            SetPassThrough(false);
        }

        FitHoleToTarget(step.target, step.holePadding);
        FitHighlightToTarget(step.target, step.holePadding);
        PlaceTooltip(step.target, step.tooltipOffset);


        _showSeq = DOTween.Sequence().SetUpdate(true);

        if (overlay)
            _showSeq.Join(overlay.DOFade(0.75f, animDuration).SetEase(Ease.OutQuad).SetUpdate(true));

        if (tooltipGroup)
            _showSeq.Join(tooltipGroup.DOFade(1f, animDuration).SetEase(Ease.OutQuad).SetUpdate(true));

        if (highlightBorder)
        {
            highlightBorder.localScale = Vector3.one * 0.92f;
            _showSeq.Join(highlightBorder.DOScale(1f, animDuration).SetEase(Ease.OutBack).SetUpdate(true));
        }

        StartPulse();
        StartSweep();

        if (handPointer && step.showHandPointer)
            StartHand(step);
        else if (handPointer)
            handPointer.gameObject.SetActive(false);

        if (step.requireTargetClick)
            HookTargetClick(step.target);
        else
            UnhookTargetClick(null);
    }

    void FitHoleToTarget(RectTransform target, Vector2 padding)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Camera cam = null; // Screen Space Overlay bo‘lsa null

        Vector2 blScreen = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 trScreen = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, blScreen, cam, out Vector2 blLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, trScreen, cam, out Vector2 trLocal);

        // canvas pivot 0.5 bo‘lsa local markazdan boshlanadi, uni bottom-left ga o‘tkazamiz
        float canvasW = canvasRect.rect.width;
        float canvasH = canvasRect.rect.height;

        Vector2 min = blLocal + new Vector2(canvasW * 0.5f, canvasH * 0.5f) - padding;
        Vector2 max = trLocal + new Vector2(canvasW * 0.5f, canvasH * 0.5f) + padding;

        SetMask(maskBottom, new Vector2(0, 0), new Vector2(canvasW, min.y));
        SetMask(maskTop, new Vector2(0, max.y), new Vector2(canvasW, canvasH - max.y));
        SetMask(maskLeft, new Vector2(0, min.y), new Vector2(min.x, max.y - min.y));
        SetMask(maskRight, new Vector2(max.x, min.y), new Vector2(canvasW - max.x, max.y - min.y));
    }

    // offsetMin = (left, bottom), offsetMax = (-right, -top) for stretch.
    // Lekin biz “manual” qilib: anchor stretch + offsets via SetInsetAndSizeFromParentEdge ham bo‘ladi.
    // Bu soddaroq: mask rect’larni absolute position + size bilan qo‘yamiz.
    void SetMask(RectTransform rt, Vector2 pos, Vector2 size)
    {
        if (!rt) return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;

        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    void FitHighlightToTarget(RectTransform target, Vector2 padding)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Camera cam = null;

        Vector2 blScreen = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 trScreen = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, blScreen, cam, out Vector2 blLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, trScreen, cam, out Vector2 trLocal);

        Vector2 size = (trLocal - blLocal) + new Vector2(padding.x * 2f, padding.y * 2f);
        Vector2 center = (blLocal + trLocal) * 0.5f;

        highlightBorder.anchorMin = new Vector2(0.5f, 0.5f);
        highlightBorder.anchorMax = new Vector2(0.5f, 0.5f);
        highlightBorder.pivot = new Vector2(0.5f, 0.5f);

        highlightBorder.anchoredPosition = center;
        highlightBorder.sizeDelta = size;
        highlightBorder.localScale = Vector3.one;
    }
    void PlaceTooltip(RectTransform target, Vector2 offset)
    {
        if (!tooltipPanel || !target || !canvasRect) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);

        Camera cam = null; // Screen Space Overlay bo'lsa null
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(cam, corners[0]),
            cam,
            out Vector2 targetMin);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(cam, corners[2]),
            cam,
            out Vector2 targetMax);

        tooltipPanel.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipPanel.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipPanel.pivot = new Vector2(0.5f, 0.5f);
        tooltipPanel.localScale = Vector3.one;

        Vector2 tooltipSize = tooltipPanel.rect.size;
        if (tooltipSize.x <= 1f || tooltipSize.y <= 1f)
            tooltipSize = tooltipPanel.sizeDelta;

        Vector2 tooltipHalf = tooltipSize * 0.5f;
        Rect canvasBounds = canvasRect.rect;
        Vector2 targetCenter = (targetMin + targetMax) * 0.5f;

        float minX = canvasBounds.xMin + tooltipHalf.x + tooltipCanvasMargin;
        float maxX = canvasBounds.xMax - tooltipHalf.x - tooltipCanvasMargin;
        float minY = canvasBounds.yMin + tooltipHalf.y + tooltipCanvasMargin;
        float maxY = canvasBounds.yMax - tooltipHalf.y - tooltipCanvasMargin;

        Vector2 desired = PickTooltipPosition(
            targetMin,
            targetMax,
            targetCenter,
            tooltipHalf,
            offset,
            minX,
            maxX,
            minY,
            maxY);

        tooltipPanel.anchoredPosition = desired;
    }

    Vector2 PickTooltipPosition(
        Vector2 targetMin,
        Vector2 targetMax,
        Vector2 targetCenter,
        Vector2 tooltipHalf,
        Vector2 offset,
        float minX,
        float maxX,
        float minY,
        float maxY)
    {
        Vector2[] candidates = BuildTooltipCandidates(targetMin, targetMax, targetCenter, offset, minX, maxX, minY, maxY, tooltipHalf);

        float bestScore = float.MaxValue;
        Vector2 best = candidates[0];

        for (int i = 0; i < candidates.Length; i++)
        {
            Vector2 candidate = ClampTooltipPosition(candidates[i], minX, maxX, minY, maxY);
            float overlapPenalty = TooltipOverlapsTarget(candidate, tooltipHalf, targetMin, targetMax) ? 100000f : 0f;
            float offsetPenalty = Vector2.Distance(candidate, targetCenter + offset);
            float edgePenalty = GetEdgePenalty(candidate, minX, maxX, minY, maxY);
            float score = overlapPenalty + offsetPenalty + edgePenalty;

            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    Vector2[] BuildTooltipCandidates(
        Vector2 targetMin,
        Vector2 targetMax,
        Vector2 targetCenter,
        Vector2 offset,
        float minX,
        float maxX,
        float minY,
        float maxY,
        Vector2 tooltipHalf)
    {
        float gap = Mathf.Max(tooltipTargetGap, 8f);
        float sideY = Mathf.Clamp(targetCenter.y + offset.y * 0.35f, minY, maxY);
        float verticalX = Mathf.Clamp(targetCenter.x + offset.x * 0.35f, minX, maxX);

        Vector2 right = new Vector2(targetMax.x + tooltipHalf.x + gap, sideY);
        Vector2 left = new Vector2(targetMin.x - tooltipHalf.x - gap, sideY);
        Vector2 above = new Vector2(verticalX, targetMax.y + tooltipHalf.y + gap);
        Vector2 below = new Vector2(verticalX, targetMin.y - tooltipHalf.y - gap);
        Vector2 preferred = targetCenter + offset;

        if (Mathf.Abs(offset.x) >= Mathf.Abs(offset.y))
        {
            return offset.x >= 0f
                ? new[] { right, above, below, left, preferred }
                : new[] { left, above, below, right, preferred };
        }

        return offset.y >= 0f
            ? new[] { above, right, left, below, preferred }
            : new[] { below, right, left, above, preferred };
    }

    Vector2 ClampTooltipPosition(Vector2 position, float minX, float maxX, float minY, float maxY)
    {
        if (minX > maxX)
            position.x = (minX + maxX) * 0.5f;
        else
            position.x = Mathf.Clamp(position.x, minX, maxX);

        if (minY > maxY)
            position.y = (minY + maxY) * 0.5f;
        else
            position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }

    bool TooltipOverlapsTarget(Vector2 tooltipCenter, Vector2 tooltipHalf, Vector2 targetMin, Vector2 targetMax)
    {
        float minX = tooltipCenter.x - tooltipHalf.x - tooltipTargetGap;
        float maxX = tooltipCenter.x + tooltipHalf.x + tooltipTargetGap;
        float minY = tooltipCenter.y - tooltipHalf.y - tooltipTargetGap;
        float maxY = tooltipCenter.y + tooltipHalf.y + tooltipTargetGap;

        return minX < targetMax.x &&
               maxX > targetMin.x &&
               minY < targetMax.y &&
               maxY > targetMin.y;
    }

    float GetEdgePenalty(Vector2 position, float minX, float maxX, float minY, float maxY)
    {
        float xPenalty = Mathf.Min(Mathf.Abs(position.x - minX), Mathf.Abs(maxX - position.x));
        float yPenalty = Mathf.Min(Mathf.Abs(position.y - minY), Mathf.Abs(maxY - position.y));
        return -Mathf.Min(xPenalty, yPenalty) * 0.1f;
    }

    void StartPulse()
    {
        if (!highlightBorder) return;

        _pulseTween = highlightBorder
            .DOScale(1.03f, 0.55f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    void StartSweep()
    {
        if (!sweepGlow || !highlightBorder) return;

        sweepGlow.gameObject.SetActive(true);
        sweepGlow.SetAsLastSibling();

        // sweepGlow is an image (like gradient) that moves across the border
        // Place it left of highlight and move to right
        _sweepTween = DOTween.Sequence().SetUpdate(true)
            .AppendCallback(() =>
            {
                sweepGlow.position = highlightBorder.position;
                sweepGlow.sizeDelta = highlightBorder.sizeDelta;
                sweepGlow.anchoredPosition += new Vector2(-highlightBorder.sizeDelta.x * 0.6f, 0);
                sweepGlow.localScale = Vector3.one;
            })
            .Append(sweepGlow.DOAnchorPos(sweepGlow.anchoredPosition + new Vector2(highlightBorder.sizeDelta.x * 1.2f, 0), 0.9f)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true))
            .AppendInterval(0.35f)
            .SetLoops(-1);
    }

    void StartHand(Step step)
    {
        if (!handPointer || step.target == null) return;

        handPointer.gameObject.SetActive(true);
        handPointer.SetAsLastSibling();

        Vector2 padding = step.holePadding;

        handPointer.position = step.target.position;
        handPointer.anchoredPosition += new Vector2(0, -(padding.y + 65f));
        handPointer.localScale = Vector3.one;

        _handTween?.Kill();

        Vector2 startPos = handPointer.anchoredPosition;

        switch (step.handAnimType)
        {
            case HandAnimType.Tap:
                _handTween = DOTween.Sequence().SetUpdate(true)
                    .Append(handPointer.DOScale(0.92f, 0.18f).SetEase(Ease.OutQuad).SetUpdate(true))
                    .Append(handPointer.DOScale(1.00f, 0.22f).SetEase(Ease.OutBack).SetUpdate(true))
                    .AppendInterval(0.45f)
                    .SetLoops(-1);
                break;

            case HandAnimType.DragUp:
                _handTween = DOTween.Sequence().SetUpdate(true)
                    .Append(handPointer.DOAnchorPosY(startPos.y + step.dragAmount, 0.35f).SetEase(Ease.InOutSine).SetUpdate(true))
                    .Append(handPointer.DOAnchorPosY(startPos.y, 0.35f).SetEase(Ease.InOutSine).SetUpdate(true))
                    .AppendInterval(0.2f)
                    .SetLoops(-1);
                break;

            case HandAnimType.DragDown:
                _handTween = DOTween.Sequence().SetUpdate(true)
                    .Append(handPointer.DOAnchorPosY(startPos.y - step.dragAmount, 0.35f).SetEase(Ease.InOutSine).SetUpdate(true))
                    .Append(handPointer.DOAnchorPosY(startPos.y, 0.35f).SetEase(Ease.InOutSine).SetUpdate(true))
                    .AppendInterval(0.2f)
                    .SetLoops(-1);
                break;

            case HandAnimType.DragLeft:
                _handTween = DOTween.Sequence().SetUpdate(true)
                    .Append(handPointer.DOAnchorPosX(startPos.x - step.dragAmount, 0.35f).SetEase(Ease.InOutSine).SetUpdate(true))
                    .Append(handPointer.DOAnchorPosX(startPos.x, 0.35f).SetEase(Ease.InOutSine).SetUpdate(true))
                    .AppendInterval(0.2f)
                    .SetLoops(-1);
                break;

            case HandAnimType.DragRight:
                _handTween = DOTween.Sequence().SetUpdate(true)
                    .Append(handPointer.DOAnchorPosX(startPos.x + step.dragAmount, 0.35f).SetEase(Ease.InOutSine).SetUpdate(true))
                    .Append(handPointer.DOAnchorPosX(startPos.x, 0.35f).SetEase(Ease.InOutSine).SetUpdate(true))
                    .AppendInterval(0.2f)
                    .SetLoops(-1);
                break;
        }
    }
    void SetPassThrough(bool enabled)
    {
        if (!passThroughArea || !passThroughImage) return;

        passThroughArea.gameObject.SetActive(enabled);
        passThroughImage.raycastTarget = enabled;
    }

    void FitPassThroughToTarget(RectTransform target, Vector2 padding)
    {
        if (!passThroughArea) return;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector2 min = (Vector2)corners[0] - padding;
        Vector2 max = (Vector2)corners[2] + padding;

        passThroughArea.anchorMin = Vector2.zero;
        passThroughArea.anchorMax = Vector2.zero;
        passThroughArea.pivot = new Vector2(0.5f, 0.5f);

        Vector2 center = (min + max) * 0.5f;
        Vector2 size = (max - min);

        passThroughArea.position = center;
        passThroughArea.sizeDelta = size;
    }

    // Target click capture
    Button _hookedButton;
    void HookTargetClick(RectTransform target)
    {
        UnhookTargetClick(null);

        // target ustida Button bo‘lsa, shuni tutamiz
        var btn = target.GetComponent<Button>();
        if (!btn) btn = target.GetComponentInChildren<Button>();
        if (!btn) return;

        _hookedButton = btn;
        _hookedButton.onClick.AddListener(OnTargetClicked);
    }

    void UnhookTargetClick(RectTransform _)
    {
        if (_hookedButton != null)
            _hookedButton.onClick.RemoveListener(OnTargetClicked);

        _hookedButton = null;
    }

    void OnTargetClicked()
    {
        // requireTargetClick step tugadi => next
        Next();
    }

    void KillTweens()
    {
        _pulseTween?.Kill();
        _sweepTween?.Kill();
        _handTween?.Kill();
        _showSeq?.Kill();
        _pulseTween = _sweepTween = _handTween = _showSeq = null;

        if (handPointer) handPointer.DOKill();
        if (sweepGlow) sweepGlow.DOKill();
        if (highlightBorder) highlightBorder.DOKill();
        if (overlay) overlay.DOKill();
        if (tooltipGroup) tooltipGroup.DOKill();
    }
}
