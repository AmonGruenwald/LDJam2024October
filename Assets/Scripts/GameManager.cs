using UnityEngine;

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
    void Start()
    {
        currentCreature = CreatureCreator.CreateDummyCreature();
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
