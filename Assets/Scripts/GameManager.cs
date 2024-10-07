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

    [SerializeField] private Sprite waterElemSprite;
    [SerializeField] private Sprite metalElemSprite;
    [SerializeField] private Sprite earthElemSprite;
    [SerializeField] private Sprite fireElemSprite;
    [SerializeField] private Sprite woodElemSprite;

    private Creature currentCreature;
    private Creature newCreature;
    private GameState state = GameState.Picking;

    private InputAction touchAction;
    private CreatureSegmentation segmentation;
    private CreatureClassification classification;
    private GameObject provisionalBattlefield;

    void Start()
    {
        segmentation = GetComponent<CreatureSegmentation>();
        classification = GetComponent<CreatureClassification>();
        touchAction = InputSystem.actions.FindAction("Touch");
        FightManager.OnFightComplete += HandleFightResult;
        creatureShowcaseUI.SetActive(false);
    }


    private void HandleFightResult(Creature winner)
    {
        state = GameState.Picking;
        if (winner.Equals(currentCreature))
        {
            Destroy(currentCreature.gameObject);
            Destroy(newCreature.gameObject);
            currentCreature = CreatureCreator.CreateDummyCreature();
            Debug.Log("U lost");
        }
        else
        {
            Debug.Log("Game continues");
            Destroy(currentCreature.gameObject);
            currentCreature = newCreature;
            currentCreature.CurrentHealth = currentCreature.Health;
        }

        if (provisionalBattlefield != null) {
            Destroy(provisionalBattlefield);
            provisionalBattlefield = null;
        }

    }
    void Update()
    {
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

                    yield return new WaitForSeconds(2f);

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
        
        string elementString = "";
        Sprite elementImage;

        switch (newCreature.Element)
        {
            case Element.Water:
                elementString = "Water";
                elementImage = waterElemSprite;
                break;
            case Element.Metal:
                elementString = "Metal";
                elementImage = metalElemSprite;
                break;
            case Element.Earth:
                elementString = "Earth";
                elementImage = earthElemSprite;
                break;
            case Element.Fire:
                elementString = "Fire";
                elementImage = fireElemSprite;
                break;
            case Element.Wood:
                elementString = "Wood";
                elementImage = woodElemSprite;
                break;
        }

        elementName.SetText(elementString);

        creatureShowcaseUI.GetComponent<Animation>()["UIShow"].speed = 1.0f;
        creatureShowcaseUI.GetComponent<Animation>()["UIShow"].time = 0.0f;
        creatureShowcaseUI.GetComponent<Animation>().Play("UIShow");

        yield return null;

        creatureShowcaseUI.SetActive(true);
        newCreature.transform.SetParent(creatureShowcasePoint, true);

        newCreature.transform.DOLocalRotateQuaternion(Quaternion.identity, duration).SetEase(Ease.InFlash).SetEase(Ease.OutBounce);
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
        } else {
            state = GameState.Picking;
            Destroy(newCreature.gameObject);
        }

        yield return new WaitForSeconds(1.0f);
        creatureShowcaseUI.SetActive(false);
    }

    private GameObject GetNearestFittingPlane() {
        var planes = FindObjectsByType<ARPlane>(FindObjectsSortMode.None)
            .Where((plane) => (plane.size.x * plane.size.y) > _minPlaneSize && plane.alignment.IsHorizontal())
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
}

public enum GameState {
    Picking,
    CreatureConfirmation,
    Fighting
}