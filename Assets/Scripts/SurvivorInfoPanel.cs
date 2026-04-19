using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SurvivorInfoPanel : MonoBehaviour
{
    public static SurvivorInfoPanel Instance { get; private set; }

    private GameObject panel;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI stateText;
    private TextMeshProUGUI inventoryText;
    private TextMeshProUGUI hungerText;

    private SurvivorController currentSurvivor;

    void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
    }

    void BuildPanel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("InfoCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 패널 배경
        panel = new GameObject("SurvivorInfoPanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(-320, 0);
        rt.offsetMax = new Vector2(0, 0);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.75f);

        // 이름
        nameText = CreateText("NameText", panel, new Vector2(0, -40), 22, FontStyles.Bold);

        // 구분선 레이블
        CreateLabel("StateLabel", panel, new Vector2(0, -90), "[ STATUS ]", 14, new Color(1f, 0.8f, 0.2f));
        stateText = CreateText("StateText", panel, new Vector2(0, -120), 18);

        CreateLabel("HungerLabel", panel, new Vector2(0, -165), "[ HUNGER ]", 14, new Color(1f, 0.8f, 0.2f));
        hungerText = CreateText("HungerText", panel, new Vector2(0, -195), 16);

        CreateLabel("InvLabel", panel, new Vector2(0, -240), "[ INVENTORY ]", 14, new Color(1f, 0.8f, 0.2f));
        inventoryText = CreateText("InvText", panel, new Vector2(0, -310), 15);
        inventoryText.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 200);
    }

    TextMeshProUGUI CreateText(string name, GameObject parent, Vector2 anchoredPos, float size, FontStyles style = FontStyles.Normal)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(280, 40);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        return tmp;
    }

    void CreateLabel(string name, GameObject parent, Vector2 pos, string text, float size, Color color)
    {
        TextMeshProUGUI tmp = CreateText(name, parent, pos, size);
        tmp.text = text;
        tmp.color = color;
    }

    void Update()
    {
        if (currentSurvivor != null && panel.activeSelf)
            Refresh();
    }

    public void Show(SurvivorController survivor)
    {
        if (currentSurvivor == survivor && panel.activeSelf)
        {
            Hide();
            return;
        }

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

        string stateName = SurvivorController.stateLabels.TryGetValue(
            currentSurvivor.CurrentState, out string label) ? label : "?";
        stateText.text = stateName;

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
                         : currentSurvivor.hunger > 25 ? Color.yellow
                         : Color.red;

        if (currentSurvivor.inventory.Count == 0)
            inventoryText.text = "(비어있음)";
        else
        {
            var counts = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var item in currentSurvivor.inventory)
                counts[item] = counts.TryGetValue(item, out int c) ? c + 1 : 1;

            string inv = "";
            foreach (var kv in counts)
                inv += $"{kv.Key} x{kv.Value}\n";
            inventoryText.text = inv.TrimEnd();
        }
    }
}
