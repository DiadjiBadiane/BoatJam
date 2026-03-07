// Assets/Scripts/UI/HomeScreenBuilder.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HomeScreenBuilder : MonoBehaviour
{
    [ContextMenu("Build Home Screen UI")]
    public void BuildHomeScreen()
    {
        // Assuming this script is on the Canvas
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) return;

        // Destroy existing HomePanel if exists
        Transform existing = transform.Find("HomePanel");
        if (existing) DestroyImmediate(existing.gameObject);

        // Destroy existing LevelSelectPanel if exists
        Transform existingLevelSelect = transform.Find("LevelSelectPanel");
        if (existingLevelSelect) DestroyImmediate(existingLevelSelect.gameObject);

        // Create HomePanel
        GameObject homePanel = new GameObject("HomePanel");
        homePanel.transform.SetParent(transform, false);
        RectTransform hpRect = homePanel.AddComponent<RectTransform>();
        hpRect.anchorMin = Vector2.zero;
        hpRect.anchorMax = Vector2.one;
        hpRect.offsetMin = Vector2.zero;
        hpRect.offsetMax = Vector2.zero;

        // Add HomeScreenAnimator
        HomeScreenAnimator animator = homePanel.AddComponent<HomeScreenAnimator>();

        // Background RawImage
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(homePanel.transform, false);
        RawImage bgImg = bg.AddComponent<RawImage>();
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgImg.color = new Color(0.1f, 0.16f, 0.28f); // Dark blue
        animator.backgroundImage = bgImg;

        // Clouds (simplified as Images)
        for (int i = 0; i < 3; i++)
        {
            GameObject cloud = new GameObject($"Cloud{i+1}");
            cloud.transform.SetParent(bg.transform, false);
            Image cImg = cloud.AddComponent<Image>();
            cImg.color = new Color(1,1,1,0.18f);
            RectTransform cRect = cloud.GetComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(140 - i*20, 40 - i*5);
            cRect.anchoredPosition = new Vector2(-200 + i*100, 200 - i*50);
            // Assign to animator
            if (i == 0) animator.cloud1 = cRect;
            else if (i == 1) animator.cloud2 = cRect;
            else animator.cloud3 = cRect;
        }

        // Waves
        for (int i = 0; i < 3; i++)
        {
            GameObject wave = new GameObject($"Wave{i+1}");
            wave.transform.SetParent(bg.transform, false);
            Image wImg = wave.AddComponent<Image>();
            wImg.color = new Color(0.38f, 0.85f, 0.98f, 0.25f);
            RectTransform wRect = wave.GetComponent<RectTransform>();
            wRect.sizeDelta = new Vector2(400, 80 - i*20);
            wRect.anchorMin = new Vector2(0, 0);
            wRect.anchorMax = new Vector2(1, 0);
            wRect.anchoredPosition = new Vector2(0, 100 - i*20);
            if (i == 0) animator.wave1 = wRect;
            else if (i == 1) animator.wave2 = wRect;
            else animator.wave3 = wRect;
        }

        // Glint
        GameObject glint = new GameObject("Glint");
        glint.transform.SetParent(bg.transform, false);
        Image gImg = glint.AddComponent<Image>();
        gImg.color = new Color(1, 0.87f, 0.5f, 0.6f);
        RectTransform gRect = glint.GetComponent<RectTransform>();
        gRect.sizeDelta = new Vector2(200, 6);
        gRect.anchoredPosition = new Vector2(0, 200);
        animator.glint = gImg;

        // Logo Area
        GameObject logoArea = new GameObject("LogoArea");
        logoArea.transform.SetParent(homePanel.transform, false);
        VerticalLayoutGroup logoLayout = logoArea.AddComponent<VerticalLayoutGroup>();
        logoLayout.childAlignment = TextAnchor.MiddleCenter;
        logoLayout.spacing = 8;
        RectTransform logoRect = logoArea.GetComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.5f, 0.7f);
        logoRect.anchorMax = new Vector2(0.5f, 0.7f);
        logoRect.sizeDelta = new Vector2(300, 200);
        logoRect.anchoredPosition = Vector2.zero;

        // Boat Icon
        GameObject boatIcon = new GameObject("BoatIcon");
        boatIcon.transform.SetParent(logoArea.transform, false);
        TextMeshProUGUI boatText = boatIcon.AddComponent<TextMeshProUGUI>();
        boatText.text = "⛵";
        boatText.fontSize = 64;
        boatText.alignment = TextAlignmentOptions.Center;
        animator.boatIcon = boatIcon.GetComponent<RectTransform>();

        // Title
        GameObject title = new GameObject("Title");
        title.transform.SetParent(logoArea.transform, false);
        TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
        titleText.text = "BOAT JAM";
        titleText.fontSize = 52;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        // Add outline (requires TMP Outline component or material)
        animator.titleText = titleText;

        // Subtitle
        GameObject subtitle = new GameObject("Subtitle");
        subtitle.transform.SetParent(logoArea.transform, false);
        TextMeshProUGUI subText = subtitle.AddComponent<TextMeshProUGUI>();
        subText.text = "Harbor Escape";
        subText.fontSize = 13;
        subText.color = new Color(1,1,1,0.65f);
        subText.alignment = TextAlignmentOptions.Center;
        subText.fontStyle = FontStyles.UpperCase | FontStyles.Bold;
        animator.subtitleText = subText;

        // Stars
        GameObject starsObj = new GameObject("Stars");
        starsObj.transform.SetParent(logoArea.transform, false);
        HorizontalLayoutGroup starsLayout = starsObj.AddComponent<HorizontalLayoutGroup>();
        starsLayout.spacing = 4;
        starsLayout.childAlignment = TextAnchor.MiddleCenter;
        animator.stars = new GameObject[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject star = new GameObject($"Star{i+1}");
            star.transform.SetParent(starsObj.transform, false);
            TextMeshProUGUI starText = star.AddComponent<TextMeshProUGUI>();
            starText.text = "⭐";
            starText.fontSize = 18;
            animator.stars[i] = star;
        }

        // Buttons Area
        GameObject buttonsArea = new GameObject("ButtonsArea");
        buttonsArea.transform.SetParent(homePanel.transform, false);
        VerticalLayoutGroup btnLayout = buttonsArea.AddComponent<VerticalLayoutGroup>();
        btnLayout.spacing = 14;
        btnLayout.childAlignment = TextAnchor.MiddleCenter;
        btnLayout.childForceExpandHeight = false;
        btnLayout.childControlHeight = false;
        RectTransform btnRect = buttonsArea.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.3f);
        btnRect.anchorMax = new Vector2(0.5f, 0.3f);
        btnRect.sizeDelta = new Vector2(300, 300);
        btnRect.anchoredPosition = Vector2.zero;

        // Play Button
        GameObject playBtn = CreateButton("PlayButton", "⚓ PLAY", buttonsArea.transform);
        Image playImg = playBtn.GetComponent<Image>();
        playImg.color = new Color(0.95f, 0.35f, 0.25f); // Red-orange
        TextMeshProUGUI playTxt = playBtn.GetComponentInChildren<TextMeshProUGUI>();
        playTxt.fontSize = 22;
        playTxt.color = Color.white;
        animator.playButton = playBtn.GetComponent<Button>();
        LayoutElement playLayout = playBtn.AddComponent<LayoutElement>();
        playLayout.preferredHeight = 60;
        playLayout.preferredWidth = 280;

        // Levels Button
        GameObject levelsBtn = CreateButton("LevelsButton", "📋 LEVELS", buttonsArea.transform);
        animator.levelsButton = levelsBtn.GetComponent<Button>();
        LayoutElement levelsLayout = levelsBtn.AddComponent<LayoutElement>();
        levelsLayout.preferredHeight = 60;
        levelsLayout.preferredWidth = 280;

        // Settings and Credits Row
        GameObject row = new GameObject("SettingsCreditsRow");
        row.transform.SetParent(buttonsArea.transform, false);
        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 14;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childControlHeight = false;
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(280, 60);
        LayoutElement rowLayout2 = row.AddComponent<LayoutElement>();
        rowLayout2.preferredHeight = 60;
        rowLayout2.preferredWidth = 280;

        GameObject settingsBtn = CreateButton("SettingsButton", "⚙️ Settings", row.transform);
        animator.settingsButton = settingsBtn.GetComponent<Button>();
        LayoutElement settingsLayout = settingsBtn.AddComponent<LayoutElement>();
        settingsLayout.preferredHeight = 60;
        settingsLayout.preferredWidth = 130;

        GameObject creditsBtn = CreateButton("CreditsButton", "🏆 Credits", row.transform);
        animator.creditsButton = creditsBtn.GetComponent<Button>();
        LayoutElement creditsLayout = creditsBtn.AddComponent<LayoutElement>();
        creditsLayout.preferredHeight = 60;
        creditsLayout.preferredWidth = 130;

        // Version
        GameObject version = new GameObject("Version");
        version.transform.SetParent(homePanel.transform, false);
        TextMeshProUGUI verText = version.AddComponent<TextMeshProUGUI>();
        verText.text = "v1.0.0";
        verText.fontSize = 11;
        verText.color = new Color(1,1,1,0.3f);
        verText.alignment = TextAlignmentOptions.Center;
        RectTransform verRect = version.GetComponent<RectTransform>();
        verRect.anchorMin = new Vector2(0.5f, 0.05f);
        verRect.anchorMax = new Vector2(0.5f, 0.05f);
        verRect.sizeDelta = new Vector2(100, 20);
        animator.versionText = verText;

        // Decorative Boats
        GameObject deco1 = new GameObject("DecoBoat1");
        deco1.transform.SetParent(homePanel.transform, false);
        TextMeshProUGUI deco1Text = deco1.AddComponent<TextMeshProUGUI>();
        deco1Text.text = "⛵";
        deco1Text.fontSize = 28;
        deco1Text.color = new Color(1,1,1,0.18f);
        RectTransform deco1Rect = deco1.GetComponent<RectTransform>();
        deco1Rect.anchorMin = new Vector2(0.1f, 0.6f);
        deco1Rect.anchorMax = new Vector2(0.1f, 0.6f);
        deco1Rect.sizeDelta = new Vector2(50, 50);
        animator.decoBoat1 = deco1Rect;

        GameObject deco2 = new GameObject("DecoBoat2");
        deco2.transform.SetParent(homePanel.transform, false);
        TextMeshProUGUI deco2Text = deco2.AddComponent<TextMeshProUGUI>();
        deco2Text.text = "🚤";
        deco2Text.fontSize = 20;
        deco2Text.color = new Color(1,1,1,0.18f);
        RectTransform deco2Rect = deco2.GetComponent<RectTransform>();
        deco2Rect.anchorMin = new Vector2(0.85f, 0.65f);
        deco2Rect.anchorMax = new Vector2(0.85f, 0.65f);
        deco2Rect.sizeDelta = new Vector2(50, 50);
        animator.decoBoat2 = deco2Rect;

        // Level Select Panel
        GameObject levelSelectPanel = new GameObject("LevelSelectPanel");
        levelSelectPanel.transform.SetParent(transform, false);
        RectTransform lspRect = levelSelectPanel.AddComponent<RectTransform>();
        lspRect.anchorMin = Vector2.zero;
        lspRect.anchorMax = Vector2.one;
        lspRect.offsetMin = Vector2.zero;
        lspRect.offsetMax = Vector2.zero;
        Image lspBg = levelSelectPanel.AddComponent<Image>();
        lspBg.color = new Color(0.07f, 0.12f, 0.2f, 0.95f);

        GameObject levelTitle = new GameObject("Title");
        levelTitle.transform.SetParent(levelSelectPanel.transform, false);
        TextMeshProUGUI levelTitleText = levelTitle.AddComponent<TextMeshProUGUI>();
        levelTitleText.text = "SELECT LEVEL";
        levelTitleText.fontSize = 42;
        levelTitleText.alignment = TextAlignmentOptions.Center;
        levelTitleText.color = Color.white;
        RectTransform levelTitleRect = levelTitle.GetComponent<RectTransform>();
        levelTitleRect.anchorMin = new Vector2(0.5f, 0.9f);
        levelTitleRect.anchorMax = new Vector2(0.5f, 0.9f);
        levelTitleRect.sizeDelta = new Vector2(700, 80);
        levelTitleRect.anchoredPosition = Vector2.zero;

        GameObject scrollView = new GameObject("LevelScrollView");
        scrollView.transform.SetParent(levelSelectPanel.transform, false);
        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRect.sizeDelta = new Vector2(860, 420);
        scrollRect.anchoredPosition = new Vector2(0, -10);
        Image scrollBg = scrollView.AddComponent<Image>();
        scrollBg.color = new Color(1f, 1f, 1f, 0.08f);
        ScrollRect sr = scrollView.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(20f, 0f);
        contentRect.offsetMax = new Vector2(-20f, 0f);

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(150, 70);
        grid.spacing = new Vector2(14, 14);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = viewportRect;
        sr.content = contentRect;

        GameObject levelButtonTemplate = CreateButton("LevelButtonTemplate", "1", content.transform);
        levelButtonTemplate.SetActive(false);
        RectTransform templateRect = levelButtonTemplate.GetComponent<RectTransform>();
        templateRect.sizeDelta = new Vector2(150, 70);
        TextMeshProUGUI templateText = levelButtonTemplate.GetComponentInChildren<TextMeshProUGUI>();
        if (templateText != null)
        {
            templateText.fontSize = 30;
            templateText.fontStyle = FontStyles.Bold;
        }

        GameObject lockIcon = new GameObject("LockIcon");
        lockIcon.transform.SetParent(levelButtonTemplate.transform, false);
        TextMeshProUGUI lockText = lockIcon.AddComponent<TextMeshProUGUI>();
        lockText.text = "LOCK";
        lockText.fontSize = 16;
        lockText.alignment = TextAlignmentOptions.Center;
        lockText.color = new Color(1f, 0.85f, 0.3f, 0.95f);
        RectTransform lockRect = lockIcon.GetComponent<RectTransform>();
        lockRect.anchorMin = new Vector2(0.5f, 0.2f);
        lockRect.anchorMax = new Vector2(0.5f, 0.2f);
        lockRect.sizeDelta = new Vector2(80, 24);
        lockRect.anchoredPosition = Vector2.zero;
        lockIcon.SetActive(false);

        GameObject closeBtn = CreateButton("LevelSelectCloseButton", "BACK", levelSelectPanel.transform);
        RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0.1f);
        closeRect.anchorMax = new Vector2(0.5f, 0.1f);
        closeRect.sizeDelta = new Vector2(240, 56);
        closeRect.anchoredPosition = Vector2.zero;

        // Ensure home is visible by default when rebuilt
        levelSelectPanel.SetActive(false);

        // Auto-wire MainMenuManager references when present in scene
        MainMenuManager menuManager = FindObjectOfType<MainMenuManager>();
        if (menuManager != null)
        {
            menuManager.homePanel = homePanel;
            menuManager.levelSelectPanel = levelSelectPanel;
            menuManager.levelButtonContainer = content.transform;
            if (menuManager.levelButtonPrefab == null)
                menuManager.levelButtonPrefab = levelButtonTemplate;
            menuManager.levelSelectCloseButton = closeBtn.GetComponent<Button>();
            menuManager.playButton = playBtn.GetComponent<Button>();
            menuManager.levelSelectButton = levelsBtn.GetComponent<Button>();
            menuManager.settingsButton = settingsBtn.GetComponent<Button>();
            menuManager.creditsButton = creditsBtn.GetComponent<Button>();
        }

        Debug.Log("Home Screen UI built successfully!");
    }

    GameObject CreateButton(string name, string text, Transform parent)
    {
        GameObject btn = new GameObject(name);
        btn.transform.SetParent(parent, false);
        Image img = btn.AddComponent<Image>();
        img.color = new Color(1,1,1,0.12f);
        Button button = btn.AddComponent<Button>();
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btn.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.fontSize = 18;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        RectTransform btnRect = btn.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(250, 50);
        return btn;
    }
}