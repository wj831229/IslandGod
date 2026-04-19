using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ViewManager : MonoBehaviour
{
    public static ViewManager Instance;

    private SurvivorController selected;
    public SurvivorController Selected => selected;
    public bool IsGodView => selected == null;

    private GameObject godViewButton;

    void Awake()
    {
        Instance = this;
        CreateGodViewButton();
    }

    void CreateGodViewButton()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        godViewButton = new GameObject("GodViewButton");
        godViewButton.transform.SetParent(canvas.transform, false);

        RectTransform rt = godViewButton.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(10, -10);
        rt.sizeDelta = new Vector2(130, 36);

        Image bg = godViewButton.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.3f, 0.6f, 0.9f);

        Button btn = godViewButton.AddComponent<Button>();
        btn.onClick.AddListener(Deselect);

        // 버튼 텍스트
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(godViewButton.transform, false);
        RectTransform trt = textObj.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "👁 신의 시점";
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        // 처음엔 숨김 (신의 시점일 때는 버튼 불필요)
        godViewButton.SetActive(false);
    }

    public void Select(SurvivorController survivor)
    {
        // 이전 선택 해제
        if (selected != null && selected != survivor)
            selected.SetRingVisible(false);

        // 같은 표류자 재클릭 → 해제
        if (selected == survivor)
        {
            Deselect();
            return;
        }

        selected = survivor;
        selected.SetRingVisible(true);
        SurvivorInfoPanel.Instance?.Show(survivor);

        // 신의 시점 버튼 표시
        if (godViewButton != null)
            godViewButton.SetActive(true);
    }

    public void Deselect()
    {
        if (selected != null)
            selected.SetRingVisible(false);
        selected = null;
        SurvivorInfoPanel.Instance?.Hide();

        // 신의 시점 버튼 숨김
        if (godViewButton != null)
            godViewButton.SetActive(false);
    }
}
