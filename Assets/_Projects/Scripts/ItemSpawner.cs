using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ItemSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform boxSpawnPosition;
    [SerializeField] private GameObject boxPrefab;
    [SerializeField] private float boxSpawnDelay = 1f;

    [Header("Item Distribution")]
    [SerializeField] private List<GameObject> allItemPrefabs = new List<GameObject>(); // All 19 items
    [SerializeField] private int[] itemsPerDay = { 6, 6, 7 }; // Items to spawn each day

    [Header("Spawn Animation")]
    [SerializeField] private float itemSpawnDelay = 0.2f;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 2f, 0);
    [SerializeField] private float horizontalSpread = 2f; // How far left/right items can spawn
    [SerializeField] private float dropRadius = 1.5f; // Radius around box where items can land
    [SerializeField] private float dropDuration = 0.5f;
    [SerializeField] private Ease dropEase = Ease.OutBounce;

    [Header("Box Animation")]
    [SerializeField] private float boxClickScale = 1.2f;
    [SerializeField] private float boxClickDuration = 0.3f;
    [SerializeField] private Ease boxClickEase = Ease.OutElastic;
    [SerializeField] private Sprite openedBoxSprite;
    [SerializeField] private float boxOpenDelay = 0.1f;

    // Events
    public static System.Action<int> OnItemsSpawned; // day number
    public static System.Action OnBoxSpawned;
    public static System.Action OnAllItemsForDaySpawned;

    // Singleton
    public static ItemSpawner Instance { get; private set; }

    private List<GameObject> remainingItems = new List<GameObject>();
    private GameObject currentBox;
    private bool hasSpawnedToday = false;
    private int currentDayItems = 0;
    private List<GameObject> spawnedItemsToday = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeItemList();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Subscribe to events
        TimeManager.OnDayStarted += OnDayStarted;
        TimeManager.OnWorkTimeStarted += OnWorkTimeStarted;
        HermitCrabAnimator.OnHermitFinishedLeaving += OnHermitFinishedLeaving;
        TimeManager.OnDayEnded += OnDayEnded;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        TimeManager.OnDayStarted -= OnDayStarted;
        TimeManager.OnWorkTimeStarted -= OnWorkTimeStarted;
        HermitCrabAnimator.OnHermitFinishedLeaving -= OnHermitFinishedLeaving;
        TimeManager.OnDayEnded -= OnDayEnded;
    }

    private void InitializeItemList()
    {
        // Copy all items to remaining items list
        remainingItems.Clear();
        remainingItems.AddRange(allItemPrefabs);

        Debug.Log($"ItemSpawner: Initialized with {remainingItems.Count} items total");
    }

    private void OnDayStarted(int day)
    {
        hasSpawnedToday = false;
        currentDayItems = 0;
        spawnedItemsToday.Clear();

        Debug.Log($"ItemSpawner: Day {day} started. Items remaining: {remainingItems.Count}");
    }

    private void OnWorkTimeStarted()
    {
        // Don't spawn box immediately - wait for hermit to leave
        Debug.Log("ItemSpawner: Work time started, waiting for hermit to leave...");
    }

    private void OnHermitFinishedLeaving()
    {
        // Now spawn the box after hermit has left
        if (!hasSpawnedToday && remainingItems.Count > 0)
        {
            Invoke(nameof(SpawnBox), boxSpawnDelay);
        }
    }

    private void OnDayEnded(int day)
    {
        // Clean up any remaining box
        if (currentBox != null)
        {
            Destroy(currentBox);
            currentBox = null;
        }

        Debug.Log($"ItemSpawner: Day {day} ended. Spawned {spawnedItemsToday.Count} items today.");
    }

    private void SpawnBox()
    {
        if (hasSpawnedToday || remainingItems.Count == 0)
        {
            Debug.Log("ItemSpawner: Cannot spawn box - already spawned today or no items left");
            return;
        }

        if (boxPrefab == null || boxSpawnPosition == null)
        {
            Debug.LogError("ItemSpawner: Box prefab or spawn position not assigned!");
            return;
        }
        SoundManager.Instance.PlaySFX(SoundEffect.DecoSpawn);

        // Spawn the box
        currentBox = Instantiate(boxPrefab, boxSpawnPosition.position, boxSpawnPosition.rotation);

        // Add click functionality to the box
        BoxClickHandler clickHandler = currentBox.GetComponent<BoxClickHandler>();
        if (clickHandler == null)
        {
            clickHandler = currentBox.AddComponent<BoxClickHandler>();
        }

        clickHandler.Initialize(this);

        // Animate box spawn
        Vector3 originalScale = currentBox.transform.localScale;
        currentBox.transform.localScale = Vector3.zero;
        currentBox.transform.DOScale(originalScale, 0.5f).SetEase(Ease.OutBack);

        OnBoxSpawned?.Invoke();
        Debug.Log("ItemSpawner: Box spawned and ready for clicking");
    }

    public void OnBoxClicked()
    {
        if (hasSpawnedToday || currentBox == null)
        {
            Debug.Log("ItemSpawner: Box already used today");
            return;
        }
        SoundManager.Instance.PlaySFX(SoundEffect.OpenBox);
        StartCoroutine(SpawnItemsFromBox());
    }

    private System.Collections.IEnumerator SpawnItemsFromBox()
    {
        hasSpawnedToday = true;

        // Get current day (1-based)
        int currentDay = TimeManager.Instance?.CurrentDay ?? 1;
        int dayIndex = currentDay - 1; // Convert to 0-based for array

        // Determine how many items to spawn today
        int itemsToSpawn = 0;
        if (dayIndex >= 0 && dayIndex < itemsPerDay.Length)
        {
            itemsToSpawn = Mathf.Min(itemsPerDay[dayIndex], remainingItems.Count);
        }
        else
        {
            // Fallback: spawn remaining items
            itemsToSpawn = remainingItems.Count;
        }

        Debug.Log($"ItemSpawner: Spawning {itemsToSpawn} items for day {currentDay}");

        // Animate box opening (scale up and down)
        if (currentBox != null)
        {
            SpriteRenderer boxSpriteRenderer = currentBox.GetComponent<SpriteRenderer>();

            currentBox.transform.DOScale(currentBox.transform.localScale * boxClickScale, boxClickDuration)
                .SetEase(boxClickEase)
                .OnComplete(() =>
                {
                    if (currentBox != null)
                    {
                        // Change to opened box sprite after a brief delay
                        if (openedBoxSprite != null && boxSpriteRenderer != null)
                        {
                            boxSpriteRenderer.sprite = openedBoxSprite;
                        }

                        // Scale back to normal size
                        currentBox.transform.DOScale(Vector3.one, boxClickDuration * 0.5f)
                            .SetEase(Ease.OutQuad);
                    }
                });
        }

        // Wait a moment for box animation
        yield return new WaitForSeconds(boxClickDuration + boxOpenDelay);

        // Spawn items
        for (int i = 0; i < itemsToSpawn && remainingItems.Count > 0; i++)
        {
            SpawnSingleItem();
            SoundManager.Instance.PlaySFX(SoundEffect.DecoSpawn);
            yield return new WaitForSeconds(itemSpawnDelay);
        }

        if (currentBox != null)
        {
            yield return new WaitForSeconds(0.5f); // Brief pause after last item

            // Fade out the opened box
            SpriteRenderer boxSpriteRenderer = currentBox.GetComponent<SpriteRenderer>();
            if (boxSpriteRenderer != null)
            {
                boxSpriteRenderer.DOFade(0f, 1f).SetEase(Ease.InQuad);
            }

            // Scale down and destroy
            currentBox.transform.DOScale(Vector3.zero, 1f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    if (currentBox != null)
                    {
                        Destroy(currentBox);
                        currentBox = null;
                    }
                });
        }

        OnItemsSpawned?.Invoke(currentDay);
        OnAllItemsForDaySpawned?.Invoke();

        Debug.Log($"ItemSpawner: Finished spawning {itemsToSpawn} items. {remainingItems.Count} items remaining.");
    }

    private void SpawnSingleItem()
    {
        if (remainingItems.Count == 0) return;

        // Get random item from remaining list
        int randomIndex = Random.Range(0, remainingItems.Count);
        GameObject itemPrefab = remainingItems[randomIndex];

        // Remove from remaining list
        remainingItems.RemoveAt(randomIndex);

        // Create random spawn position above the box with horizontal spread
        Vector3 spawnPos = boxSpawnPosition.position + spawnOffset;

        // Add random horizontal offset for spawn position
        float randomHorizontalOffset = Random.Range(-horizontalSpread, horizontalSpread);
        spawnPos.x += randomHorizontalOffset;

        // Add slight vertical variation to make it look more natural
        spawnPos.y += Random.Range(0f, 0.5f);

        // Spawn the item (keeping original rotation from prefab)
        GameObject spawnedItem = Instantiate(itemPrefab, spawnPos, itemPrefab.transform.rotation);
        spawnedItemsToday.Add(spawnedItem);

        // Get the DraggableItem component and disable interaction during animation
        DraggableItem draggableComponent = spawnedItem.GetComponent<DraggableItem>();
        if (draggableComponent != null)
        {
            draggableComponent.isDraggable = false; // Disable during animation
        }

        // Calculate random drop target position in a circle around the box
        Vector2 randomCirclePoint = Random.insideUnitCircle * dropRadius;
        Vector3 targetPos = boxSpawnPosition.position + new Vector3(randomCirclePoint.x, randomCirclePoint.y, 0f); // FIXED: Use randomCirclePoint.y for Y axis

        // Make sure Z position is correct for 2D
        targetPos.z = 0f;

        // Animate drop with slight arc for more natural movement
        spawnedItem.transform.DOMove(targetPos, dropDuration)
            .SetEase(dropEase)
            .OnComplete(() =>
            {
                // Ensure final position has correct Z
                Vector3 finalPos = spawnedItem.transform.position;
                finalPos.z = 0f;
                spawnedItem.transform.position = finalPos;

                // Re-enable interaction after animation completes
                if (draggableComponent != null)
                {
                    draggableComponent.isDraggable = true; // Re-enable after animation
                    draggableComponent.isCurrentlyPlaced = true;
                    draggableComponent.lastValidPosition = finalPos;
                }

                Debug.Log($"ItemSpawner: {itemPrefab.name} landed and ready for interaction");
            });

        Debug.Log($"ItemSpawner: Spawned {itemPrefab.name} at offset ({randomHorizontalOffset:F2}, dropping to ({randomCirclePoint.x:F2}, {randomCirclePoint.y:F2}))");
    }

    // Public methods for debugging/manual control
    public void ForceSpawnBox()
    {
        if (currentBox == null)
        {
            SpawnBox();
        }
    }

    public void ResetSpawner()
    {
        InitializeItemList();
        hasSpawnedToday = false;
        currentDayItems = 0;
        spawnedItemsToday.Clear();

        if (currentBox != null)
        {
            Destroy(currentBox);
            currentBox = null;
        }
    }

    // Getters for debugging
    public int GetRemainingItemCount() => remainingItems.Count;
    public int GetSpawnedTodayCount() => spawnedItemsToday.Count;
    public bool HasSpawnedToday() => hasSpawnedToday;
}

// Simple click handler for the box
public class BoxClickHandler : MonoBehaviour
{
    private ItemSpawner spawner;
    private bool hasBeenClicked = false;

    public void Initialize(ItemSpawner itemSpawner)
    {
        spawner = itemSpawner;
    }

    private void OnMouseDown()
    {
        if (!hasBeenClicked && spawner != null)
        {
            hasBeenClicked = true;
            spawner.OnBoxClicked();
        }
    }

    // Alternative: Use collider trigger for touch/mobile support
    private void OnTriggerEnter2D(Collider2D other)
    {
        // This method can be used if you prefer trigger-based interaction
    }
}