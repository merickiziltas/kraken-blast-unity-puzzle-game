using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using KrakenCache.Core;
using KrakenCache.Data;
using KrakenCache.Items;
using KrakenCache.Logic;
using KrakenCache.Model;

namespace KrakenCache.Controllers
{
    public sealed class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance { get; private set; }
        [SerializeField] private float cellSize = 1.44f;
        [SerializeField] private float cellSpacing = 0.02f;
        [SerializeField] private GameAssets assets;
        public GridModel Grid { get; private set; }
        public GameState CurrentState { get; private set; } = GameState.Initializing;
        public LevelData CurrentLevelData { get; private set; }
        public int RemainingMoves { get; private set; }
        public float CellSize => cellSize;
        public GoalAnimationTracker GoalAnimations { get; } = new GoalAnimationTracker();
        public event Action OnLevelInitialized;
        public event Action<int> OnMovesChanged;
        public event Action<ObstacleType, int> OnGoalProgress;
        public event Action<Vector3, int> OnChalicesReleased;
        public event Action OnLevelWon;
        public event Action OnLevelLost;
        public BoardEffects Effects { get; private set; }
        private Camera mainCamera;
        private Transform boardRoot, itemsRoot;
        private BoardPresentation presentation;
        private readonly List<ObstacleItem> activeObstacles = new List<ObstacleItem>();
        private readonly Queue<KeyValuePair<Cell, SpecialItem>> chain = new Queue<KeyValuePair<Cell, SpecialItem>>();
        private readonly Dictionary<Cell, SpecialItem> queuedVisuals = new Dictionary<Cell, SpecialItem>();

        // Track unique cells and per-source obstacle limits, including door-only hits.
        private sealed class BlastSource
        {
            public readonly HashSet<Cell> Cells = new HashSet<Cell>();
            public readonly Dictionary<ObstacleItem, int> Hits = new Dictionary<ObstacleItem, int>();
            public readonly HashSet<ObstacleItem> CompletedObstacles = new HashSet<ObstacleItem>();
            public readonly Dictionary<ObstacleItem, int> AppliedCellHits = new Dictionary<ObstacleItem, int>();
            public bool ObstaclesApplied;
        }

        private void Awake()
        {
            Instance = this;
            DOTween.SetTweensCapacity(8192, 4096);
            mainCamera = Camera.main;
            if (assets == null) assets = Resources.Load<GameAssets>("GameAssets");
            Effects = gameObject.AddComponent<BoardEffects>();
            Effects.Setup(assets);
        }
        private void Start()
        {
            if (GameProgress.IsAllLevelsFinished) UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
            else InitializeLevel(GameProgress.LastPlayedLevel);
        }

        public void InitializeLevel(int levelNumber)
        {
            StopAllCoroutines();
            GameAudio.StopGameplay();
            Effects?.ClearTransient();
            if (mainCamera != null) mainCamera.transform.DOKill(true);
            GoalAnimations.Reset();
            chain.Clear();
            queuedVisuals.Clear();
            foreach (var obstacle in activeObstacles) Unsubscribe(obstacle);
            activeObstacles.Clear();
            if (boardRoot != null)
            {
                foreach (Transform child in boardRoot.GetComponentsInChildren<Transform>()) child.DOKill();
                Destroy(boardRoot.gameObject);
            }
            CurrentState = GameState.Initializing;
            CurrentLevelData = LevelLoader.LoadLevel(levelNumber);
            if (CurrentLevelData == null || assets == null || mainCamera == null) return;
            RemainingMoves = CurrentLevelData.move_count;
            Grid = new GridModel(CurrentLevelData.grid_width, CurrentLevelData.grid_height);
            boardRoot = new GameObject("BoardRoot").transform;
            boardRoot.SetParent(transform, false);
            itemsRoot = new GameObject("Items").transform;
            itemsRoot.SetParent(boardRoot, false);
            float pitch = cellSize + cellSpacing;
            Vector2 frameSize = new Vector2(Grid.Width * pitch + 0.55f, Grid.Height * pitch + 0.55f);
            var frame = new GameObject("BoardFrame");
            frame.transform.SetParent(boardRoot, false);
            var renderer = frame.AddComponent<SpriteRenderer>();
            renderer.sprite = assets.gridBackground;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = frameSize;
            renderer.sortingOrder = -20;
            presentation = boardRoot.gameObject.AddComponent<BoardPresentation>();
            presentation.Setup(mainCamera, boardRoot, assets, frameSize);
            BuildGrid();
            HintManager.UpdateHints(Grid);
            CurrentState = GameState.Idle;
            OnLevelInitialized?.Invoke();
            OnMovesChanged?.Invoke(RemainingMoves);
            foreach (ObstacleType type in Enum.GetValues(typeof(ObstacleType))) ReportGoalProgress(type);
        }

        private void BuildGrid()
        {
            var boxes = new Dictionary<Vector2Int, ChaliceBoxItem>();
            for (int col = 0; col < Grid.Width; col++)
                for (int row = 0; row < Grid.Height; row++)
                {
                    Cell cell = Grid.GetCell(col, row);
                    string token = CurrentLevelData.GetToken(col, row);
                    Item item;
                    switch (token)
                    {
                        case "r": item = new CubeItem(CubeColor.Red); break;
                        case "g": item = new CubeItem(CubeColor.Green); break;
                        case "b": item = new CubeItem(CubeColor.Blue); break;
                        case "y": item = new CubeItem(CubeColor.Yellow); break;
                        case "rand": item = new CubeItem(FallController.GetRandomCubeColor()); break;
                        case "vro": item = new RocketItem(true); break;
                        case "hro": item = new RocketItem(false); break;
                        case "t": item = new TntItem(); break;
                        case "s": item = new StoneItem(); break;
                        case "v": item = new VaseItem(); break;
                        case "cbBL": case "cbBR": case "cbTL": case "cbTR":
                            var key = new Vector2Int(col - (token.EndsWith("R") ? 1 : 0),
                                row - (token.StartsWith("cbT") ? 1 : 0));
                            if (!boxes.TryGetValue(key, out ChaliceBoxItem box))
                            {
                                box = new ChaliceBoxItem();
                                boxes.Add(key, box);
                            }
                            box.RegisterCell(cell);
                            item = box;
                            break;
                        default: item = null; break;
                    }
                    cell.SetItem(item);
                    if (item == null) continue;
                    if (!(item is ChaliceBoxItem) || token == "cbBL")
                    {
                        Vector3 position = CellToWorldPosition(col, row);
                        if (token == "cbBL") position += new Vector3(1, 1, 0) * (cellSize + cellSpacing) * 0.5f;
                        SpawnItemView(item, position);
                    }
                    if (item is ObstacleItem obstacle && !activeObstacles.Contains(obstacle))
                    {
                        activeObstacles.Add(obstacle);
                        obstacle.OnItemDestroyed += HandleObstacleDestroyed;
                        if (obstacle is ChaliceBoxItem cb) cb.OnChaliceCollected += HandleChaliceProgress;
                    }
                }
        }

        private ItemView SpawnItemView(Item item, Vector3 position)
        {
            var obj = new GameObject("Item_" + item.GetType().Name);
            obj.transform.SetParent(itemsRoot, false);
            obj.transform.position = position;
            var view = obj.AddComponent<ItemView>();
            view.Setup(item, assets);
            return view;
        }
        public Vector3 CellToWorldPosition(int col, int row)
        {
            float pitch = cellSize + cellSpacing;
            return boardRoot.position + new Vector3((col - (Grid.Width - 1) * 0.5f) * pitch,
                (row - (Grid.Height - 1) * 0.5f) * pitch, 0);
        }
        public Cell WorldPositionToCell(Vector3 position)
        {
            Vector3 local = position - boardRoot.position;
            float pitch = cellSize + cellSpacing;
            return Grid.GetCell(Mathf.RoundToInt(local.x / pitch + (Grid.Width - 1) * 0.5f),
                Mathf.RoundToInt(local.y / pitch + (Grid.Height - 1) * 0.5f));
        }
        private void Update()
        {
            if (CurrentState != GameState.Idle || mainCamera == null) return;
            if (!GetPointerDown(out Vector2 screenPos)) return;
            if (EventSystem.current != null)
            {
                var data = new PointerEventData(EventSystem.current) { position = screenPos };
                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(data, results);
                if (results.Count > 0) return;
            }
            TryTap(WorldPositionToCell(mainCamera.ScreenToWorldPoint(screenPos)));
        }
        private bool GetPointerDown(out Vector2 position)
        {
#if ENABLE_INPUT_SYSTEM
            var pointer = UnityEngine.InputSystem.Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            { position = pointer.position.ReadValue(); return true; }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0)) { position = Input.mousePosition; return true; }
#endif
            position = Vector2.zero;
            return false;
        }
        public bool TryTap(Cell cell)
        {
            if (CurrentState != GameState.Idle || cell == null || cell.IsEmpty || RemainingMoves <= 0) return false;
            if (cell.CurrentItem is CubeItem)
            {
                var group = MatchFinder.FindMatchingCubeGroup(Grid, cell);
                if (group.Count < 2) return false;
                StartCoroutine(BlastCubes(cell, group));
                return true;
            }
            if (cell.CurrentItem is SpecialItem)
            {
                StartCoroutine(BlastSpecials(cell, MatchFinder.FindConnectedSpecialItemGroup(Grid, cell)));
                return true;
            }
            return false;
        }
        private void BeginTurn()
        {
            CurrentState = GameState.Processing;
            RemainingMoves--;
            OnMovesChanged?.Invoke(RemainingMoves);
        }
        private IEnumerator BlastCubes(Cell tapped, List<Cell> group)
        {
            BeginTurn();
            Vector3 center = CellToWorldPosition(tapped.Col, tapped.Row);
            bool create = group.Count >= 4;
            GameAudio.Play(SoundCue.CubePop);
            foreach (Cell cell in group)
            {
                Item cube = cell.ClearItem();
                var view = cube.ViewObject != null ? cube.ViewObject.GetComponent<ItemView>() : null;
                if (view == null) continue;
                if (create) view.AnimateMergeTo(center, null);
                else view.AnimateBlast(null);
            }
            DamageAdjacentObstacles(group);
            yield return new WaitForSeconds(0.24f);
            if (create)
            {
                SpecialItem special = group.Count >= 6 ? (SpecialItem)new TntItem() : new RocketItem(UnityEngine.Random.value > 0.5f);
                tapped.SetItem(special);
                var view = SpawnItemView(special, center);
                view.AnimateAppear();
                GameAudio.Play(SoundCue.SpecialCreate);
                yield return new WaitForSeconds(0.2f);
            }
            yield return FinishTurn();
        }
        private IEnumerator BlastSpecials(Cell tapped, List<Cell> group)
        {
            BeginTurn();
            if (group.Count == 1) QueueSpecial(tapped);
            else
            {
                ComboType type = ExplosionCalculator.DetermineComboType(group);
                Vector3 center = CellToWorldPosition(tapped.Col, tapped.Row);
                ItemView anchor = null;
                int arrivals = 0;
                foreach (Cell cell in group)
                {
                    Item special = cell.ClearItem();
                    if (cell == tapped)
                    {
                        // One presentation anchor survives until the combined effect starts.
                        queuedVisuals[cell] = (SpecialItem)special;
                        if (special.ViewObject != null)
                        {
                            anchor = special.ViewObject.GetComponent<ItemView>();
                            Sprite preview = type == ComboType.RocketRocket
                                ? (((RocketItem)special).IsVertical ? assets.verticalRocket : assets.horizontalRocket) : assets.tnt;
                            anchor.PrepareComboAnchor(preview);
                        }
                    }
                    else if (special.ViewObject != null)
                    {
                        arrivals++;
                        special.ViewObject.GetComponent<ItemView>().AnimateComboMergeTo(center, () => arrivals--);
                    }
                }
                Effects.ComboGather(center, cellSize);
                yield return new WaitUntil(() => arrivals == 0);
                if (anchor != null)
                {
                    Effects.ComboCharge(center, cellSize);
                    Tween preparation = type == ComboType.RocketRocket ? anchor.AnimateRocketComboSpin()
                        : type == ComboType.TntTnt ? anchor.AnimateTntComboCharge(cellSize*2+cellSpacing)
                        : anchor.AnimateComboCharge(1.3f);
                    yield return preparation.WaitForCompletion();
                }
                var source = new BlastSource();
                if (type == ComboType.TntTnt) yield return Tnt(tapped, 3, source);
                else
                {
                    int lanes = type == ComboType.TntRocket ? 1 : 0;
                    // All tracks share one source so intersections do not double-hit a vase/box cell.
                    int running = 0;
                    for (int offset = -lanes; offset <= lanes; offset++)
                    {
                        if (Grid.IsValid(tapped.Col + offset, tapped.Row))
                        { running++; StartCoroutine(RocketTrack(tapped.Col + offset, tapped.Row, true, source, () => running--)); }
                        if (Grid.IsValid(tapped.Col, tapped.Row + offset))
                        { running++; StartCoroutine(RocketTrack(tapped.Col, tapped.Row + offset, false, source, () => running--)); }
                    }
                    yield return new WaitUntil(() => running == 0);
                }
                ApplyObstacleHits(source);
            }
            while (chain.Count > 0)
            {
                // Snapshot the current wave before starting it: its newly triggered
                // items stay queued for the next wave rather than joining a combo.
                var wave = new List<KeyValuePair<Cell, SpecialItem>>(chain.Count);
                while (chain.Count > 0) wave.Add(chain.Dequeue());
                int running = wave.Count;
                foreach (var entry in wave)
                    StartCoroutine(ResolveChainSource(entry, () => running--));
                yield return new WaitUntil(() => running == 0);
            }
            yield return FinishTurn();
        }
        private IEnumerator ResolveChainSource(KeyValuePair<Cell, SpecialItem> entry, Action complete)
        {
            // Each triggered special owns its own source, even when it starts in
            // the same frame as another special. Only tapped groups form combos.
            var source = new BlastSource();
            try
            {
                if (entry.Value is RocketItem rocket)
                    yield return RocketTrack(entry.Key.Col, entry.Key.Row, rocket.IsVertical, source, null);
                else yield return Tnt(entry.Key, 2, source);
                ApplyObstacleHits(source);
            }
            finally { complete?.Invoke(); }
        }
        private void QueueSpecial(Cell cell)
        {
            if (!(cell.CurrentItem is SpecialItem special)) return;
            cell.ClearItem();
            chain.Enqueue(new KeyValuePair<Cell, SpecialItem>(cell, special));
            // The model is removed now to prevent duplicate triggers, but its visual
            // remains until this queued explosion actually starts.
            queuedVisuals[cell] = special;
            if (special.ViewObject != null)
                special.ViewObject.GetComponent<ItemView>()?.AnimateQueuedActivation();
        }
        private void RetireQueuedVisual(Cell origin)
        {
            if (!queuedVisuals.TryGetValue(origin, out SpecialItem special)) return;
            queuedVisuals.Remove(origin);
            if (special.ViewObject != null)
            {
                // Replacement flight parts/effects already exist in this frame.
                // Hide immediately instead of overlapping until deferred Destroy.
                foreach (var renderer in special.ViewObject.GetComponentsInChildren<SpriteRenderer>())
                    renderer.enabled = false;
                BoardEffects.Dispose(special.ViewObject);
                special.ViewObject = null;
            }
        }
        private IEnumerator RocketTrack(int col, int row, bool vertical, BlastSource source, Action complete)
        {
            GameAudio.Play(SoundCue.Rocket);
            Vector3 center = CellToWorldPosition(col, row);
            GameObject positive = Effects.CreateRocketPart(vertical ? assets.verticalRocketPartTop : assets.horizontalRocketPartRight, center);
            GameObject negative = Effects.CreateRocketPart(vertical ? assets.verticalRocketPartBottom : assets.horizontalRocketPartLeft, center);
            RetireQueuedVisual(Grid.GetCell(col, row));
            int extent = vertical ? Grid.Height : Grid.Width;
            int origin = vertical ? row : col;
            float stepDuration = 0.065f;
            Vector3 dir = vertical ? Vector3.up : Vector3.right;
            var positiveBeam = Effects.CreateRocketBeam(center,cellSize);
            var negativeBeam = Effects.CreateRocketBeam(center,cellSize);
            float lower=(vertical?CellToWorldPosition(col,0).y:CellToWorldPosition(0,row).x)-(cellSize+cellSpacing)*.5f;
            float upper=(vertical?CellToWorldPosition(col,Grid.Height-1).y:CellToWorldPosition(Grid.Width-1,row).x)+(cellSize+cellSpacing)*.5f;
            Action<RocketBeam,Vector3> followBeam=(beam,endpoint)=>
            {
                if(beam==null)return;
                if(vertical)endpoint.y=Mathf.Clamp(endpoint.y,lower,upper);
                else endpoint.x=Mathf.Clamp(endpoint.x,lower,upper);
                beam.Follow(endpoint);
            };
            float positiveTrailTime = -1f, negativeTrailTime = -1f;
            Action<GameObject, Vector3> emitTrail = (part, direction) =>
                Effects.RocketTrail(part.transform.position, direction, cellSize);
            HitCell(Grid.GetCell(col, row), source, true);
            for (int distance = 1; distance <= Mathf.Max(origin + 1, extent - origin); distance++)
            {
                int ahead = origin + distance;
                int behind = origin - distance;
                foreach (var part in new[] { positive, negative }) part.transform.DOKill();
                positive.transform.DOMove(center + dir * distance * (cellSize + cellSpacing), stepDuration)
                    .SetEase(Ease.Linear).SetLink(positive).OnUpdate(() =>
                    {
                        followBeam(positiveBeam,positive.transform.position);
                        if (Time.time - positiveTrailTime < 0.035f) return;
                        positiveTrailTime = Time.time;
                        emitTrail(positive, dir);
                    });
                negative.transform.DOMove(center - dir * distance * (cellSize + cellSpacing), stepDuration)
                    .SetEase(Ease.Linear).SetLink(negative).OnUpdate(() =>
                    {
                        followBeam(negativeBeam,negative.transform.position);
                        if (Time.time - negativeTrailTime < 0.035f) return;
                        negativeTrailTime = Time.time;
                        emitTrail(negative, -dir);
                    });
                yield return new WaitForSeconds(stepDuration);
                if (ahead < extent) HitCell(vertical ? Grid.GetCell(col, ahead) : Grid.GetCell(ahead, row), source, true);
                if (behind >= 0) HitCell(vertical ? Grid.GetCell(col, behind) : Grid.GetCell(behind, row), source, true);
            }
            BoardEffects.Dispose(positive);
            BoardEffects.Dispose(negative);
            positiveBeam?.Finish(); negativeBeam?.Finish();
            complete?.Invoke();
        }
        private IEnumerator Tnt(Cell origin, int radius, BlastSource source)
        {
            Effects.Tnt(CellToWorldPosition(origin.Col, origin.Row), radius, cellSize);
            RetireQueuedVisual(origin);
            mainCamera.transform.DOKill(true);
            float shake = radius == 3 ? 0.2f : 0.15f;
            mainCamera.transform.DOShakePosition(0.25f, new Vector3(shake, shake, 0), 18, 90, false, true).SetLink(mainCamera.gameObject);
            for (int x = -radius; x <= radius; x++)
                for (int y = -radius; y <= radius; y++) HitCell(Grid.GetCell(origin.Col + x, origin.Row + y), source);
            // TNT resolves its whole area in this frame. Start obstacle reactions with
            // cube blasts, retaining the aggregate hit count for each 2x2 box.
            ApplyObstacleHits(source);
            yield return new WaitForSeconds(0.28f);
        }
        private void HitCell(Cell cell, BlastSource source, bool applyOnArrival = false)
        {
            if (cell == null || !source.Cells.Add(cell) || cell.IsEmpty) return;
            Item item = cell.CurrentItem;
            if (item is CubeItem)
            {
                cell.ClearItem();
                if (item.ViewObject != null) item.ViewObject.GetComponent<ItemView>().AnimateBlast(null);
            }
            else if (item is SpecialItem) QueueSpecial(cell);
            else if (item is ObstacleItem obstacle)
            {
                source.Hits.TryGetValue(obstacle, out int hits);
                source.Hits[obstacle] = hits + 1;
                if (applyOnArrival && !source.CompletedObstacles.Contains(obstacle))
                {
                    // Opening a door consumes this source for the entire box. Vases
                    // and stones likewise receive only one hit from this source.
                    if (!(obstacle is ChaliceBoxItem box) || box.IsDoorPhase)
                        source.CompletedObstacles.Add(obstacle);
                    else
                    {
                        source.AppliedCellHits.TryGetValue(obstacle, out int applied);
                        source.AppliedCellHits[obstacle] = applied + 1;
                    }
                    obstacle.TryTakeDamage(DamageSource.SpecialItem, 1, out _);
                }
            }
        }
        private void ApplyObstacleHits(BlastSource source)
        {
            // TNT applies before its animation wait; the outer chain/combo resolver
            // also finalizes sources. Each source must damage obstacles only once.
            if (source.ObstaclesApplied) return;
            source.ObstaclesApplied = true;
            foreach (var hit in source.Hits)
            {
                if (source.CompletedObstacles.Contains(hit.Key)) continue;
                source.AppliedCellHits.TryGetValue(hit.Key, out int applied);
                int remaining = hit.Value - applied;
                if (remaining > 0)
                    hit.Key.TryTakeDamage(DamageSource.SpecialItem, hit.Key is ChaliceBoxItem ? remaining : 1, out _);
            }
        }
        private void DamageAdjacentObstacles(List<Cell> group)
        {
            var hits = new Dictionary<ObstacleItem, int>();
            foreach (Cell cube in group)
            {
                var neighborsForCube = new HashSet<ObstacleItem>();
                foreach (Cell neighbor in Grid.GetOrthogonalNeighbors(cube.Col, cube.Row))
                    if (neighbor.CurrentItem is ObstacleItem obstacle) neighborsForCube.Add(obstacle);
                // One cube touching two cells of the same box still supplies one chalice.
                foreach (var obstacle in neighborsForCube)
                { hits.TryGetValue(obstacle, out int count); hits[obstacle] = count + 1; }
            }
            foreach (var hit in hits)
                hit.Key.TryTakeDamage(DamageSource.AdjacentBlast, hit.Key is ChaliceBoxItem ? hit.Value : 1, out _);
        }
        private IEnumerator FinishTurn()
        {
            var falls = FallController.CalculateFalls(Grid);
            float duration = 0;
            foreach (var move in falls)
                if (move.Item.ViewObject != null)
                    duration = Mathf.Max(duration, move.Item.ViewObject.GetComponent<ItemView>().AnimateFall(CellToWorldPosition(move.ToCell.Col, move.ToCell.Row), 0));
            if (duration > 0) yield return new WaitForSeconds(duration);
            var spawnCells = FallController.GetUnblockedEmptyCellsForSpawn(Grid);
            var columnCounts = new Dictionary<int, int>();
            duration = 0;
            foreach (Cell cell in spawnCells)
            {
                columnCounts.TryGetValue(cell.Col, out int index);
                columnCounts[cell.Col] = index + 1;
                var cube = new CubeItem(FallController.GetRandomCubeColor());
                cell.SetItem(cube);
                Vector3 target = CellToWorldPosition(cell.Col, cell.Row);
                Vector3 spawn = CellToWorldPosition(cell.Col, Grid.Height + index);
                var view = SpawnItemView(cube, spawn);
                duration = Mathf.Max(duration, view.AnimateFall(target, 0));
            }
            // The model already contains the final layout: give incoming cubes their hints
            // during the fall, so the settled board does not visibly switch hints later.
            HintManager.UpdateHints(Grid);
            if (duration > 0) yield return new WaitForSeconds(duration);
            // Ordinary turns reopen input as soon as the board settles. Only a result
            // panel needs to wait for the actual goal arrivals (or cancelled flights).
            if (activeObstacles.Count == 0 || RemainingMoves <= 0)
                yield return new WaitUntil(() => !GoalAnimations.HasPending);
            if (activeObstacles.Count == 0)
            {
                CurrentState = GameState.LevelWon;
                Effects.Celebrate(mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.6f, 10)));
                GameProgress.CompleteCurrentLevel();
                OnLevelWon?.Invoke();
            }
            else if (RemainingMoves <= 0) { CurrentState = GameState.LevelLost; OnLevelLost?.Invoke(); }
            else CurrentState = GameState.Idle;
        }
        public int GetRemainingGoal(ObstacleType type)
        {
            int count = 0;
            foreach (var obstacle in activeObstacles)
                if (obstacle.ObstacleType == type)
                    count += obstacle is ChaliceBoxItem box ? ChaliceBoxItem.TargetChalices - box.ChalicesCollected : 1;
            return count;
        }
        private void HandleChaliceProgress(int count) => ReportGoalProgress(ObstacleType.ChaliceBox);
        public void ReleaseChalices(Vector3 position, int count) => OnChalicesReleased?.Invoke(position, count);
        private void ReportGoalProgress(ObstacleType type) => OnGoalProgress?.Invoke(type, GetRemainingGoal(type));
        private void HandleObstacleDestroyed(Item item)
        {
            var obstacle = (ObstacleItem)item;
            activeObstacles.Remove(obstacle);
            Unsubscribe(obstacle);
            ReportGoalProgress(obstacle.ObstacleType);
        }
        private void Unsubscribe(ObstacleItem obstacle)
        {
            obstacle.OnItemDestroyed -= HandleObstacleDestroyed;
            if (obstacle is ChaliceBoxItem box) box.OnChaliceCollected -= HandleChaliceProgress;
        }
        private void OnDestroy()
        {
            queuedVisuals.Clear();
            chain.Clear();
            GoalAnimations.Reset();
            foreach (var obstacle in activeObstacles) Unsubscribe(obstacle);
            if (mainCamera != null) mainCamera.transform.DOKill();
            if (Instance == this) Instance = null;
        }
    }
}
