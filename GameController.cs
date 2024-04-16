using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [Header("Camera Properties")]
    public Transform DiceView;
    public Transform MapView;
    public int cameraMoveSpeed;
    public bool cameraAlignedToMap = false;
    public bool cameraAlignedToDice = false;
    public Camera mainCamera;

    [Header("Map Properties")]
    public float TokenMoveSpeed;
    public GameObject ScrollPre;
    public GameObject PlayerTokenPrefab;
    GenMap genMap;
    GameObject PlayerToken;
    MapEvents mapEvents;

    [Header("Event")]
    public List<MapEventTemplate> Events = new List<MapEventTemplate>();

    [Header("Items")]
    
    public GameObject cardPrefab;
    CardHolder cardHolder;

    public List<DiceTemplate> DiceTemplates = new List<DiceTemplate>();

    public List<CardTemplate> CardTemplates = new List<CardTemplate>();

    public List<BoosterTemplate> boosters = new List<BoosterTemplate>();

    

    [Header("Health Properties")]
    public float MaxHealth;
    public float CurrentHealth;
    public TMP_Text HealthText;
    public GameObject HealthVile;    


    DiceRoller diceRoller;

    AtkCardHolder atkCardHolder;


    [Header("Game Properties")]
    public int currentRound = 1;
    Transform currentIconTransform;
    public Transform lastIconTransform;
    private GameObject Scroll;
    private delegate bool Comparison(float CurrentHealthVolume, float NewHealthVolume);
    public TMP_Text moneyText;



    [Header("Game Stats")]
    public int HitsTaken = 0;
    public int DamageTaken = 0;
    public int EnemiesKilled = 0;
    public int MoneyHeld = 0;
    public int TickDamage = 0;


    [Header("Booster properties")]
    public Dictionary<Rarity, int> roundWeights = new Dictionary<Rarity, int>(){
        {Rarity.CurrentlyImpossible,100},
        {Rarity.Legendary,95},
        {Rarity.Epic,80},
        {Rarity.Rare,40},
        {Rarity.Uncommon,15},
        {Rarity.Common,0},
    };
    
    //public Dictionary<Rarity,List<CardTemplate>> cardWeights = new Dictionary<Rarity,List<CardTemplate>>();


    


    public List<int> diceResults = new List<int>();

    ScoreCards scoreCards;


    public Dictionary<Rarity, (List<DiceTemplate>, List<CardTemplate>) > ItemWeights = new Dictionary<Rarity, (List<DiceTemplate>, List<CardTemplate>) >();

    public void SetItemWeights(){

        ItemWeights = new Dictionary<Rarity, (List<DiceTemplate>, List<CardTemplate>)>(){
            {Rarity.CurrentlyImpossible,(new List<DiceTemplate>{}, new List<CardTemplate>{})},
            {Rarity.Legendary,(new List<DiceTemplate>{}, new List<CardTemplate>{})},
            {Rarity.Epic,(new List<DiceTemplate>{}, new List<CardTemplate>{})},
            {Rarity.Rare,(new List<DiceTemplate>{}, new List<CardTemplate>{})},
            {Rarity.Uncommon,(new List<DiceTemplate>{}, new List<CardTemplate>{})},
            {Rarity.Common,(new List<DiceTemplate>{}, new List<CardTemplate>{})},
        };

        foreach(DiceTemplate template in DiceTemplates){
            ItemWeights[template.itemRarity].Item1.Add(template);
        }

        foreach(CardTemplate template in CardTemplates){
            ItemWeights[template.itemRarity].Item2.Add(template);
        }

    }

    public (Rarity, int) RandomItem(string type){

        int baseRarityPerc = Mathf.Clamp(Mathf.CeilToInt(Mathf.Pow(currentRound,2) / Random.Range(1.2f,1.5f)),1,101);
        int maxRarityPerc = Mathf.Clamp(Mathf.CeilToInt(Mathf.Pow(currentRound,2)),1,101);

        //1-100 
        int rarityPercent = Random.Range(baseRarityPerc,maxRarityPerc);

        List<Rarity> rarities = new List<Rarity>();

        foreach(KeyValuePair<Rarity, int> kvp in roundWeights){
            if(rarityPercent < kvp.Value){
                break;
            }
            rarities.Add(kvp.Key);
        }

        (Rarity, int) item = (Rarity.Common,0);


        if(type == "Dice"){
            foreach(Rarity rarity in rarities){
                if(ItemWeights[rarity].Item1.Count > 0){
                    item = (rarity,Random.Range(0,ItemWeights[rarity].Item1.Count));
                }else{
                    continue;
                }
            }
        }else{
            foreach(Rarity rarity in rarities){
                if(ItemWeights[rarity].Item2.Count > 0){
                    item = (rarity,Random.Range(0,ItemWeights[rarity].Item2.Count));
                }else{
                    continue;
                }
            }
        }

        return item;
    }


    public void UpdateMoney(int ammount, bool isBuying){

        if(!isBuying){
            bool corrupt = false;
            GameObject cardTriggered = null;

            foreach(GameObject card in cardHolder.CardsHeld){
                if(card.GetComponent<CardController>().cardType == CardType.CorruptCoins){
                    corrupt = true;
                    cardTriggered = card;
                }
            }

            for(int i = 0; i < ammount; i++){
                int toAdd = corrupt ? Random.Range(1,4) == 3 ? 2 : 1 : 1;

                if(toAdd == 2){
                    CardController cardController = cardTriggered.GetComponent<CardController>();
                    scoreCards.ScoreAnim(CardType.CorruptCoins);
                }
                
                moneyText.text = $"${int.Parse(moneyText.text.Substring(1)) + toAdd}";
                MoneyHeld = int.Parse(moneyText.text.Substring(1));
            }
        }else{
            moneyText.text = $"${int.Parse(moneyText.text.Substring(1)) - ammount}";
            MoneyHeld = int.Parse(moneyText.text.Substring(1));
        }
        

        
    }
    
    void Start()
    {
        //get all scripts
        mainCamera = FindObjectOfType<Camera>();
        genMap = FindObjectOfType<GenMap>();       
        diceRoller = FindObjectOfType<DiceRoller>();
        atkCardHolder = FindObjectOfType<AtkCardHolder>();
        mapEvents = FindObjectOfType<MapEvents>();
        cardHolder = FindObjectOfType<CardHolder>();
        scoreCards = FindObjectOfType<ScoreCards>();
    }

    //spawns in the starter dice and scroll, Generates map and moves camera to map. sets last icon to start icon
    void GameStarted(){
        CurrentHealth = 0;
        diceRoller.ActivateDice();
        UpdateHealth(MaxHealth,false);
        Scroll = GameObject.Instantiate(ScrollPre, new Vector3(4.9f, 1.3f, 113.2f), Quaternion.identity);
        Scroll.transform.rotation = Quaternion.Euler(-90f, -90f, 0f);
        genMap.IconGeneration();
        StartCoroutine(MapViewAnim());

        
    }

    float cameraZmin = 6;
    float cameraZmax = 45;
    //temp key press to start game
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.K)){
            GameStarted();
        }
        if(cameraAlignedToMap){
            Vector3 pos = mainCamera.transform.position;
            pos.z += Input.mouseScrollDelta.y;


            if(pos.z > cameraZmin && pos.z < cameraZmax){
                mainCamera.transform.position = pos;
            }
        }
    }

    public void RoundConclusion(){
        currentRound++;
        Scroll = GameObject.Instantiate(ScrollPre, new Vector3(4.9f, 1.3f, 113.2f), Quaternion.identity);
        Scroll.transform.rotation = Quaternion.Euler(-90f, -90f, 0f);
        genMap.displayIcons(true);
        StartCoroutine(MapViewAnim());
    }

    public void UpdateHealth(float ChangeFactor, bool Damaged){

        //ternary opperator for knowing if to add or subtract health
        float NewHealth = Damaged ? CurrentHealth -= ChangeFactor : CurrentHealth += ChangeFactor;

        float MinVileValue = 4.89f;
        float MaxVileValue = -3.8f;
        float HealthPercentile = NewHealth / MaxHealth;
        

        float CurrentHealthVolume = HealthVile.GetComponent<Liquid>().fillAmount;
        float NewHealthVolume = MinVileValue + (MaxVileValue - MinVileValue) * HealthPercentile;
        
        HealthText.text = MaxHealth.ToString() + "/" + NewHealth.ToString();

        

        if(Damaged){
            StartCoroutine(AnimHealth((CurrentHealthVolume, NewHealthVolume) => CurrentHealthVolume < NewHealthVolume, CurrentHealthVolume, NewHealthVolume, true));
        }else{
            StartCoroutine(AnimHealth((CurrentHealthVolume, NewHealthVolume) => CurrentHealthVolume > NewHealthVolume, CurrentHealthVolume, NewHealthVolume, false));
        }

       
    }

    public void IncreaseMaxHealth(float amount){
        MaxHealth += amount;
         UpdateHealth(amount,false);
    }

    private IEnumerator AnimHealth(Comparison comp, float CurrentHealthVolume, float NewHealthVolume, bool Damaged){

        while(comp(CurrentHealthVolume,NewHealthVolume)){

            CurrentHealthVolume = Damaged ? CurrentHealthVolume + 0.1f : CurrentHealthVolume - 0.1f;

            HealthVile.GetComponent<Liquid>().fillAmount = CurrentHealthVolume;

            yield return null;

        }
    }


    //called when an icon is clicked, moves player token to that icon, then disolves it and moves camera back to dice tray. clears environment and hides decals. scroll also rolls back up
    public IEnumerator IconSelected(Transform icon){

        currentIconTransform = icon;

        StartCoroutine(MovePlayerToken());

        yield return new WaitForSeconds(1.5f);

        StartCoroutine(DiceViewAnim());
        atkCardHolder.ReplenishCards();

        yield return new WaitForSeconds(1f);

        genMap.clearEnviro();
        genMap.displayIcons(false);

        yield return new WaitForSeconds(0.2f);

        Scroll.GetComponent<Animator>().SetBool("IconSelected", true);

        yield return new WaitForSeconds(2f);

        StartCoroutine(IconRoutine(icon.name));

    }

    public IEnumerator IconRoutine(string iconName){
        diceRoller.canRoll = false;
        switch(iconName){
            case "Encounter":
                StartCoroutine(mapEvents.SpawnEnemy(currentRound));
            break;

            case "Card Booster":
                mapEvents.SpawnBooster(false);
            break;

            case "Dice Booster":
                mapEvents.SpawnDiceBox(false);
            break;

            default:

                foreach(MapEventTemplate eventTemplate in Events){
                    if(iconName == eventTemplate.name){
                        StartCoroutine(mapEvents.SpawnEvent(eventTemplate));
                        break;
                    }
                }

            break;
        }
        yield return null;
    }

    //drops the player token at the current icon 
    public IEnumerator DropPlayerToken(){
        genMap.HighlightPaths(lastIconTransform);
        
        
        Vector3 tokenOffset = lastIconTransform.position;
        

        if(PlayerToken == null){
            PlayerToken = GameObject.Instantiate(PlayerTokenPrefab, tokenOffset + new Vector3(0,30,0), Quaternion.Euler(-90,0,0));
        }
        while(Vector3.Distance(PlayerToken.transform.position,tokenOffset) > 0.1f){
            PlayerToken.transform.position = Vector3.Lerp(PlayerToken.transform.position,tokenOffset, TokenMoveSpeed * Time.deltaTime);
            yield return null;
        }
        cameraAlignedToMap = true;
    }

    //moves the player token to the selected icon
    public IEnumerator MovePlayerToken(){
        Vector3 tokenOffset = currentIconTransform.position;

        while(Vector3.Distance(PlayerToken.transform.position,tokenOffset) > 0.1f){
            PlayerToken.transform.position = Vector3.Lerp(PlayerToken.transform.position, tokenOffset, TokenMoveSpeed * Time.deltaTime);
            yield return new WaitForSeconds(0.01f);
        }

        lastIconTransform = currentIconTransform.transform;

        StartCoroutine(DissolveToken());
    }

    //dissolves the token
    IEnumerator DissolveToken(){
        float StartValue = 0f;
        float EndValue = 1f;
        float TimeValue = 1f;
        float Elapsed = 0f;

        while(Elapsed < TimeValue){
            float CurrentValue = Mathf.Lerp(StartValue, EndValue, Elapsed / TimeValue);

            Elapsed += Time.deltaTime;
            PlayerToken.GetComponent<MeshRenderer>().material.SetFloat("_Step", CurrentValue);

            yield return null;
        }

        yield return new WaitForSeconds(1f);

        Destroy(PlayerToken);
    }
    
    //moves camera to the map
    public IEnumerator MapViewAnim(){
        cameraAlignedToDice = false;
        yield return new WaitForSeconds(2f);
        while(Vector3.Distance(mainCamera.transform.position, lastIconTransform.position + new Vector3(0,13,-5)) > 0.1f){
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, lastIconTransform.position + new Vector3(0,13,-5), cameraMoveSpeed * Time.deltaTime);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation ,Quaternion.Euler(48f,0f,0f), cameraMoveSpeed * Time.deltaTime);
            yield return new WaitForSeconds(0.01f);
        }
        StartCoroutine(DropPlayerToken());
    }

    //moves camera to the dice tray
    public IEnumerator DiceViewAnim(){
        cameraAlignedToMap = false;
        yield return new WaitForSeconds(0.1f);
        while(Vector3.Distance(mainCamera.transform.position, DiceView.position) > 0.1f){
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, DiceView.position, cameraMoveSpeed * Time.deltaTime);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation ,Quaternion.Euler(24.4f,0f,0f), cameraMoveSpeed * Time.deltaTime);
            yield return new WaitForSeconds(0.01f);
        }
        cameraAlignedToDice = true;
        Destroy(Scroll, 1f);
    }

    public IEnumerator MoveCameraTo(Transform newView){
        cameraAlignedToMap = false;
        yield return new WaitForSeconds(0.1f);
        while(Vector3.Distance(mainCamera.transform.position, newView.position) > 0.1f){
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, newView.position, cameraMoveSpeed * Time.deltaTime);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation ,newView.rotation, cameraMoveSpeed * Time.deltaTime);
            yield return new WaitForSeconds(0.01f);
        }
    }
   

}
