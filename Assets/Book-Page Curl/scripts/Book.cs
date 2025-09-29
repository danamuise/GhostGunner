//The implementation is based on this article:http://rbarraza.com/html5-canvas-pageflip/
//As the rbarraza.com website is not live anymore you can get an archived version from web archive 
//or check an archived version that I uploaded on my website: https://dandarawy.com/html5-canvas-pageflip/

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
public enum FlipMode
{
    RightToLeft,
    LeftToRight
}
[ExecuteInEditMode]
public class Book : MonoBehaviour
{
    public Canvas canvas;
    [SerializeField]
    RectTransform BookPanel;
    public Sprite background;
    public GameObject pageLeft;
    public Sprite[] bookPages;
    public bool interactable = true;
    public bool enableShadowEffect = true;
    //represent the index of the sprite shown in the right page
    public int currentPage = 0;
    public int TotalPageCount
    {
        get { return bookPages.Length; }
    }
    public Vector3 EndBottomLeft
    {
        get { return ebl; }
    }
    public Vector3 EndBottomRight
    {
        get { return ebr; }
    }
    public float Height
    {
        get
        {
            return BookPanel.rect.height;
        }
    }
    public Image ClippingPlane;
    public Image NextPageClip;
    public Image Shadow;
    public Image ShadowLTR;
    public Image Left;
    public Image LeftNext;
    public Image Right;
    public Image RightNext;
    public UnityEvent OnFlip;
    float radius1, radius2;
    //Spine Bottom
    Vector3 sb;
    //Spine Top
    Vector3 st;
    //corner of the page
    Vector3 c;
    //Edge Bottom Right
    Vector3 ebr;
    //Edge Bottom Left
    Vector3 ebl;
    //follow point 
    Vector3 f;
    bool pageDragging = false;
    //current flip mode
    FlipMode mode;

    public Button btn_next;
    public Button btn_prev;
    public Button btn_close;
    public Button btn_useBook;

    // Define open & closed positions/rotations
    public Vector3 closedPosition = new Vector3(243f, -23f, 0f);
    public Vector3 closedRotation = new Vector3(0f, 0f, -26.3f);

    public Vector3 openPosition = new Vector3(-444f, 92f, 0f);
    public Vector3 openRotation = new Vector3(0f, 0f, 0f);

    public float openCloseDuration = 0.5f;
    [Header("Book Parent container")]
    public GameObject bookMainPrefab; // the parent to enable/disable

    public Vector3 state4Position = new Vector3(245f, -15f, 0f);
    public Vector3 offscreenPosition = new Vector3(729f, -15f, 0f);
    public Vector3 smallScale = new Vector3(0.68f, 0.68f, 0.68f);
    public Vector3 fullScale = new Vector3(1.59956f, 1.59956f, 1.59956f);
    public BookPhotoMatcher bookPhotoMatcher;
    public UnityEvent onBookOpened;
    //prevents conflict with page turning and book close
    public bool isPageTurning { get; private set; }
    private bool pageTurnButtonClicked = false;

    void Start()
    {
        if (bookPhotoMatcher != null)
        {
            Debug.Log("🔒 Disabling book photo buttons at start.");
            bookPhotoMatcher.SetPhotoButtonsInteractable(false);
        }
            

        transform.localPosition = offscreenPosition;
        transform.localEulerAngles = closedRotation;
        transform.localScale = smallScale;
        // Validate canvas
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (!canvas) Debug.LogError("Book should be a child to a Canvas!");

        // Hide flipping pages initially
        Left.gameObject.SetActive(false);
        Right.gameObject.SetActive(false);

        // Set up initial pages
        UpdateSprites();
        CalcCurlCriticalPoints();

        // Set up page clip and shadow dimensions
        float pageWidth = BookPanel.rect.width / 2.0f;
        float pageHeight = BookPanel.rect.height;

        NextPageClip.rectTransform.sizeDelta = new Vector2(pageWidth, pageHeight + pageHeight * 2);
        ClippingPlane.rectTransform.sizeDelta = new Vector2(pageWidth * 2 + pageHeight, pageHeight + pageHeight * 2);

        float hyp = Mathf.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
        float shadowPageHeight = pageWidth / 2 + hyp;

        Shadow.rectTransform.sizeDelta = new Vector2(pageWidth, shadowPageHeight);
        Shadow.rectTransform.pivot = new Vector2(1, (pageWidth / 2) / shadowPageHeight);

        ShadowLTR.rectTransform.sizeDelta = new Vector2(pageWidth, shadowPageHeight);
        ShadowLTR.rectTransform.pivot = new Vector2(0, (pageWidth / 2) / shadowPageHeight);

        // ✅ Initialize Book at closed position & rotation
        //transform.localPosition = closedPosition;
        //transform.localEulerAngles = closedRotation;

        // ✅ Show only Use Book button; hide control buttons
        //SetUseBookButtonVisible(true);
        //SetControlButtonsVisible(false);

        // ✅ Hook up buttons
        if (btn_useBook != null) btn_useBook.onClick.AddListener(OpenBook);
        if (btn_next != null) btn_next.onClick.AddListener(OnNextPageClicked);
        if (btn_prev != null) btn_prev.onClick.AddListener(OnPrevPageClicked);

        btn_close.onClick.AddListener(CloseBook);
    }


    private void CalcCurlCriticalPoints()
    {
        sb = new Vector3(0, -BookPanel.rect.height / 2);
        ebr = new Vector3(BookPanel.rect.width / 2, -BookPanel.rect.height / 2);
        ebl = new Vector3(-BookPanel.rect.width / 2, -BookPanel.rect.height / 2);
        st = new Vector3(0, BookPanel.rect.height / 2);
        radius1 = Vector2.Distance(sb, ebr);
        float pageWidth = BookPanel.rect.width / 2.0f;
        float pageHeight = BookPanel.rect.height;
        radius2 = Mathf.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
    }

    public Vector3 transformPoint(Vector3 mouseScreenPos)
    {
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Vector3 mouseWorldPos = canvas.worldCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, canvas.planeDistance));
            Vector2 localPos = BookPanel.InverseTransformPoint(mouseWorldPos);

            return localPos;
        }
        else if (canvas.renderMode == RenderMode.WorldSpace)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 globalEBR = transform.TransformPoint(ebr);
            Vector3 globalEBL = transform.TransformPoint(ebl);
            Vector3 globalSt = transform.TransformPoint(st);
            Plane p = new Plane(globalEBR, globalEBL, globalSt);
            float distance;
            p.Raycast(ray, out distance);
            Vector2 localPos = BookPanel.InverseTransformPoint(ray.GetPoint(distance));
            return localPos;
        }
        else
        {
            //Screen Space Overlay
            Vector2 localPos = BookPanel.InverseTransformPoint(mouseScreenPos);
            return localPos;
        }
    }
    void Update()
    {
        if (pageDragging && interactable)
        {
            UpdateBook();
        }
    }
    public void UpdateBook()
    {
        f = Vector3.Lerp(f, transformPoint(Input.mousePosition), Time.deltaTime * 10);
        if (mode == FlipMode.RightToLeft)
            UpdateBookRTLToPoint(f);
        else
            UpdateBookLTRToPoint(f);
    }
    public void UpdateBookLTRToPoint(Vector3 followLocation)
    {
        mode = FlipMode.LeftToRight;
        f = followLocation;
        ShadowLTR.transform.SetParent(ClippingPlane.transform, true);
        ShadowLTR.transform.localPosition = new Vector3(0, 0, 0);
        ShadowLTR.transform.localEulerAngles = new Vector3(0, 0, 0);
        Left.transform.SetParent(ClippingPlane.transform, true);

        Right.transform.SetParent(BookPanel.transform, true);
        Right.transform.localEulerAngles = Vector3.zero;
        LeftNext.transform.SetParent(BookPanel.transform, true);

        c = Calc_C_Position(followLocation);
        Vector3 t1;
        float clipAngle = CalcClipAngle(c, ebl, out t1);
        //0 < T0_T1_Angle < 180
        clipAngle = (clipAngle + 180) % 180;

        ClippingPlane.transform.localEulerAngles = new Vector3(0, 0, clipAngle - 90);
        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

        //page position and angle
        Left.transform.position = BookPanel.TransformPoint(c);
        float C_T1_dy = t1.y - c.y;
        float C_T1_dx = t1.x - c.x;
        float C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;
        Left.transform.localEulerAngles = new Vector3(0, 0, C_T1_Angle - 90 - clipAngle);

        NextPageClip.transform.localEulerAngles = new Vector3(0, 0, clipAngle - 90);
        NextPageClip.transform.position = BookPanel.TransformPoint(t1);
        LeftNext.transform.SetParent(NextPageClip.transform, true);
        Right.transform.SetParent(ClippingPlane.transform, true);
        Right.transform.SetAsFirstSibling();

        ShadowLTR.rectTransform.SetParent(Left.rectTransform, true);
    }
    public void UpdateBookRTLToPoint(Vector3 followLocation)
    {
        mode = FlipMode.RightToLeft;
        f = followLocation;
        Shadow.transform.SetParent(ClippingPlane.transform, true);
        Shadow.transform.localPosition = Vector3.zero;
        Shadow.transform.localEulerAngles = Vector3.zero;
        Right.transform.SetParent(ClippingPlane.transform, true);

        Left.transform.SetParent(BookPanel.transform, true);
        Left.transform.localEulerAngles = Vector3.zero;
        RightNext.transform.SetParent(BookPanel.transform, true);
        c = Calc_C_Position(followLocation);
        Vector3 t1;
        float clipAngle = CalcClipAngle(c, ebr, out t1);
        if (clipAngle > -90) clipAngle += 180;

        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);
        ClippingPlane.transform.localEulerAngles = new Vector3(0, 0, clipAngle + 90);
        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

        //page position and angle
        Right.transform.position = BookPanel.TransformPoint(c);
        float C_T1_dy = t1.y - c.y;
        float C_T1_dx = t1.x - c.x;
        float C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;
        Right.transform.localEulerAngles = new Vector3(0, 0, C_T1_Angle - (clipAngle + 90));

        NextPageClip.transform.localEulerAngles = new Vector3(0, 0, clipAngle + 90);
        NextPageClip.transform.position = BookPanel.TransformPoint(t1);
        RightNext.transform.SetParent(NextPageClip.transform, true);
        Left.transform.SetParent(ClippingPlane.transform, true);
        Left.transform.SetAsFirstSibling();

        Shadow.rectTransform.SetParent(Right.rectTransform, true);
    }
    private float CalcClipAngle(Vector3 c, Vector3 bookCorner, out Vector3 t1)
    {
        Vector3 t0 = (c + bookCorner) / 2;
        float T0_CORNER_dy = bookCorner.y - t0.y;
        float T0_CORNER_dx = bookCorner.x - t0.x;
        float T0_CORNER_Angle = Mathf.Atan2(T0_CORNER_dy, T0_CORNER_dx);
        float T0_T1_Angle = 90 - T0_CORNER_Angle;

        float T1_X = t0.x - T0_CORNER_dy * Mathf.Tan(T0_CORNER_Angle);
        T1_X = normalizeT1X(T1_X, bookCorner, sb);
        t1 = new Vector3(T1_X, sb.y, 0);

        //clipping plane angle=T0_T1_Angle
        float T0_T1_dy = t1.y - t0.y;
        float T0_T1_dx = t1.x - t0.x;
        T0_T1_Angle = Mathf.Atan2(T0_T1_dy, T0_T1_dx) * Mathf.Rad2Deg;
        return T0_T1_Angle;
    }
    private float normalizeT1X(float t1, Vector3 corner, Vector3 sb)
    {
        if (t1 > sb.x && sb.x > corner.x)
            return sb.x;
        if (t1 < sb.x && sb.x < corner.x)
            return sb.x;
        return t1;
    }
    private Vector3 Calc_C_Position(Vector3 followLocation)
    {
        Vector3 c;
        f = followLocation;
        float F_SB_dy = f.y - sb.y;
        float F_SB_dx = f.x - sb.x;
        float F_SB_Angle = Mathf.Atan2(F_SB_dy, F_SB_dx);
        Vector3 r1 = new Vector3(radius1 * Mathf.Cos(F_SB_Angle), radius1 * Mathf.Sin(F_SB_Angle), 0) + sb;

        float F_SB_distance = Vector2.Distance(f, sb);
        if (F_SB_distance < radius1)
            c = f;
        else
            c = r1;
        float F_ST_dy = c.y - st.y;
        float F_ST_dx = c.x - st.x;
        float F_ST_Angle = Mathf.Atan2(F_ST_dy, F_ST_dx);
        Vector3 r2 = new Vector3(radius2 * Mathf.Cos(F_ST_Angle),
           radius2 * Mathf.Sin(F_ST_Angle), 0) + st;
        float C_ST_distance = Vector2.Distance(c, st);
        if (C_ST_distance > radius2)
            c = r2;
        return c;
    }
    public void DragRightPageToPoint(Vector3 point)
    {
        if (currentPage >= bookPages.Length) return;
        pageDragging = true;
        mode = FlipMode.RightToLeft;
        f = point;


        NextPageClip.rectTransform.pivot = new Vector2(0, 0.12f);
        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);

        Left.gameObject.SetActive(true);
        Left.rectTransform.pivot = new Vector2(0, 0);
        Left.transform.position = RightNext.transform.position;
        Left.transform.eulerAngles = new Vector3(0, 0, 0);
        Left.sprite = (currentPage < bookPages.Length) ? bookPages[currentPage] : background;
        Left.transform.SetAsFirstSibling();

        Right.gameObject.SetActive(true);
        Right.transform.position = RightNext.transform.position;
        Right.transform.eulerAngles = new Vector3(0, 0, 0);
        Right.sprite = (currentPage < bookPages.Length - 1) ? bookPages[currentPage + 1] : background;

        RightNext.sprite = (currentPage < bookPages.Length - 2) ? bookPages[currentPage + 2] : background;

        LeftNext.transform.SetAsFirstSibling();
        if (enableShadowEffect) Shadow.gameObject.SetActive(true);
        UpdateBookRTLToPoint(f);
    }
    public void OnMouseDragRightPage()
    {
        if (interactable)
            DragRightPageToPoint(transformPoint(Input.mousePosition));

    }
    public void DragLeftPageToPoint(Vector3 point)
    {
        if (currentPage <= 0) return;
        pageDragging = true;
        mode = FlipMode.LeftToRight;
        f = point;

        NextPageClip.rectTransform.pivot = new Vector2(1, 0.12f);
        ClippingPlane.rectTransform.pivot = new Vector2(0, 0.35f);

        Right.gameObject.SetActive(true);
        Right.transform.position = LeftNext.transform.position;
        Right.sprite = bookPages[currentPage - 1];
        Right.transform.eulerAngles = new Vector3(0, 0, 0);
        Right.transform.SetAsFirstSibling();

        Left.gameObject.SetActive(true);
        Left.rectTransform.pivot = new Vector2(1, 0);
        Left.transform.position = LeftNext.transform.position;
        Left.transform.eulerAngles = new Vector3(0, 0, 0);
        Left.sprite = (currentPage >= 2) ? bookPages[currentPage - 2] : background;

        LeftNext.sprite = (currentPage >= 3) ? bookPages[currentPage - 3] : background;

        RightNext.transform.SetAsFirstSibling();
        if (enableShadowEffect) ShadowLTR.gameObject.SetActive(true);
        UpdateBookLTRToPoint(f);
    }
    public void OnMouseDragLeftPage()
    {
        if (interactable)
            DragLeftPageToPoint(transformPoint(Input.mousePosition));

    }
    public void OnMouseRelease()
    {
        if (interactable)
            ReleasePage();
    }
    public void ReleasePage()
    {
        if (pageDragging)
        {
            pageDragging = false;
            float distanceToLeft = Vector2.Distance(c, ebl);
            float distanceToRight = Vector2.Distance(c, ebr);
            if (distanceToRight < distanceToLeft && mode == FlipMode.RightToLeft)
                TweenBack();
            else if (distanceToRight > distanceToLeft && mode == FlipMode.LeftToRight)
                TweenBack();
            else
                TweenForward();
        }
    }
    Coroutine currentCoroutine;
    void UpdateSprites()
    {
        LeftNext.sprite = (currentPage > 0 && currentPage <= bookPages.Length) ? bookPages[currentPage - 1] : background;
        RightNext.sprite = (currentPage >= 0 && currentPage < bookPages.Length) ? bookPages[currentPage] : background;
    }
    public void TweenForward()
    {
        if (mode == FlipMode.RightToLeft)
            currentCoroutine = StartCoroutine(TweenTo(ebl, 0.15f, () => { Flip(); }));
        else
            currentCoroutine = StartCoroutine(TweenTo(ebr, 0.15f, () => { Flip(); }));
    }
    void Flip()
    {
        if (mode == FlipMode.RightToLeft)
            currentPage += 2;
        else
            currentPage -= 2;

        LeftNext.transform.SetParent(BookPanel.transform, true);
        Left.transform.SetParent(BookPanel.transform, true);
        LeftNext.transform.SetParent(BookPanel.transform, true);
        Left.gameObject.SetActive(false);
        Right.gameObject.SetActive(false);
        Right.transform.SetParent(BookPanel.transform, true);
        RightNext.transform.SetParent(BookPanel.transform, true);
        UpdateSprites();
        Shadow.gameObject.SetActive(false);
        ShadowLTR.gameObject.SetActive(false);
        if (OnFlip != null)
            OnFlip.Invoke();

        // 🔍 Updated page number logging
        if (currentPage <= 0)
        {
            Debug.Log("📖 Page: 0 and 0 (Cover)");
        }
        else
        {
            int leftPage = currentPage;
            int rightPage = Mathf.Min(currentPage + 1, TotalPageCount);
            Debug.Log($"📖 Page: {leftPage} and {rightPage}");
        }
        UpdatePageButtons();
        pageTurnButtonClicked = false;
        Debug.Log("✅ Page turn finished, flag reset to false.");

        // ✅ Enable/Disable pageLeft SpriteRenderer depending on currentPage
        if (pageLeft != null)
        {
            var sr = pageLeft.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = currentPage >= 2;
        }

        if (bookPhotoMatcher != null)
            bookPhotoMatcher.UpdateBookPhotosForPage(currentPage);
        
        // ✅ Only enable photo buttons if we're on pages 2 and 3
        if (currentPage == 2 && bookPhotoMatcher != null)
        {
            Debug.Log("✅ Enabling photo buttons now that page 2–3 is visible.");
            bookPhotoMatcher.activatePhotoButtons();
        }

    }


    public void TweenBack()
    {
        if (mode == FlipMode.RightToLeft)
        {
            currentCoroutine = StartCoroutine(TweenTo(ebr, 0.15f,
                () =>
                {
                    UpdateSprites();
                    RightNext.transform.SetParent(BookPanel.transform);
                    Right.transform.SetParent(BookPanel.transform);

                    Left.gameObject.SetActive(false);
                    Right.gameObject.SetActive(false);
                    pageDragging = false;
                }
                ));
        }
        else
        {
            currentCoroutine = StartCoroutine(TweenTo(ebl, 0.15f,
                () =>
                {
                    UpdateSprites();

                    LeftNext.transform.SetParent(BookPanel.transform);
                    Left.transform.SetParent(BookPanel.transform);

                    Left.gameObject.SetActive(false);
                    Right.gameObject.SetActive(false);
                    pageDragging = false;
                }
                ));
        }
    }
    public IEnumerator TweenTo(Vector3 to, float duration, System.Action onFinish)
    {
        isPageTurning = true;

        int steps = (int)(duration / 0.025f);
        Vector3 displacement = (to - f) / steps;
        for (int i = 0; i < steps - 1; i++)
        {
            if (mode == FlipMode.RightToLeft)
                UpdateBookRTLToPoint(f + displacement);
            else
                UpdateBookLTRToPoint(f + displacement);

            yield return new WaitForSeconds(0.025f);
        }

        if (onFinish != null)
            onFinish();

        isPageTurning = false;
    }

    public void CloseBook()
    {
        if (pageTurnButtonClicked)
        {
            Debug.Log("⏳ CloseBook requested after page button click — adding 1 second delay.");
            StartCoroutine(DelayedCloseBook(1f));
            return;
        }

        ActuallyCloseBook();
    }

    private IEnumerator DelayedCloseBook(float delay)
    {
        yield return new WaitForSeconds(delay);
        ActuallyCloseBook();
    }


    private IEnumerator WaitForPageTurnAndClose()
    {
        while (isPageTurning)
        {
            yield return null;
        }
        // add a short buffer delay after turn
        yield return new WaitForSeconds(0.5f);
        ActuallyCloseBook();
    }

    private void ActuallyCloseBook()
    {
        Debug.Log("📕 Actually closing book cleanly");

        currentPage = 0;
        UpdateSprites();

        Left.gameObject.SetActive(false);
        Right.gameObject.SetActive(false);
        Shadow.gameObject.SetActive(false);
        ShadowLTR.gameObject.SetActive(false);
        pageDragging = false;

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        ClippingPlane.transform.localEulerAngles = Vector3.zero;
        ClippingPlane.transform.localPosition = Vector3.zero;
        Left.transform.localEulerAngles = Vector3.zero;
        Left.transform.localPosition = Vector3.zero;
        Right.transform.localEulerAngles = Vector3.zero;
        Right.transform.localPosition = Vector3.zero;

        SetControlButtonsVisible(false);
        SetUseBookButtonVisible(true);

        StartCoroutine(AnimateTransform(closedPosition, closedRotation, smallScale, openCloseDuration));

        if (bookPhotoMatcher != null)
            bookPhotoMatcher.SetPhotoButtonsInteractable(false);

        if (pageLeft != null)
        {
            var sr = pageLeft.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = false;
        }
    }


    public void HideBook()
    {
        currentPage = 0;
        UpdateSprites();

        Left.gameObject.SetActive(false);
        Right.gameObject.SetActive(false);
        Shadow.gameObject.SetActive(false);
        ShadowLTR.gameObject.SetActive(false);
        pageDragging = false;

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        ClippingPlane.transform.localEulerAngles = Vector3.zero;
        ClippingPlane.transform.localPosition = Vector3.zero;
        Left.transform.localEulerAngles = Vector3.zero;
        Left.transform.localPosition = Vector3.zero;
        Right.transform.localEulerAngles = Vector3.zero;
        Right.transform.localPosition = Vector3.zero;

        SetControlButtonsVisible(false);
        SetUseBookButtonVisible(true);

        StopAllCoroutines();
        StartCoroutine(AnimateTransform(offscreenPosition, closedRotation, smallScale, openCloseDuration));

        if (bookPhotoMatcher != null)
            bookPhotoMatcher.SetPhotoButtonsInteractable(false);

        // ✅ force pageLeft hidden
        if (pageLeft != null)
        {
            var sr = pageLeft.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = false;
        }
    }


    void SetControlButtonsVisible(bool visible)
    {
        if (btn_next != null) btn_next.gameObject.SetActive(visible);
        if (btn_prev != null) btn_prev.gameObject.SetActive(visible);
        if (btn_close != null) btn_close.gameObject.SetActive(visible);
    }

    public void OpenBook()
    {
        
        StopAllCoroutines();
        StartCoroutine(OpenBookRoutine());
        StartCoroutine(NotifyBookOpenedAfterDelay());
    }

    private IEnumerator NotifyBookOpenedAfterDelay()
    {
        yield return new WaitForSeconds(1f); // Adjust for your book open animation time
        onBookOpened?.Invoke();
    }

    private IEnumerator OpenBookRoutine()
    {
        // ✅ Wait for animation to finish
        yield return AnimateTransform(
            openPosition,
            openRotation,
            new Vector3(1.59956002f, 1.59956002f, 1.59956002f),
            openCloseDuration
        );

        SetControlButtonsVisible(true);
        SetUseBookButtonVisible(false);

        UpdatePageButtons();  // ✅ added to fix the next-button fade bug
    }


    IEnumerator AnimateTransform(Vector3 targetPosition, Vector3 targetRotation, Vector3 targetScale, float duration)
    {
        Vector3 startPos = transform.localPosition;
        Vector3 startRot = transform.localEulerAngles;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easedT = EaseOutCubic(t);

            transform.localPosition = Vector3.Lerp(startPos, targetPosition, easedT);
            transform.localEulerAngles = Vector3.Lerp(startRot, targetRotation, easedT);
            transform.localScale = Vector3.Lerp(startScale, targetScale, easedT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = targetPosition;
        transform.localEulerAngles = targetRotation;
        transform.localScale = targetScale;
    }

    //Overloaded function
    IEnumerator AnimateTransform(Vector3 targetPos, Vector3 targetRot, float duration)
    {
        Vector3 startPos = transform.localPosition;
        Vector3 startRot = transform.localEulerAngles;
        Vector3 currentScale = transform.localScale; // keep scale unchanged
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easedT = EaseOutCubic(t);

            transform.localPosition = Vector3.Lerp(startPos, targetPos, easedT);
            transform.localEulerAngles = Vector3.Lerp(startRot, targetRot, easedT);
            transform.localScale = currentScale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = targetPos;
        transform.localEulerAngles = targetRot;
    }


    void SetUseBookButtonVisible(bool visible)
    {
        if (btn_useBook != null) btn_useBook.gameObject.SetActive(visible);
    }

    public void MoveBookIn()
    {
        if (bookMainPrefab != null && !bookMainPrefab.activeSelf)
        {
            bookMainPrefab.SetActive(true);
        }

        StopAllCoroutines();

        // ✅ Disable photo button interaction early
        if (bookPhotoMatcher != null)
        {
            Debug.Log("🔒 Disabling photo buttons from MoveBookIn()");
            bookPhotoMatcher.SetPhotoButtonsInteractable(false);
        }

        // Move in to closed position with initial scale (no scale change yet)
        StartCoroutine(AnimateTransform(
            closedPosition,
            closedRotation,
            new Vector3(0.68f, 0.68f, 0.68f),
            openCloseDuration * 0.8f
        ));

        SetControlButtonsVisible(false);
        SetUseBookButtonVisible(true);
    }



    private float EaseOutCubic(float t)
    {
        return 1 - Mathf.Pow(1 - t, 3);
    }

    private void UpdatePageButtons()
    {
        // Disable "Next" if we've reached or passed the final spread
        bool atLastPage = currentPage >= TotalPageCount - 2;
        bool atFirstPage = currentPage <= 0;

        if (btn_next != null)
            btn_next.interactable = !atLastPage;

        if (btn_prev != null)
            btn_prev.interactable = !atFirstPage;
    }

    public void MoveBookOut()
    {
        Debug.Log("📘 Moving book offscreen...");

        StopAllCoroutines();
        StartCoroutine(AnimateTransform(
            offscreenPosition,
            closedRotation,
            smallScale,
            openCloseDuration * 0.8f
        ));

        SetControlButtonsVisible(false);
        SetUseBookButtonVisible(false);

        if (bookPhotoMatcher != null)
            bookPhotoMatcher.SetPhotoButtonsInteractable(false);
    }

    public void PlayButtonClickSound()
    {
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.Play("buttonClick0");
        }
    }

    private void OnNextPageClicked()
    {
        pageTurnButtonClicked = true;
        Debug.Log("➡️ Next page button clicked, flag set true.");
    }

    private void OnPrevPageClicked()
    {
        pageTurnButtonClicked = true;
        Debug.Log("⬅️ Prev page button clicked, flag set true.");
    }

    public void NextLevel()
    {
        GameState.Instance.LevelNumber += 1;
        GameState.Instance.ContinueFromLastSave = true;
        SceneManager.LoadScene("MainGameScene");
        Debug.Log($"➡ Advance to LEVEL {GameState.Instance.LevelNumber} (mainGameScene).");
    }
}
