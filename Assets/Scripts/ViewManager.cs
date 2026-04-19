using UnityEngine;
using UnityEngine.EventSystems;

public class ViewManager : MonoBehaviour
{
    public static ViewManager Instance;

    private SurvivorController selected;
    public SurvivorController Selected => selected;
    public bool IsGodView => selected == null;

    // 같은 프레임에 표류자 클릭이 처리됐는지 추적
    private bool survivorClickedThisFrame;

    void Awake() => Instance = this;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // UI 패널 위 클릭은 무시
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            // 표류자가 클릭되지 않았으면 신의 시점으로
            if (!survivorClickedThisFrame)
                Deselect();
        }

        survivorClickedThisFrame = false;
    }

    public void Select(SurvivorController survivor)
    {
        survivorClickedThisFrame = true;

        // 이전 선택 해제
        if (selected != null && selected != survivor)
            selected.SetRingVisible(false);

        // 같은 표류자를 다시 클릭하면 해제
        if (selected == survivor)
        {
            Deselect();
            return;
        }

        selected = survivor;
        selected.SetRingVisible(true);
        SurvivorInfoPanel.Instance?.Show(survivor);
    }

    public void Deselect()
    {
        if (selected != null)
            selected.SetRingVisible(false);
        selected = null;
        SurvivorInfoPanel.Instance?.Hide();
    }
}
