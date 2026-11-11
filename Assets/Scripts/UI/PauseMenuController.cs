using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

// Esc 우선순위와 화면 설정을 하나의 런타임 UI에서 관리한다.
[DefaultExecutionOrder(-2000)]
public sealed class PauseMenuController : MonoBehaviour
{
    private enum View { Closed, Menu, Settings, QuitConfirmation }

    private static PauseMenuController instance;
    private View view;
    private Vector2 scroll;
    private Resolution[] resolutions;
    private int selectedResolution;
    private int escapeConsumedFrame = -1;
    private EventSystem suspendedEventSystem;

    public static bool IsOpen => instance != null && instance.view != View.Closed;
    public static bool BlocksWorldInput => IsOpen || (instance != null && instance.escapeConsumedFrame == Time.frameCount);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureCreated()
    {
        if (instance != null) return;
        var host = new GameObject("PauseMenuController(AutoCreated)");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<PauseMenuController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        resolutions = Screen.resolutions
            .GroupBy(value => (value.width, value.height))
            .Select(group => group.Last())
            .OrderBy(value => value.width).ThenBy(value => value.height)
            .ToArray();
        M5SaveService.ApplySavedDisplaySettings();
        selectedResolution = FindResolutionIndex(M5SaveService.ResolutionWidth, M5SaveService.ResolutionHeight);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (view == View.Settings || view == View.QuitConfirmation)
        {
            view = View.Menu;
            return;
        }
        if (view == View.Menu)
        {
            Close();
            return;
        }
        if (InventoryPanel.CloseForEscape())
        {
            escapeConsumedFrame = Time.frameCount;
            return;
        }
        if (GroundLoot.IsSelectionOpen)
        {
            escapeConsumedFrame = Time.frameCount;
            return; // 해당 UI가 같은 프레임에 자신을 닫는다.
        }
        Open(View.Menu);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        if (suspendedEventSystem != null) suspendedEventSystem.enabled = true;
        if (instance == this) instance = null;
    }

    private void OnGUI()
    {
        if (view == View.Closed) return;
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
        Rect area = new Rect(Screen.width * 0.5f - 190f, Screen.height * 0.5f - 180f, 380f, 360f);
        GUILayout.BeginArea(area, GUI.skin.window);
        if (view == View.Menu) DrawMenu();
        else if (view == View.Settings) DrawSettings();
        else DrawQuitConfirmation();
        GUILayout.EndArea();
    }

    private void DrawMenu()
    {
        GUILayout.Label("일시정지");
        GUILayout.Space(18);
        if (GUILayout.Button("계속", GUILayout.Height(48))) Close();
        if (GUILayout.Button("설정", GUILayout.Height(48))) view = View.Settings;
        if (GUILayout.Button("종료", GUILayout.Height(48)))
        {
            if (ExpeditionSession.IsActive) view = View.QuitConfirmation;
            else QuitGame();
        }
    }

    private void DrawSettings()
    {
        GUILayout.Label("화면 설정");
        bool fullscreen = GUILayout.Toggle(M5SaveService.Fullscreen, "전체화면");
        if (fullscreen != M5SaveService.Fullscreen)
            M5SaveService.UpdateDisplaySettings(CurrentWidth(), CurrentHeight(), fullscreen);

        GUILayout.Label("해상도");
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(220));
        if (resolutions.Length == 0) GUILayout.Label("사용 가능한 해상도 목록이 없습니다.");
        for (int i = 0; i < resolutions.Length; i++)
        {
            var resolution = resolutions[i];
            if (GUILayout.Toggle(i == selectedResolution, $"{resolution.width} x {resolution.height}") && i != selectedResolution)
            {
                selectedResolution = i;
                M5SaveService.UpdateDisplaySettings(resolution.width, resolution.height, M5SaveService.Fullscreen);
            }
        }
        GUILayout.EndScrollView();
        if (GUILayout.Button("뒤로")) view = View.Menu;
    }

    private void DrawQuitConfirmation()
    {
        GUILayout.Label("원정 중 종료하면 MIA 처리되어\n장착품과 획득품을 모두 잃습니다.");
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("종료 확정", GUILayout.Height(48))) QuitGame();
        if (GUILayout.Button("취소", GUILayout.Height(48))) view = View.Menu;
    }

    private void Open(View target)
    {
        view = target;
        Time.timeScale = 0f;
        suspendedEventSystem = EventSystem.current;
        if (suspendedEventSystem != null) suspendedEventSystem.enabled = false;
    }

    private void Close()
    {
        view = View.Closed;
        Time.timeScale = 1f;
        if (suspendedEventSystem != null) suspendedEventSystem.enabled = true;
        suspendedEventSystem = null;
    }

    private void QuitGame()
    {
        if (!ExpeditionSession.IsActive) M5SaveService.SaveNow();
        Time.timeScale = 1f;
        if (suspendedEventSystem != null) suspendedEventSystem.enabled = true;
#if UNITY_EDITOR
        Debug.Log("[PauseMenu] 에디터 Play Mode를 종료합니다.");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private int FindResolutionIndex(int width, int height)
    {
        for (int i = 0; i < resolutions.Length; i++)
            if (resolutions[i].width == width && resolutions[i].height == height) return i;
        return resolutions.Length > 0 ? resolutions.Length - 1 : -1;
    }

    private int CurrentWidth() => selectedResolution >= 0 ? resolutions[selectedResolution].width : Screen.width;
    private int CurrentHeight() => selectedResolution >= 0 ? resolutions[selectedResolution].height : Screen.height;
}
