using System.Collections;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(CreatureSegmentation))]
[RequireComponent(typeof(CreatureClassification))]
public class GameManager : MonoBehaviour
{
    [SerializeField]
    public CreatureCreator CreatureCreator;
    [SerializeField]
    public FightManager FightManager;
    [SerializeField]
    bool DebugTrigger = false;

    [SerializeField] private GameObject introUI;
    [SerializeField] private AnimationClip introUIHideAnimation;

    [SerializeField] private GameObject mainUI;
    [SerializeField] private TextMeshProUGUI streakText;
    [SerializeField] private TextMeshProUGUI hiScoreText;
    [SerializeField] private TextMeshProUGUI nameMainUI;
    [SerializeField] private TextMeshProUGUI highestStatMainUI;
    [SerializeField] private TextMeshProUGUI lowestStatMainUi;
    [SerializeField] private Image elementImageMainUi;
    [SerializeField] private TextMeshProUGUI elementNameMainUi;

    [SerializeField] private Button advanceIntroButton;
    [SerializeField] private GameObject creatureShowcaseUI;
    [SerializeField] private Transform creatureShowcasePoint;
    [SerializeField] private Button keepButton;
    [SerializeField] private Button discardButton;
    [SerializeField] private float _minPlaneSize;
    [SerializeField] private GameObject provisionalBattlefieldPrefab;
    [SerializeField] private TextMeshProUGUI creatureText;
    [SerializeField] private GameObject spawnParticles;
    [SerializeField] private TextMeshProUGUI highestStat;
    [SerializeField] private TextMeshProUGUI lowestStat;
    [SerializeField] private Image elementImage;
    [SerializeField] private TextMeshProUGUI elementName;

    [SerializeField] private GameObject victoryDefeatUI;
    [SerializeField] private Button victoryDefeatUIButton;
    [SerializeField] private TextMeshProUGUI victoryDefeatUIMain;
    [SerializeField] private TextMeshProUGUI victoryDefeatUIStreak;

    [SerializeField] private Sprite waterElemSprite;
    [SerializeField] private Sprite metalElemSprite;
    [SerializeField] private Sprite earthElemSprite;
    [SerializeField] private Sprite fireElemSprite;
    [SerializeField] private Sprite woodElemSprite;

    private Creature currentCreature;
    private Creature newCreature;
    private GameState state = GameState.Intro;

    private InputAction touchAction;
    private CreatureSegmentation segmentation;
    private CreatureClassification classification;
    private GameObject provisionalBattlefield;

    private int streak;
    private int maxStreak;

    IEnumerator Start()
    {
        segmentation = GetComponent<CreatureSegmentation>();
        classification = GetComponent<CreatureClassification>();
        touchAction = InputSystem.actions.FindAction("Touch");
        FightManager.OnFightComplete += HandleFightResult;
        creatureShowcaseUI.SetActive(false);
        maxStreak = PlayerPrefs.GetInt("hiscore", 0);

        currentCreature = CreatureCreator.CreateDummyCreature();
        currentCreature.gameObject.SetActive(false);

        introUI.GetComponent<Animation>().Play();

        yield return new WaitForSeconds(3f);

        advanceIntroButton.onClick.AddListener(() => {
            StartCoroutine(CoroAdvanceIntro());
            advanceIntroButton.onClick.RemoveAllListeners();
        });
    }

    private IEnumerator CoroAdvanceIntro() {
        introUI.GetComponent<Animation>().clip = introUIHideAnimation;
        introUI.GetComponent<Animation>().Play();

        yield return new WaitForSeconds(1f);
        Destroy(introUI);
        
        state = GameState.Picking;
    }

    private void HandleFightResult(Creature winner)
    {
        StartCoroutine(DoHandleFightResult(winner));
    }

    private IEnumerator DoHandleFightResult(Creature winner) {

        if (winner.Equals(currentCreature))
        {
            currentCreature.JumpAway();
            newCreature.Die();
            yield return new WaitForSeconds(1.5f);
            FightManager.DestroyHealthBars();
            yield return ShowVictoryOrDefeat(false);
            Destroy(currentCreature.gameObject);
            currentCreature = CreatureCreator.CreateDummyCreature();
            currentCreature.gameObject.SetActive(false);
            streak = 0;
        }
        else
        {
            streak++;
            if (streak > maxStreak) {
                maxStreak = streak;
                PlayerPrefs.SetInt("hiscore", streak);
            }
            currentCreature.Die();
            newCreature.JumpAway();
            yield return new WaitForSeconds(1.5f);
            FightManager.DestroyHealthBars();
            yield return ShowVictoryOrDefeat(true);
            currentCreature = newCreature;
            currentCreature.CurrentHealth = currentCreature.Health;
        }

        if (provisionalBattlefield != null) {
            Destroy(provisionalBattlefield);
            provisionalBattlefield = null;
        }
        state = GameState.Picking;
    }

    private IEnumerator ShowVictoryOrDefeat(bool won) {
        victoryDefeatUI.SetActive(true);
        victoryDefeatUIMain.SetText(won ? "VICTORY" : "DEFEAT");
        victoryDefeatUIMain.GetComponent<TypewriterFloatControl>().fullText = victoryDefeatUIMain.text;
        victoryDefeatUIStreak.SetText(streak + " STREAK");
        victoryDefeatUIStreak.GetComponent<TypewriterFloatControl>().fullText = victoryDefeatUIStreak.text;

        var anim = victoryDefeatUI.GetComponent<Animation>();
        anim.Play("UIShowFast");

        yield return new WaitForSeconds(1.0f);

        bool clicked = false;
        victoryDefeatUIButton.onClick.AddListener(() => clicked = true);
        while (!clicked) {
            yield return null;
        }
        victoryDefeatUIButton.onClick.RemoveAllListeners();

        anim.Play("WelcomeUIHide");
        yield return new WaitForSeconds(1f);
        victoryDefeatUI.SetActive(false);
    }

    void Update()
    {
        if (state == GameState.Picking || state == GameState.CreatureConfirmation) {
            ShowMainUI();
        } else {
            HideMainUI();
        }

        if (FightManager.FightRunning)
        {
            return;
        }
        if (DebugTrigger)
        {
            DebugTrigger = false;
            newCreature = CreatureCreator.CreateCreature(createRandomTexture(), new ClassificationPrediction(), GetCreatureSpawnPosition(Vector2.zero));
            FightManager.StartFight(currentCreature, newCreature, null);
            state = GameState.Fighting;
        }

        if (state == GameState.Picking && touchAction.WasPerformedThisFrame()) {
            state = GameState.CreatureConfirmation;
            StartCoroutine(CreateCreatureFromImage());
        }
    }

    private IEnumerator CreateCreatureFromImage() {
        Vector2 pos;
        if (Touchscreen.current != null) {
            pos = Touchscreen.current.primaryTouch.position.value;
        }
        else {
            pos = Mouse.current.position.value;
        }
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 normalizedTouchPos = new Vector2(pos.x / screenSize.x, 1 - (pos.y / screenSize.y));
        Debug.Log("Touch at  " + pos + ", screen size: " + screenSize + ", normalized pos: " + normalizedTouchPos);
        var texture = segmentation.TakeSnapshot();

        if (texture != null) {
            yield return null;
            
            var spawnPosition = GetCreatureSpawnPosition(pos);
            var particles = Instantiate(spawnParticles, spawnPosition, Quaternion.identity);
            yield return segmentation.SegmentTexture(texture, normalizedTouchPos);
            Destroy(texture);
            if (segmentation.SegmentResult == null) {
                state = GameState.Picking;
            } else {
                var segmentedTexture = segmentation.SegmentResult;
                var croppedTexture = segmentation.CroppedTextureWithBackground;
                if (segmentedTexture == null) {
                    state = GameState.Picking; // Revert to picking state
                } else {
                    classification.Classify(croppedTexture);
                    Destroy(croppedTexture);

                    yield return null;

                    if (currentCreature == null) {
                        currentCreature = CreatureCreator.CreateDummyCreature();
                        currentCreature.gameObject.SetActive(false);
                    }
                    var spawnPositionOffsetBack = spawnPosition + Camera.main.transform.forward * 0.15f;
                    newCreature = CreatureCreator.CreateCreature(segmentedTexture, classification.Prediction, spawnPositionOffsetBack);
                    newCreature.transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);

                    yield return new WaitForSeconds(0.3f);

                    particles.GetComponent<ParticleSystem>().Stop();
                    Destroy(particles, 5);

                    yield return new WaitForSeconds(1f);

                    yield return CreatureShowcase(classification.Prediction);
                }
            }
        } else {
            state = GameState.Picking;
        }
    }

    private Vector3 GetCreatureSpawnPosition(Vector2 screenPos) {
        var mainCamera = Camera.main;
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log("Raycast hit " + hit.transform.gameObject.name + ", spawning at " + hit.point);
            return hit.point;
        } else {
            Debug.Log("No raycast hit, spawning at " + hit.point);
            return ray.GetPoint(3f);
        }
    }

    private IEnumerator CreatureShowcase(ClassificationPrediction prediction) {
        float duration = 1.5f;

        creatureText.SetText(newCreature.Name);
        lowestStat.SetText(newCreature.LowestStat());
        highestStat.SetText(newCreature.HighestStat());
        
        SetElementNameAndImage(elementName, elementImage, newCreature.Element);

        creatureShowcaseUI.GetComponent<Animation>()["UIShow"].speed = 1.0f;
        creatureShowcaseUI.GetComponent<Animation>()["UIShow"].time = 0.0f;
        creatureShowcaseUI.GetComponent<Animation>().Play("UIShow");

        yield return null;

        creatureShowcaseUI.SetActive(true);
        newCreature.transform.SetParent(creatureShowcasePoint, true);

        newCreature.transform.DOLocalRotateQuaternion(Quaternion.identity, duration).SetEase(Ease.InFlash).SetEase(Ease.OutBounce);
        newCreature.AttackAnimation();
        yield return newCreature.transform.DOLocalMove(Vector3.zero, duration).SetEase(Ease.InFlash).SetEase(Ease.OutBounce).WaitForCompletion();

        newCreature.transform.position = creatureShowcasePoint.transform.position;
        newCreature.transform.rotation = creatureShowcasePoint.transform.rotation;
        newCreature.ParticleSystem.Play();
        
        bool keep = false;
        bool discard = false;

        keepButton.onClick.AddListener(() => keep = true);
        discardButton.onClick.AddListener(() => discard = true);

        while (!keep && !discard) {
            yield return null;
        }

        keepButton.onClick.RemoveAllListeners();
        discardButton.onClick.RemoveAllListeners();

        newCreature.transform.SetParent(null);

        creatureShowcaseUI.GetComponent<Animation>()["UIShow"].speed = -2.0f;
        creatureShowcaseUI.GetComponent<Animation>()["UIShow"].time = 3.0f;
        creatureShowcaseUI.GetComponent<Animation>().Play("UIShow");

        yield return new WaitForSeconds(1.0f);

        if (keep) {
            currentCreature.gameObject.SetActive(true);
            newCreature.ParticleSystem.Clear();
            newCreature.ParticleSystem.Stop();
            var plane = GetNearestFittingPlane();
            plane = null;
            if (plane == null) {
                var position = Camera.main.transform.position + Camera.main.transform.forward * 1.2f;
                provisionalBattlefield = CreateProvisionalBattlefield(position);
                plane = provisionalBattlefield;
            }

            state = GameState.Fighting;
            StartCoroutine(FightManager.StartFight(currentCreature, newCreature, plane.transform));
        } else
        {
            newCreature.ParticleSystem.Clear();
            newCreature.ParticleSystem.Stop();
            state = GameState.Picking;
            newCreature.Die();
        }

        yield return new WaitForSeconds(1.0f);
        creatureShowcaseUI.SetActive(false);
    }

    private GameObject GetNearestFittingPlane() {
        var planes = FindObjectsByType<ARPlane>(FindObjectsSortMode.None)
            .Where((plane) => (plane.size.x * plane.size.y) > _minPlaneSize && plane.alignment.IsHorizontal() && IsInFront(plane.transform))
            .ToArray();

        if (planes.Length == 0) {
            return null;
        }

        ARPlane nearestPlane = null;
        float nearestPlaneDistance = float.PositiveInfinity;
        foreach (var plane in planes) {
            var dist = Vector3.Distance(Camera.main.transform.position, plane.transform.position);
            if (dist < nearestPlaneDistance) {
                nearestPlane = plane;
                nearestPlaneDistance = dist;
            }
        }

        return nearestPlane.gameObject;
    }

    bool IsInFront(Transform obj)
    {
        Vector3 toObject = (obj.position - Camera.main.transform.position).normalized;
        float dotProduct = Vector3.Dot(Camera.main.transform.forward, toObject);
        return dotProduct > 0;
    }

    private GameObject CreateProvisionalBattlefield(Vector3 position) {
        return Instantiate(provisionalBattlefieldPrefab, position, Quaternion.identity);
    }

    private Texture2D createRandomTexture()
    {
        Texture2D texture = new Texture2D(16, 16);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                Color randomColor = UnityEngine.Random.ColorHSV();
                texture.SetPixel(x, y, randomColor);
            }
        }

        // Apply the changes to the texture
        texture.Apply();
        return texture;
    }

    void SetElementNameAndImage(TextMeshProUGUI text, Image image, Element element) {
        string elementString = "";
        Sprite elementSprite = waterElemSprite;

        switch (element)
        {
            case Element.Water:
                elementString = "Water";
                elementSprite = waterElemSprite;
                break;
            case Element.Metal:
                elementString = "Metal";
                elementSprite = metalElemSprite;
                break;
            case Element.Earth:
                elementString = "Earth";
                elementSprite = earthElemSprite;
                break;
            case Element.Fire:
                elementString = "Fire";
                elementSprite = fireElemSprite;
                break;
            case Element.Wood:
                elementString = "Wood";
                elementSprite = woodElemSprite;
                break;
        }

        text.SetText(elementString);
        image.sprite = elementSprite;
    }

    private void ShowMainUI() {
        if (!mainUI.activeSelf) {
            var anim = mainUI.GetComponent<Animation>();
            anim["ShowMainUIAnim"].speed = 1f;
            anim.Play("ShowMainUIAnim");
            mainUI.SetActive(true);
        }

        if (currentCreature) {
            streakText.SetText(streak + " STREAK");
            hiScoreText.SetText("Hi-Score: " + maxStreak);

            nameMainUI.SetText("<b>Enemy</b>\n" + currentCreature.Name);
            highestStatMainUI.SetText(currentCreature.HighestStat());
            lowestStatMainUi.SetText(currentCreature.LowestStat());
            SetElementNameAndImage(elementNameMainUi, elementImageMainUi, currentCreature.Element);
        } else {
            nameMainUI.SetText("currentCreature is null, oh no");
        }
    }

    bool hidingMainUI = false;

    private void HideMainUI() {
        if (mainUI.activeSelf && !hidingMainUI) {
            hidingMainUI = true;
            StartCoroutine(HideMainUICoro());
        }
    }

    private IEnumerator HideMainUICoro() {
        var anim = mainUI.GetComponent<Animation>();
        anim["ShowMainUIAnim"].time = 0.40f;
        anim["ShowMainUIAnim"].speed = -1f;
        anim.Play("ShowMainUIAnim");
        yield return new WaitForSeconds(0.5f);
        mainUI.SetActive(false);
        hidingMainUI = false;
    }
}

public enum GameState {
    Intro,
    Picking,
    CreatureConfirmation,
    Fighting
}