using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CreatureSegmentation))]
public class GameManager : MonoBehaviour
{
    [SerializeField]
    public CreatureCreator CreatureCreator;
    [SerializeField]
    public FightManager FightManager;
    [SerializeField]
    bool DebugTrigger = false;

    private Creature currentCreature;
    private Creature newCreature;
    private bool fightRunning = false;
    private RotateAroundObject rotationCamera;

    private InputAction touchAction;

    private CreatureSegmentation segmentation;

    void Start()
    {
        segmentation = GetComponent<CreatureSegmentation>();
        touchAction = InputSystem.actions.FindAction("Touch");
        //currentCreature = CreatureCreator.CreateDummyCreature();
        FightManager.OnFightComplete += HandleFightResult;
    }

    private void HandleFightResult(Creature winner)
    {
        fightRunning = false;
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
        }

    }
    // Update is called once per frame
    void Update()
    {
        if (FightManager.FightRunning)
        {
            return;
        }
        if (DebugTrigger)
        {
            DebugTrigger = false;
            newCreature = CreatureCreator.CreateCreature(createRandomTexture());
            FightManager.StartFight(currentCreature, newCreature);
            this.fightRunning = true;
        }

        if (touchAction.WasPerformedThisFrame()) {
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
            if (texture == null) {
                return; // TODO: ???
            }
            
            var segmentedTexture = segmentation.SegmentTexture(texture, normalizedTouchPos);
            if (segmentedTexture == null) {
                return; // TODO: ???
            }

            if (currentCreature == null) {
                currentCreature = CreatureCreator.CreateDummyCreature();
            }
            newCreature = CreatureCreator.CreateCreature(segmentedTexture);
            FightManager.StartFight(currentCreature, newCreature);
            this.fightRunning = true;
        }
    }

    private Texture2D createRandomTexture()
    {
        Texture2D texture = new Texture2D(16, 16);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                Color randomColor = Random.ColorHSV();
                texture.SetPixel(x, y, randomColor);
            }
        }

        // Apply the changes to the texture
        texture.Apply();
        return texture;
    }
}
