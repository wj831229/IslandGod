using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SurvivorInfoPanel : MonoBehaviour
{
    public static SurvivorInfoPanel Instance { get; private set; }

    private const int SLOT_COUNT = 10;
    private const int SLOTS_PER_ROW = 5;

    private GameObject panel;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI stateText;
    private TextMeshProUGUI hungerText;
    private Image[] slotBgs = new Image[SLOT_COUNT];
    private TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[SLOT_COUNT];

    private SurvivorController currentSurvivor;

    static readonly Color slotEmpty   = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    static readonly Color slotFilled  = new Color(0.3f, 0.55f, 0.25f, 0.9f);

    void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
    }

    void BuildPanel()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("InfoCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        panel = new GameObject("SurvivorInfoPanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(-300, 0);
        rt.offsetMax = new Vector2(0, 0);

        panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        nameText  = CreateText("NameText",  panel, new Vector2(0, -40),  22, FontStyles.Bold);
        CreateLabel("StateLabel",  panel, new Vector2(0, -85),  "[ 상태 ]",  13);
        stateText = CreateText("StateText", panel, new Vector2(0, -112), 18);
        CreateLabel("HungerLabel", panel, new Vector2(0, -150), "[ 배고픔 ]", 13);
        hungerText = CreateText("HungerText", panel, new Vector2(0, -176), 16);
        CreateLabel("InvLabel",    panel, new Vector2(0, -215), "[ 인벤토리 ]", 13);

        // 슬롯 그리드 (5x2)
        float slotSize = 48f;
        float gap = 6f;
        float startX = -((SLOTS_PER_ROW * slotSize + (SLOTS_PER_ROW - 1) * gap) / 2f) + slotSize / 2f;
        float startY = -255f;

        for (int i = 0; i < SLOT_COUNT; i++)
        {
            int col = i % SLOTS_PER_ROW;
            int row = i / SLOTS_PER_ROW;
            float x = startX + col * (slotSize + gap);
            float y = startY - row * (slotSize + gap);

            GameObject slot = new GameObject($"Slot_{i}");
            slot.transform.SetParent(panel.transform, false);

            RectTransform srt = slot.AddComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(x, y);
            srt.sizeDelta = new Vector2(slotSize, slotSize);

            slotBgs[i] = slot.AddComponent<Image>();
            slotBgs[i].color = slotEmpty;

            // 슬롯 테두리
            var outline = slot.AddComponent<Outline>();
            outline.effectColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            outline.effectDistance = new Vector2(1, -1);

            // 슬롯 텍스트
            GameObject textObj = new GameObject("SlotText");
            textObj.transform.SetParent(slot.transform, false);
            RectTransform trt = textObj.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            slotTexts[i] = textObj.AddComponent<TextMeshProUGUI>();
            slotTexts[i].fontSize = 9f;
            slotTexts[i].alignment = TextAlignmentOptions.Center;
            slotTexts[i].color = Color.white;
        }
    }

    TextMeshProUGUI CreateText(string name, GameObject parent, Vector2 pos, float size, FontStyles style = FontStyles.Normal)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(280, 36);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        return tmp;
    }

    void CreateLabel(string name, GameObject parent, Vector2 pos, string text, float size)
    {
        var tmp = CreateText(name, parent, pos, size);
        tmp.text = text;
        tmp.color = new Color(1f, 0.8f, 0.2f);
    }

    void Update()
    {
        if (currentSurvivor != null && panel.activeSelf)
            Refresh();
    }

    public void Show(SurvivorController survivor)
    {
        if (currentSurvivor == survivor && panel.activeSelf) { Hide(); return; }
        currentSurvivor = survivor;
        panel.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        currentSurvivor = null;
        panel.SetActive(false);
    }

    void Refresh()
    {
        if (currentSurvivor == null) return;

        nameText.text = currentSurvivor.gameObject.name;

        SurvivorController.stateLabels.TryGetValue(currentSurvivor.CurrentState, out string label);
        stateText.text = label ?? "?";
        stateText.color = currentSurvivor.CurrentState switch
        {
            SurvivorState.채집중 => Color.yellow,
            SurvivorState.먹는중 => Color.green,
            SurvivorState.전투중 => Color.red,
            SurvivorState.수면중 => new Color(0.5f, 0.7f, 1f),
            SurvivorState.사망   => Color.gray,
            _ => Color.white
        };

        hungerText.text = $"{(int)currentSurvivor.hunger} / 100";
        hungerText.color = currentSurvivor.hunger > 50 ? Color.green
                         : currentSurvivor.hunger > 25 ? Color.yellow : Color.red;

        // 슬롯 채우기
        var inv = currentSurvivor.inventory;
        // 슬롯별 아이템 집계 (같은 아이템은 같은 슬롯에 묶기)
        var counts = new Dictionary<string, int>();
        foreach (var item in inv)
            counts[item] = counts.TryGetValue(item, out int c) ? c + 1 : 1;

        var keys = new List<string>(counts.Keys);
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            if (i < keys.Count)
            {
                slotBgs[i].color = slotFilled;
                slotTexts[i].text = $"{keys[i]}\nx{counts[keys[i]]}";
            }
            else
            {
                slotBgs[i].color = slotEmpty;
                slotTexts[i].text = "";
            }
        }
    }
}
